// ============================================================================
// TOPIC: Inheritance
// ============================================================================
// INTERVIEW ANSWER:
// Inheritance lets a derived class reuse and extend the behavior of a base class.
// In C#, a class can inherit from exactly one base class but implement multiple
// interfaces. The derived class gets all non-private members of the base and can
// override virtual methods to specialize behavior. It's one of the core mechanisms
// for code reuse and establishing "is-a" relationships.
// ============================================================================

// INTERVIEW ANSWER: The `virtual` keyword on a base class method says "derived classes
// CAN override this, but they don't have to — there's a sensible default." The `override`
// keyword in the derived class provides the new implementation.

public class PaymentProcessor
{
    public string ProcessorName { get; }
    public decimal FeePercentage { get; protected set; }

    // INTERVIEW ANSWER: Constructor chaining with `base()` ensures the base class
    // is properly initialized before the derived class adds its own setup.
    // The base constructor always runs first.
    public PaymentProcessor(string name, decimal feePercentage)
    {
        ProcessorName = name;
        FeePercentage = feePercentage;
    }

    // Virtual method — provides default behavior that can be overridden
    public virtual PaymentResult ProcessPayment(decimal amount)
    {
        var fee = amount * (FeePercentage / 100m);
        return new PaymentResult(amount, fee, $"Processed by {ProcessorName}");
    }

    // Non-virtual — derived classes CANNOT override this. This is intentional:
    // validation logic should be consistent across all processors.
    public bool ValidateAmount(decimal amount) => amount > 0 && amount <= 50_000m;

    // INTERVIEW ANSWER: `protected` means only this class and its derived classes
    // can access this member. It's how you expose internals to subclasses without
    // making them public to everyone.
    protected void LogTransaction(string message)
    {
        Console.WriteLine($"  [{ProcessorName} LOG] {message}");
    }
}

// Inheriting from PaymentProcessor — StripeProcessor "is a" PaymentProcessor
public class StripeProcessor : PaymentProcessor
{
    public string ApiVersion { get; }

    // Constructor chaining — call base constructor, then do our own setup
    public StripeProcessor(string apiVersion)
        : base("Stripe", 2.9m)
    {
        ApiVersion = apiVersion;
    }

    // Override to add Stripe-specific behavior
    public override PaymentResult ProcessPayment(decimal amount)
    {
        LogTransaction($"Initiating Stripe charge for {amount:C} (API {ApiVersion})");
        var fee = amount * (FeePercentage / 100m) + 0.30m; // Stripe's per-transaction fee
        return new PaymentResult(amount, fee, $"Stripe charge successful (API {ApiVersion})");
    }
}

public class PayPalProcessor : PaymentProcessor
{
    public PayPalProcessor() : base("PayPal", 3.49m) { }

    public override PaymentResult ProcessPayment(decimal amount)
    {
        LogTransaction($"Sending PayPal payment request for {amount:C}");
        var fee = amount * (FeePercentage / 100m) + 0.49m;
        return new PaymentResult(amount, fee, "PayPal payment completed");
    }
}

// INTERVIEW ANSWER: `sealed` prevents any further inheritance. You'd seal a class when
// you want to guarantee its behavior can't be changed by subclasses — common for
// security-sensitive code or when the class wasn't designed for extension.
public sealed class CryptoProcessor : PaymentProcessor
{
    public string Network { get; }

    public CryptoProcessor(string network)
        : base($"Crypto ({network})", 1.0m)
    {
        Network = network;
    }

    public override PaymentResult ProcessPayment(decimal amount)
    {
        LogTransaction($"Broadcasting transaction on {Network} network");
        var fee = amount * (FeePercentage / 100m);
        return new PaymentResult(amount, fee, $"Crypto payment on {Network} confirmed");
    }
}

// This would NOT compile — CryptoProcessor is sealed:
// public class BitcoinProcessor : CryptoProcessor { }

public record PaymentResult(decimal Amount, decimal Fee, string Message)
{
    public decimal NetAmount => Amount - Fee;
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== INHERITANCE DEMO ===\n");

// Create different payment processors
PaymentProcessor[] processors =
[
    new PaymentProcessor("Generic", 2.0m),
    new StripeProcessor("2024-01-01"),
    new PayPalProcessor(),
    new CryptoProcessor("Ethereum")
];

var orderAmount = 99.99m;

foreach (var processor in processors)
{
    Console.WriteLine($"Processor: {processor.ProcessorName}");
    if (processor.ValidateAmount(orderAmount))
    {
        var result = processor.ProcessPayment(orderAmount);
        Console.WriteLine($"  Amount: {result.Amount:C}");
        Console.WriteLine($"  Fee: {result.Fee:C}");
        Console.WriteLine($"  Net: {result.NetAmount:C}");
        Console.WriteLine($"  Message: {result.Message}");
    }
    Console.WriteLine();
}

// INTERVIEW ANSWER: The `is` keyword checks if an object is a specific type at runtime.
// The `as` keyword attempts a cast and returns null if it fails (instead of throwing).
// With pattern matching, `is` can also extract the typed value in one step.
Console.WriteLine("--- Type Checking with is/as ---");

PaymentProcessor unknown = new StripeProcessor("2024-06-01");

// Pattern matching with `is` — preferred modern approach
if (unknown is StripeProcessor stripe)
{
    Console.WriteLine($"It's Stripe! API Version: {stripe.ApiVersion}");
}

// `as` cast — returns null if the cast fails
var paypal = unknown as PayPalProcessor;
Console.WriteLine($"PayPal cast result: {(paypal is null ? "null (not PayPal)" : paypal.ProcessorName)}");

// `is` for simple type check (no variable extraction)
Console.WriteLine($"Is it a PaymentProcessor? {unknown is PaymentProcessor}");
Console.WriteLine($"Is it sealed CryptoProcessor? {unknown is CryptoProcessor}");

Console.WriteLine("\n--- Constructor Chaining Order ---");
// Demonstrate that base constructor runs first
Console.WriteLine("Creating a StripeProcessor...");
var sp = new StripeProcessor("2024-12-01");
Console.WriteLine($"  ProcessorName (set by base): {sp.ProcessorName}");
Console.WriteLine($"  FeePercentage (set by base): {sp.FeePercentage}%");
Console.WriteLine($"  ApiVersion (set by derived): {sp.ApiVersion}");
