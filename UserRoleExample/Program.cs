using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddIdentityApiEndpoints<AppUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] seedRoles = ["Admin", "Operator", "Viewer"];
    foreach (var role in seedRoles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

app.MapIdentityApi<AppUser>();

var users = app.MapGroup("/api/users").RequireAuthorization();

users.MapGet("/", async (UserManager<AppUser> userManager) =>
{
    var all = await userManager.Users
        .Select(u => new
        {
            u.Id, u.Email, u.DisplayName, u.FacilityCode, u.LockoutEnd
        })
        .ToListAsync();
    return Results.Ok(all);
});

users.MapGet("/{id}", async (string id, UserManager<AppUser> userManager) =>
{
    var user = await userManager.FindByIdAsync(id); // IUserStore.FindByIdAsync
    if (user is null) return Results.NotFound();

    var roles = await userManager.GetRolesAsync(user);           // IUserRoleStore.GetRolesAsync
    var claims = await userManager.GetClaimsAsync(user);         // IUserClaimStore.GetClaimsAsync
    var logins = await userManager.GetLoginsAsync(user);         // IUserLoginStore.GetLoginsAsync
    var twoFactor = await userManager.GetTwoFactorEnabledAsync(user); // IUserTwoFactorStore.GetTwoFactorEnabledAsync
    var lockedOut = await userManager.IsLockedOutAsync(user);    // IUserLockoutStore.IsLockedOutAsync

    return Results.Ok(new
    {
        user.Id,
        user.Email,
        user.EmailConfirmed,
        user.PhoneNumber,
        user.DisplayName,
        user.FacilityCode,
        Roles = roles,
        Claims = claims.Select(c => new { c.Type, c.Value }),
        Logins = logins.Select(l => new { l.LoginProvider, l.ProviderDisplayName }),
        TwoFactorEnabled = twoFactor,
        IsLockedOut = lockedOut,
        user.LockoutEnd
    });
});

users.MapPut("/{id}", async (string id, UpdateUserDto dto, UserManager<AppUser> userManager) =>
{
    var user = await userManager.FindByIdAsync(id); // IUserStore.FindByIdAsync
    if (user is null) return Results.NotFound();

    if (dto.Email is not null && dto.Email != user.Email)
    {
        var token = await userManager.GenerateChangeEmailTokenAsync(user, dto.Email); // IUserTokenStore (token generation) + IUserEmailStore
        var result = await userManager.ChangeEmailAsync(user, dto.Email, token);      // IUserEmailStore.SetEmailAsync + IUserTokenStore (token validation)
        if (!result.Succeeded) return Results.BadRequest(result.Errors);
    }

    if (dto.PhoneNumber is not null && dto.PhoneNumber != user.PhoneNumber)
    {
        var result = await userManager.SetPhoneNumberAsync(user, dto.PhoneNumber); // IUserPhoneNumberStore.SetPhoneNumberAsync
        if (!result.Succeeded) return Results.BadRequest(result.Errors);
    }

    var changed = false;
    if (dto.DisplayName is not null) { user.DisplayName = dto.DisplayName; changed = true; }
    if (dto.FacilityCode is not null) { user.FacilityCode = dto.FacilityCode; changed = true; }

    if (changed)
    {
        var result = await userManager.UpdateAsync(user); // IUserStore.UpdateAsync
        if (!result.Succeeded) return Results.BadRequest(result.Errors);
    }

    if (dto.Locked.HasValue)
    {
        var lockoutEnd = dto.Locked.Value ? DateTimeOffset.MaxValue : (DateTimeOffset?)null;
        var result = await userManager.SetLockoutEndDateAsync(user, lockoutEnd); // IUserLockoutStore.SetLockoutEndDateAsync
        if (!result.Succeeded) return Results.BadRequest(result.Errors);
    }

    return Results.NoContent();
});

users.MapDelete("/{id}", async (string id, UserManager<AppUser> userManager) =>
{
    var user = await userManager.FindByIdAsync(id); // IUserStore.FindByIdAsync
    if (user is null) return Results.NotFound();

    var result = await userManager.DeleteAsync(user); // IUserStore.DeleteAsync
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(result.Errors);
});

users.MapPost("/{id}/roles", async (string id, RoleAssignDto dto, UserManager<AppUser> userManager) =>
{
    var user = await userManager.FindByIdAsync(id); // IUserStore.FindByIdAsync
    if (user is null) return Results.NotFound();

    var result = await userManager.AddToRolesAsync(user, dto.Roles); // IUserRoleStore.AddToRoleAsync (called per role)
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(result.Errors);
});

users.MapDelete("/{id}/roles", async (string id, [FromBody] RoleAssignDto dto, UserManager<AppUser> userManager) =>
{
    var user = await userManager.FindByIdAsync(id); // IUserStore.FindByIdAsync
    if (user is null) return Results.NotFound();

    var result = await userManager.RemoveFromRolesAsync(user, dto.Roles); // IUserRoleStore.RemoveFromRoleAsync (called per role)
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(result.Errors);
});

users.MapGet("/{id}/claims", async (string id, UserManager<AppUser> userManager) =>
{
    var user = await userManager.FindByIdAsync(id); // IUserStore.FindByIdAsync
    if (user is null) return Results.NotFound();

    var claims = await userManager.GetClaimsAsync(user); // IUserClaimStore.GetClaimsAsync
    return Results.Ok(claims.Select(c => new { c.Type, c.Value }));
});

users.MapPost("/{id}/claims", async (string id, ClaimDto dto, UserManager<AppUser> userManager) =>
{
    var user = await userManager.FindByIdAsync(id); // IUserStore.FindByIdAsync
    if (user is null) return Results.NotFound();

    var result = await userManager.AddClaimAsync(user, new Claim(dto.Type, dto.Value)); // IUserClaimStore.AddClaimsAsync
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(result.Errors);
});

users.MapDelete("/{id}/claims", async (string id, [FromBody] ClaimDto dto, UserManager<AppUser> userManager) =>
{
    var user = await userManager.FindByIdAsync(id); // IUserStore.FindByIdAsync
    if (user is null) return Results.NotFound();

    var result = await userManager.RemoveClaimAsync(user, new Claim(dto.Type, dto.Value)); // IUserClaimStore.RemoveClaimsAsync
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(result.Errors);
});

users.MapPost("/{id}/password/reset", async (string id, ResetPasswordDto dto, UserManager<AppUser> userManager) =>
{
    var user = await userManager.FindByIdAsync(id); // IUserStore.FindByIdAsync
    if (user is null) return Results.NotFound();

    var token = await userManager.GeneratePasswordResetTokenAsync(user); // IUserTokenStore (via token provider)
    var result = await userManager.ResetPasswordAsync(user, token, dto.NewPassword); // IUserPasswordStore.SetPasswordHashAsync
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(result.Errors);
});

users.MapPost("/{id}/security-stamp/refresh", async (string id, UserManager<AppUser> userManager) =>
{
    var user = await userManager.FindByIdAsync(id); // IUserStore.FindByIdAsync
    if (user is null) return Results.NotFound();

    var result = await userManager.UpdateSecurityStampAsync(user); // IUserSecurityStampStore.SetSecurityStampAsync + IUserStore.UpdateAsync
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(result.Errors);
});

var roles = app.MapGroup("/api/roles").RequireAuthorization();

roles.MapGet("/", async (RoleManager<IdentityRole> roleManager) =>
{
    var all = await roleManager.Roles.Select(r => new { r.Id, r.Name }).ToListAsync();
    return Results.Ok(all);
});

roles.MapPost("/", async (CreateRoleDto dto, RoleManager<IdentityRole> roleManager) =>
{
    var result = await roleManager.CreateAsync(new IdentityRole(dto.Name)); // IRoleStore.CreateAsync
    return result.Succeeded ? Results.Created($"/api/roles/{dto.Name}", null) : Results.BadRequest(result.Errors);
});

roles.MapDelete("/{name}", async (string name, RoleManager<IdentityRole> roleManager) =>
{
    var role = await roleManager.FindByNameAsync(name); // IRoleStore.FindByNameAsync
    if (role is null) return Results.NotFound();

    var result = await roleManager.DeleteAsync(role); // IRoleStore.DeleteAsync
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(result.Errors);
});

roles.MapGet("/{name}/claims", async (string name, RoleManager<IdentityRole> roleManager) =>
{
    var role = await roleManager.FindByNameAsync(name); // IRoleStore.FindByNameAsync
    if (role is null) return Results.NotFound();

    var claims = await roleManager.GetClaimsAsync(role); // IRoleClaimStore.GetClaimsAsync
    return Results.Ok(claims.Select(c => new { c.Type, c.Value }));
});

roles.MapPost("/{name}/claims", async (string name, ClaimDto dto, RoleManager<IdentityRole> roleManager) =>
{
    var role = await roleManager.FindByNameAsync(name); // IRoleStore.FindByNameAsync
    if (role is null) return Results.NotFound();

    await roleManager.AddClaimAsync(role, new Claim(dto.Type, dto.Value)); // IRoleClaimStore.AddClaimAsync
    return Results.NoContent();
});

roles.MapDelete("/{name}/claims", async (string name, [FromBody] ClaimDto dto, RoleManager<IdentityRole> roleManager) =>
{
    var role = await roleManager.FindByNameAsync(name); // IRoleStore.FindByNameAsync
    if (role is null) return Results.NotFound();

    await roleManager.RemoveClaimAsync(role, new Claim(dto.Type, dto.Value)); // IRoleClaimStore.RemoveClaimAsync
    return Results.NoContent();
});

app.Run();

public class AppUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public string? FacilityCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options);

public record UpdateUserDto(string? Email, string? PhoneNumber, string? DisplayName, string? FacilityCode, bool? Locked);
public record RoleAssignDto(string[] Roles);
public record ClaimDto(string Type, string Value);
public record CreateRoleDto(string Name);
public record ResetPasswordDto(string NewPassword);
