# ASP.NET Core Identity — Manager/Store Pattern Study

A single-file Web API that exercises every major `UserManager<T>` and `RoleManager<T>` method, with inline comments mapping each call to the underlying `IUser*Store` / `IRoleStore` interface.

## Run

```bash
# Requires PostgreSQL running on localhost (see appsettings.json)
dotnet run --project UserRoleExample
```

Startup auto-migrates the database and seeds roles: `Admin`, `Operator`, `Viewer`.

## Endpoints

| Group | Endpoints | Store interfaces exercised |
|---|---|---|
| `POST /register`, `POST /login` | Built-in Identity API (`MapIdentityApi`) | — |
| `/api/users` | GET list, GET by id, PUT update, DELETE | `IUserStore`, `IUserEmailStore`, `IUserPhoneNumberStore`, `IUserTokenStore`, `IUserLockoutStore` |
| `/api/users/{id}/roles` | POST assign, DELETE remove | `IUserRoleStore` |
| `/api/users/{id}/claims` | GET, POST, DELETE | `IUserClaimStore` |
| `/api/users/{id}/password/reset` | POST admin reset | `IUserPasswordStore`, `IUserTokenStore` |
| `/api/users/{id}/security-stamp/refresh` | POST force logout | `IUserSecurityStampStore` |
| `/api/roles` | GET list, POST create, DELETE | `IRoleStore` |
| `/api/roles/{name}/claims` | GET, POST, DELETE | `IRoleClaimStore` |

Use `UserRoleExample.http` (Rider/VS Code REST client) for ready-made requests.

## Why Use UserManager/RoleManager Instead of Direct EF Access

- **Normalization** — `ILookupNormalizer` uppercases emails/usernames before storing, making lookups case-insensitive without relying on DB collation.
- **Validation pipeline** — `IUserValidator` and `IPasswordValidator` chains run on every create/update. Direct EF writes skip these entirely.
- **Security stamp management** — Automatically regenerated on password/email/2FA changes. The cookie middleware checks this stamp; a mismatch invalidates all sessions. This is how "sign out all devices" works.
- **Token generation** — Password reset, email confirmation, and 2FA tokens go through `IUserTwoFactorTokenProvider` with purpose isolation and stamp-based invalidation. Getting this right from scratch is non-trivial.
- **Password hashing** — `IPasswordHasher<T>` uses PBKDF2 with automatic rehashing when iteration counts are upgraded. Setting `PasswordHash` directly bypasses this.
- **Concurrency** — Works with EF's `ConcurrencyStamp` for optimistic concurrency on user/role updates.
- **Store abstraction** — The 10+ `IUser*Store` interfaces let you swap the backing store (EF, Cosmos, Dapper) without changing application code.

## Key Concepts

### SecurityStamp vs ConcurrencyStamp

- **SecurityStamp** — Random GUID regenerated on security-sensitive changes (password, email, logins, 2FA). The security stamp validator middleware checks it periodically (`SecurityStampValidationInterval`). A mismatch forces logout. `UpdateSecurityStampAsync` is how you implement "sign out everywhere."
- **ConcurrencyStamp** — EF optimistic concurrency token. Included in `WHERE` clauses on `UPDATE`. If two requests race on the same user, the second gets `DbUpdateConcurrencyException`. Never set either stamp manually.

### Email Integration (e.g. AWS SES)

Identity separates token generation from delivery. The managers create/validate tokens; you own delivery via `IEmailSender<TUser>`. The default implementation is a no-op.

To add real email:
1. Install a provider (`AWSSDK.SimpleEmail`, SendGrid, etc.)
2. Implement `IEmailSender<AppUser>` with three methods: `SendConfirmationLinkAsync`, `SendPasswordResetLinkAsync`, `SendPasswordResetCodeAsync`
3. Register it: `builder.Services.AddSingleton<IEmailSender<AppUser>, YourSender>()`

The flow: Identity generates a token → your sender delivers it → user submits it back → Identity validates via `IUserTokenStore`.

### OAuth / External Login Providers

`IUserLoginStore` (backed by `AspNetUserLogins`) already stores provider/key mappings. Adding a provider is:

```csharp
dotnet add package Microsoft.AspNetCore.Authentication.Google

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = "...";
        options.ClientSecret = "...";
    });
```

On sign-in, `SignInManager` calls `FindByLoginAsync` to check if the external login is linked. Same pattern for GitHub, Microsoft, Facebook, or any custom OIDC provider via `.AddOpenIdConnect()`.

## Store Interfaces Not Demonstrated

### `IUserAuthenticatorKeyStore`
Stores the shared TOTP secret for authenticator apps (Google Authenticator, Authy). When a user sets up 2FA, `ResetAuthenticatorKeyAsync` generates a new key that gets encoded into a QR code. `GetAuthenticatorKeyAsync` retrieves it for verification. The key lives in the `AspNetUserTokens` table.

### `IUserTwoFactorRecoveryCodeStore`
Stores one-time backup codes for when a user loses their authenticator device. `GenerateNewTwoFactorRecoveryCodesAsync` creates N codes, `RedeemTwoFactorRecoveryCodeAsync` consumes one (returns whether it was valid), and `CountCodesAsync` reports how many remain. Codes are also stored in `AspNetUserTokens`.

### `IQueryableUserStore` / `IQueryableRoleStore`
Exposes `IQueryable<TUser>` and `IQueryable<TRole>`. This is what makes `userManager.Users` and `roleManager.Roles` work as LINQ queryables — the list endpoints in this project use it implicitly. Without these interfaces, there's no way to enumerate users or roles through the managers.

## Abstractions Above the Stores

### `SignInManager<T>`
Sits above `UserManager` and orchestrates multi-step sign-in flows. `MapIdentityApi` uses it internally for `/login`. Key methods:
- `PasswordSignInAsync` — validates password, checks lockout, increments failed attempts, issues cookie
- `TwoFactorSignInAsync` — validates TOTP or recovery code as a second factor
- `ExternalLoginSignInAsync` — finds user by external login provider/key, signs them in
- `RefreshSignInAsync` — re-issues cookie with a fresh security stamp

If you need custom sign-in logic (e.g. checking an `IsActive` flag), you'd override or wrap `SignInManager`.

### `IPasswordHasher<T>`
Pluggable password hashing. Default uses PBKDF2 with versioned format headers: V2 = SHA1 (legacy ASP.NET Identity), V3 = SHA256/SHA512. When a V2 hash is verified successfully, it automatically rehashes to V3 on the next login. Swappable with Argon2/bcrypt by implementing this interface.

### `ILookupNormalizer`
Uppercases emails and usernames for indexed lookups. This is why `NormalizedEmail` and `NormalizedUserName` columns exist — the manager normalizes the value, stores the result, and all lookups query the normalized column instead of the raw value.

### `IUserConfirmation<T>`
Controls what "confirmed" means for authorization purposes. Default just checks `EmailConfirmed`. You could override it to also require admin approval, a completed profile, or an active subscription.

### Architecture Layers

```
Your Code
   ↓
SignInManager       (orchestrates sign-in flows)
   ↓
UserManager / RoleManager  (validation, normalization, stamps, tokens)
   ↓
IUser*Store / IRoleStore   (data access contract)
   ↓
UserStore<T> / RoleStore<T> (EF Core implementation)
   ↓
DbContext → Database
```

Each layer adds behavior. Skipping a layer means skipping that behavior.

## Anti-Patterns to Avoid

- **Don't inject `IUser*Store` directly** — always go through the manager, which orchestrates validation, normalization, and stamp management.
- **Don't set `PasswordHash`, `NormalizedEmail`, `NormalizedUserName`, or `SecurityStamp` via EF** — the manager handles all of these.
- **Don't skip token generation for email/phone changes** — even for admin operations, the token round-trip is the security stamp validation pattern.
