// ============================================================================
// TOPIC: Encapsulation
// ============================================================================
// INTERVIEW ANSWER:
// Encapsulation is about bundling data with the methods that operate on it and
// restricting direct access to the internal state. In C#, we use access modifiers
// (private, protected, internal, public) and properties to control HOW outside
// code interacts with an object. The goal is to protect invariants — making it
// impossible to put an object into an invalid state from outside the class.
// ============================================================================

// ---- BAD: No encapsulation — public fields everywhere ----

// INTERVIEW ANSWER: This is a classic encapsulation violation. With public fields,
// any code can set Balance to -1 million or Email to garbage. There's no way to
// enforce business rules because the data is completely exposed.
public class BadBankAccount
{
    public string AccountNumber = "";
    public string OwnerName = "";
    public decimal Balance = 0;
    public string Email = "";
    public bool IsActive = true;
}

// ---- GOOD: Proper encapsulation — controlled API ----

// INTERVIEW ANSWER: Here, the internal state is private. The only way to change
// balance is through Deposit/Withdraw, which enforce our business rules. The
// `init` accessor lets you set a value during construction but not after. The
// `required` keyword ensures you can't forget to set essential properties.
public class BankAccount
{
    // INTERVIEW ANSWER: `required` (C# 11) means the compiler won't let you
    // create a BankAccount without setting these. Combined with `init`, they
    // can be set during initialization but never modified after.
    public required string AccountNumber { get; init; }
    public required string OwnerName { get; init; }

    // Private backing field — only this class can directly modify balance
    private decimal _balance;

    // Public read-only property — outsiders can see the balance but can't set it
    public decimal Balance => _balance;

    // INTERVIEW ANSWER: `private set` means only code inside this class can
    // change this value. External code can read it but not write it.
    public bool IsActive { get; private set; } = true;

    // INTERVIEW ANSWER: `internal` means this is accessible within the same
    // assembly but invisible to external consumers. Useful for cross-class
    // collaboration within a library without exposing implementation details.
    internal DateTime LastAuditDate { get; set; } = DateTime.UtcNow;

    private string _email = "";
    public string Email
    {
        get => _email;
        set
        {
            // INTERVIEW ANSWER: Property setters are where encapsulation shines —
            // we validate EVERY attempt to change the email. No one can set an
            // invalid email because the setter enforces the rule.
            if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
                throw new ArgumentException("Invalid email address", nameof(value));
            _email = value;
        }
    }

    // INTERVIEW ANSWER: We protect the invariant "balance can't go negative" by
    // making Deposit/Withdraw the ONLY way to change the balance. Both methods
    // validate inputs and enforce rules before modifying state.
    public void Deposit(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ThrowIfInactive();
        _balance += amount;
        Console.WriteLine($"  Deposited {amount:C} → Balance: {_balance:C}");
    }

    public bool Withdraw(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ThrowIfInactive();

        if (amount > _balance)
        {
            Console.WriteLine($"  Withdrawal of {amount:C} denied — insufficient funds (Balance: {_balance:C})");
            return false;
        }

        _balance -= amount;
        Console.WriteLine($"  Withdrew {amount:C} → Balance: {_balance:C}");
        return true;
    }

    public void Deactivate()
    {
        IsActive = false;
        Console.WriteLine($"  Account {AccountNumber} deactivated");
    }

    // INTERVIEW ANSWER: `protected` lets derived classes access this but keeps
    // it hidden from external code. This is useful when you want subclasses
    // to participate in enforcement without exposing internals publicly.
    protected decimal GetRawBalance() => _balance;

    private void ThrowIfInactive()
    {
        if (!IsActive)
            throw new InvalidOperationException($"Account {AccountNumber} is not active");
    }
}

// Show that a derived class can access protected members
public class PremiumBankAccount : BankAccount
{
    public decimal OverdraftLimit { get; init; } = 1000m;

    public void ShowInternalBalance()
    {
        // Can access protected GetRawBalance() from derived class
        Console.WriteLine($"  [Premium] Raw balance via protected accessor: {GetRawBalance():C}");
    }
}

// ---- Demonstrating init and required ----

// INTERVIEW ANSWER: `init` properties can only be set during object initialization
// (in the constructor or object initializer). After that, they're effectively
// read-only. Combined with `required`, you get compile-time guarantees that
// essential data is always provided.
public class ApiConfiguration
{
    public required string BaseUrl { get; init; }
    public required string ApiKey { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxRetries { get; init; } = 3;

    // Private constructor + factory method = even more control
    // (in this case we use public constructor for simplicity)

    public override string ToString() =>
        $"URL: {BaseUrl}, Key: {ApiKey[..4]}****, Timeout: {TimeoutSeconds}s, Retries: {MaxRetries}";
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== ENCAPSULATION DEMO ===\n");

// --- BAD example ---
Console.WriteLine("--- BAD: No Encapsulation ---");
var badAccount = new BadBankAccount();
badAccount.Balance = -999_999m;    // No validation!
badAccount.Email = "not-an-email"; // No validation!
badAccount.AccountNumber = "";     // Empty account number!
Console.WriteLine($"  Balance: {badAccount.Balance:C} (should never be negative!)");
Console.WriteLine($"  Email: {badAccount.Email} (invalid!)");
Console.WriteLine($"  AccountNumber: '{badAccount.AccountNumber}' (empty!)");
Console.WriteLine();

// --- GOOD example ---
Console.WriteLine("--- GOOD: Proper Encapsulation ---");
var account = new BankAccount
{
    AccountNumber = "ACC-001",
    OwnerName = "Jane Smith",
    Email = "jane@example.com"
};

Console.WriteLine($"  Account: {account.AccountNumber}, Owner: {account.OwnerName}");
Console.WriteLine($"  Initial balance: {account.Balance:C}");

account.Deposit(1000m);
account.Deposit(500m);
account.Withdraw(200m);
account.Withdraw(2000m); // Should fail — insufficient funds

Console.WriteLine();

// Try to set invalid email — will throw
Console.WriteLine("--- Validating Email ---");
try
{
    account.Email = "bad-email";
}
catch (ArgumentException ex)
{
    Console.WriteLine($"  Caught: {ex.Message}");
}

account.Email = "jane.smith@example.com";
Console.WriteLine($"  New email: {account.Email}");
Console.WriteLine();

// Show that init properties can't be changed after construction
// These lines would NOT compile:
// account.AccountNumber = "ACC-002";  // Error: init-only property
// account.OwnerName = "John";         // Error: init-only property

// --- Deactivation ---
Console.WriteLine("--- Account Deactivation ---");
account.Deactivate();
try
{
    account.Deposit(100m); // Should throw — account is inactive
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"  Caught: {ex.Message}");
}
Console.WriteLine();

// --- Protected access from derived class ---
Console.WriteLine("--- Protected Members ---");
var premium = new PremiumBankAccount
{
    AccountNumber = "PREM-001",
    OwnerName = "Bob Premium",
    Email = "bob@vip.com"
};
premium.Deposit(5000m);
premium.ShowInternalBalance(); // Uses protected accessor
Console.WriteLine();

// --- Init and Required ---
Console.WriteLine("--- Init/Required Properties ---");
var config = new ApiConfiguration
{
    BaseUrl = "https://api.example.com",
    ApiKey = "sk-1234567890abcdef",
    TimeoutSeconds = 60
};
Console.WriteLine($"  Config: {config}");

// These would NOT compile:
// config.BaseUrl = "https://other.com";  // Error: init-only
// var bad = new ApiConfiguration { BaseUrl = "..." }; // Error: missing required ApiKey
