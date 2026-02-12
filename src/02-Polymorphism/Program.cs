// ============================================================================
// TOPIC: Polymorphism
// ============================================================================
// INTERVIEW ANSWER:
// Polymorphism means "many forms" — it lets you treat objects of different types
// through a common interface or base class, and each type responds in its own way.
// C# has two kinds: runtime polymorphism (virtual/override/abstract — the actual
// method called is determined at runtime based on the object's real type) and
// compile-time polymorphism (method overloading and operator overloading — the
// compiler picks the right method based on parameter types at compile time).
// ============================================================================

using System.Diagnostics.CodeAnalysis;

// ---- RUNTIME POLYMORPHISM (virtual/override/abstract) ----

// INTERVIEW ANSWER: An abstract class can't be instantiated directly. It defines
// a contract where some methods MUST be implemented by derived classes (abstract
// methods) while others CAN be overridden (virtual methods). It's the mechanism
// for runtime polymorphism — the CLR looks up the actual type's method table at
// runtime to find the right implementation.
public abstract class NotificationService
{
    public string ServiceName { get; }

    protected NotificationService(string serviceName) => ServiceName = serviceName;

    // Abstract — every derived class MUST implement this
    public abstract Task<bool> SendAsync(string recipient, string message);

    // Virtual — derived classes CAN override this, but there's a default
    public virtual string FormatMessage(string message) => message.Trim();

    // Non-virtual — same for all notification types
    public void LogSend(string recipient) =>
        Console.WriteLine($"  [{ServiceName}] Sending to: {recipient}");
}

public class EmailNotification : NotificationService
{
    public EmailNotification() : base("Email") { }

    public override async Task<bool> SendAsync(string recipient, string message)
    {
        LogSend(recipient);
        await Task.Delay(10); // Simulate network call
        Console.WriteLine($"  [{ServiceName}] Subject: Notification | Body: {FormatMessage(message)}");
        return true;
    }

    // Override the virtual method to add email-specific formatting
    public override string FormatMessage(string message) =>
        $"<html><body>{base.FormatMessage(message)}</body></html>";
}

public class SmsNotification : NotificationService
{
    private const int MaxLength = 160;

    public SmsNotification() : base("SMS") { }

    public override async Task<bool> SendAsync(string recipient, string message)
    {
        LogSend(recipient);
        await Task.Delay(10);
        var formatted = FormatMessage(message);
        Console.WriteLine($"  [{ServiceName}] Message ({formatted.Length} chars): {formatted}");
        return true;
    }

    // SMS has length limits — truncate the message
    public override string FormatMessage(string message)
    {
        var clean = base.FormatMessage(message);
        return clean.Length > MaxLength ? clean[..MaxLength] : clean;
    }
}

public class SlackNotification : NotificationService
{
    public string Channel { get; }

    public SlackNotification(string channel) : base("Slack") => Channel = channel;

    public override async Task<bool> SendAsync(string recipient, string message)
    {
        LogSend($"{recipient} in #{Channel}");
        await Task.Delay(10);
        Console.WriteLine($"  [{ServiceName}] #{Channel}: {FormatMessage(message)}");
        return true;
    }
}

// ---- COMPILE-TIME POLYMORPHISM (method overloading, operator overloading) ----

// INTERVIEW ANSWER: Method overloading is compile-time polymorphism — you have
// multiple methods with the same name but different parameter signatures. The
// compiler picks which one to call based on the arguments you pass. This is
// resolved at compile time, not runtime.
public class PricingCalculator
{
    // Overload 1: simple price
    public static Money Calculate(decimal basePrice) =>
        new(basePrice, 0m, basePrice);

    // Overload 2: price with discount percentage
    public static Money Calculate(decimal basePrice, decimal discountPercent)
    {
        var discount = basePrice * (discountPercent / 100m);
        return new(basePrice, discount, basePrice - discount);
    }

    // Overload 3: price with coupon code lookup
    public static Money Calculate(decimal basePrice, string couponCode)
    {
        var discount = couponCode.ToUpperInvariant() switch
        {
            "SAVE10" => basePrice * 0.10m,
            "HALF" => basePrice * 0.50m,
            "VIP" => basePrice * 0.25m,
            _ => 0m
        };
        return new(basePrice, discount, basePrice - discount);
    }
}

// INTERVIEW ANSWER: Operator overloading lets you define how operators (+, -, ==, etc.)
// work with your custom types. The compiler resolves which operator implementation to
// call based on the operand types — that's why it's compile-time polymorphism.
public readonly struct Money : IEquatable<Money>
{
    public decimal BasePrice { get; }
    public decimal Discount { get; }
    public decimal FinalPrice { get; }

    public Money(decimal basePrice, decimal discount, decimal finalPrice)
    {
        BasePrice = basePrice;
        Discount = discount;
        FinalPrice = finalPrice;
    }

    // Operator overloading
    public static Money operator +(Money a, Money b) =>
        new(a.BasePrice + b.BasePrice, a.Discount + b.Discount, a.FinalPrice + b.FinalPrice);

    public static Money operator *(Money m, int quantity) =>
        new(m.BasePrice * quantity, m.Discount * quantity, m.FinalPrice * quantity);

    public static bool operator ==(Money a, Money b) => a.FinalPrice == b.FinalPrice;
    public static bool operator !=(Money a, Money b) => !(a == b);

    public bool Equals(Money other) => FinalPrice == other.FinalPrice;
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Money m && Equals(m);
    public override int GetHashCode() => FinalPrice.GetHashCode();
    public override string ToString() => $"Base: {BasePrice:C}, Discount: {Discount:C}, Final: {FinalPrice:C}";
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== POLYMORPHISM DEMO ===\n");

// --- Runtime Polymorphism ---
Console.WriteLine("--- Runtime Polymorphism (virtual/override/abstract) ---");
Console.WriteLine("Sending the same message through different notification services:\n");

// INTERVIEW ANSWER: This is the power of runtime polymorphism — we have a collection
// of NotificationService references, but each one behaves differently when we call
// SendAsync(). The CLR dispatches to the correct override based on the actual type.
NotificationService[] services =
[
    new EmailNotification(),
    new SmsNotification(),
    new SlackNotification("engineering")
];

foreach (var service in services)
{
    Console.WriteLine($"Using {service.ServiceName}:");
    await service.SendAsync("user@example.com", "  Your deployment succeeded!  ");
    Console.WriteLine();
}

// --- Compile-Time Polymorphism ---
Console.WriteLine("--- Compile-Time Polymorphism (method overloading) ---\n");

var price1 = PricingCalculator.Calculate(100m);
var price2 = PricingCalculator.Calculate(100m, 15m);
var price3 = PricingCalculator.Calculate(100m, "SAVE10");

Console.WriteLine($"Base price only:      {price1}");
Console.WriteLine($"With 15% discount:    {price2}");
Console.WriteLine($"With SAVE10 coupon:   {price3}");

Console.WriteLine("\n--- Operator Overloading ---\n");

var itemA = PricingCalculator.Calculate(29.99m, "VIP");
var itemB = PricingCalculator.Calculate(49.99m, 10m);
var total = itemA + itemB;
var bulk = itemA * 3;

Console.WriteLine($"Item A:  {itemA}");
Console.WriteLine($"Item B:  {itemB}");
Console.WriteLine($"A + B:   {total}");
Console.WriteLine($"A x 3:   {bulk}");
Console.WriteLine($"A == A:  {itemA == itemA}");
Console.WriteLine($"A == B:  {itemA == itemB}");
