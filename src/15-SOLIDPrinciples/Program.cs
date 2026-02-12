// ============================================================================
// TOPIC: SOLID Principles
// ============================================================================
// INTERVIEW ANSWER:
// SOLID is five design principles that help you write maintainable, flexible code:
// S — Single Responsibility: each class has one reason to change
// O — Open/Closed: open for extension, closed for modification
// L — Liskov Substitution: subtypes must be usable wherever the base type is expected
// I — Interface Segregation: don't force classes to implement interfaces they don't use
// D — Dependency Inversion: depend on abstractions, not concrete implementations
// They're guidelines, not laws — the goal is code that's easy to test, extend, and reason about.
// ============================================================================

// =============================================
// S — SINGLE RESPONSIBILITY PRINCIPLE
// =============================================

// INTERVIEW ANSWER: SRP says a class should have one reason to change. If a class
// handles both order processing AND email sending AND logging, any change to email
// formatting forces you to modify and retest the order class too. Separate concerns
// into focused classes.

// BAD: This class does too many things
public class BadOrderService
{
    public void ProcessOrder(string orderId, decimal amount)
    {
        // Validates order (concern 1)
        if (amount <= 0) throw new ArgumentException("Invalid amount");

        // Saves to database (concern 2)
        Console.WriteLine($"  [BAD] Saved order {orderId} to DB");

        // Sends email (concern 3)
        Console.WriteLine($"  [BAD] Sent confirmation email for {orderId}");

        // Logs (concern 4)
        Console.WriteLine($"  [BAD] Logged order {orderId}");
    }
}

// GOOD: Each class has a single responsibility
public class OrderValidator
{
    public bool Validate(string orderId, decimal amount)
    {
        if (amount <= 0) { Console.WriteLine($"  [SRP] Validation failed: amount must be positive"); return false; }
        Console.WriteLine($"  [SRP] Order {orderId} validated");
        return true;
    }
}

public class OrderRepository
{
    public void Save(string orderId, decimal amount) =>
        Console.WriteLine($"  [SRP] Saved order {orderId} ({amount:C}) to database");
}

public class OrderNotifier
{
    public void SendConfirmation(string orderId) =>
        Console.WriteLine($"  [SRP] Sent confirmation for {orderId}");
}

public class GoodOrderService(OrderValidator validator, OrderRepository repo, OrderNotifier notifier)
{
    public void ProcessOrder(string orderId, decimal amount)
    {
        if (!validator.Validate(orderId, amount)) return;
        repo.Save(orderId, amount);
        notifier.SendConfirmation(orderId);
    }
}

// =============================================
// O — OPEN/CLOSED PRINCIPLE
// =============================================

// INTERVIEW ANSWER: OCP says code should be open for extension but closed for
// modification. Instead of adding if/else branches every time you add a new
// discount type, define an interface and add new implementations. The existing
// code doesn't change — you just plug in new classes.

// BAD: Adding a new discount type requires modifying this method
public class BadDiscountCalculator
{
    public decimal Calculate(string discountType, decimal price) => discountType switch
    {
        "percentage" => price * 0.10m,
        "fixed" => 5m,
        // Adding "buy2get1" requires modifying THIS class
        _ => 0m
    };
}

// GOOD: New discount types are new classes, not modifications
public interface IDiscountStrategy
{
    string Name { get; }
    decimal Calculate(decimal price);
}

public class PercentageDiscount(decimal percent) : IDiscountStrategy
{
    public string Name => $"{percent}% off";
    public decimal Calculate(decimal price) => price * (percent / 100m);
}

public class FixedDiscount(decimal amount) : IDiscountStrategy
{
    public string Name => $"{amount:C} off";
    public decimal Calculate(decimal price) => Math.Min(amount, price);
}

public class BuyTwoGetOneDiscount : IDiscountStrategy
{
    public string Name => "Buy 2, Get 1 Free";
    public decimal Calculate(decimal price) => price / 3m; // One-third off
}

public class DiscountCalculator
{
    public decimal Apply(decimal price, IDiscountStrategy strategy)
    {
        var discount = strategy.Calculate(price);
        Console.WriteLine($"  [OCP] {strategy.Name}: {price:C} - {discount:C} = {price - discount:C}");
        return price - discount;
    }
}

// =============================================
// L — LISKOV SUBSTITUTION PRINCIPLE
// =============================================

// INTERVIEW ANSWER: LSP says that any subtype should be usable wherever the base
// type is expected without breaking behavior. The classic violation is Square
// inheriting from Rectangle. A more real-world example: if a ReadOnlyFileSystem
// inherits from FileSystem and throws on Write(), that violates LSP — code
// expecting a FileSystem would break.

// BAD: ReadOnlyStorage violates LSP — it inherits Write but throws
public class BadStorage
{
    public virtual string Read(string key) => $"data for {key}";
    public virtual void Write(string key, string value) =>
        Console.WriteLine($"  [BAD] Wrote {key}");
}

public class BadReadOnlyStorage : BadStorage
{
    public override void Write(string key, string value) =>
        throw new NotSupportedException("Read-only storage!"); // Violates LSP!
}

// GOOD: Separate interfaces so read-only storage doesn't promise write capability
public interface IReadableStorage
{
    string Read(string key);
}

public interface IWritableStorage : IReadableStorage
{
    void Write(string key, string value);
}

public class FullStorage : IWritableStorage
{
    private readonly Dictionary<string, string> _data = [];
    public string Read(string key) => _data.GetValueOrDefault(key, "<not found>");
    public void Write(string key, string value)
    {
        _data[key] = value;
        Console.WriteLine($"  [LSP] Wrote '{key}' = '{value}'");
    }
}

public class CacheStorage : IReadableStorage
{
    public string Read(string key)
    {
        Console.WriteLine($"  [LSP] Read '{key}' from cache");
        return $"cached-{key}";
    }
    // No Write method — not promised, not needed
}

// =============================================
// I — INTERFACE SEGREGATION PRINCIPLE
// =============================================

// INTERVIEW ANSWER: ISP says don't force classes to implement interface members
// they don't use. If your interface has 10 methods and some implementations only
// need 3, split it into smaller, focused interfaces. Clients should depend on
// the narrowest interface that meets their needs.

// BAD: One fat interface forces all implementors to deal with everything
public interface IBadWorker
{
    void WriteCode();
    void ReviewCode();
    void ManageTeam();
    void AttendMeetings();
    void PrepareReports();
}

// GOOD: Segregated interfaces — each role implements only what it does
public interface ICoder
{
    void WriteCode();
    void ReviewCode();
}

public interface IManager
{
    void ManageTeam();
    void PrepareReports();
}

public interface IMeetingAttendee
{
    void AttendMeetings();
}

public class Developer : ICoder, IMeetingAttendee
{
    public void WriteCode() => Console.WriteLine("  [ISP] Developer: writing code");
    public void ReviewCode() => Console.WriteLine("  [ISP] Developer: reviewing PRs");
    public void AttendMeetings() => Console.WriteLine("  [ISP] Developer: attending standup");
}

public class TeamLead : ICoder, IManager, IMeetingAttendee
{
    public void WriteCode() => Console.WriteLine("  [ISP] Lead: writing code (sometimes)");
    public void ReviewCode() => Console.WriteLine("  [ISP] Lead: reviewing code");
    public void ManageTeam() => Console.WriteLine("  [ISP] Lead: managing team");
    public void PrepareReports() => Console.WriteLine("  [ISP] Lead: preparing reports");
    public void AttendMeetings() => Console.WriteLine("  [ISP] Lead: attending meetings");
}

// =============================================
// D — DEPENDENCY INVERSION PRINCIPLE
// =============================================

// INTERVIEW ANSWER: DIP says high-level modules shouldn't depend on low-level
// modules — both should depend on abstractions. If your OrderService directly
// creates a SqlDatabase instance, it's tightly coupled to SQL. Instead, depend
// on an IDatabase interface, and let the caller (or DI container) provide the
// implementation.

// BAD: High-level service directly depends on low-level implementation
public class BadNotificationService
{
    private readonly BadSmtpClient _smtp = new(); // Tightly coupled!

    public void Notify(string message) => _smtp.Send(message);
}

public class BadSmtpClient
{
    public void Send(string message) =>
        Console.WriteLine($"  [BAD] SMTP sent: {message}");
}

// GOOD: Both depend on the abstraction
public interface IMessageChannel
{
    void Send(string recipient, string message);
}

public class SmtpChannel : IMessageChannel
{
    public void Send(string recipient, string message) =>
        Console.WriteLine($"  [DIP] Email to {recipient}: {message}");
}

public class SlackChannel : IMessageChannel
{
    public void Send(string recipient, string message) =>
        Console.WriteLine($"  [DIP] Slack to #{recipient}: {message}");
}

public class NotificationService(IMessageChannel channel)
{
    public void Notify(string recipient, string message) =>
        channel.Send(recipient, message);
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== SOLID PRINCIPLES DEMO ===\n");

// --- S: Single Responsibility ---
Console.WriteLine("=== S — Single Responsibility ===\n");
Console.WriteLine("BAD (one class does everything):");
var badService = new BadOrderService();
badService.ProcessOrder("ORD-1", 99.99m);
Console.WriteLine();

Console.WriteLine("GOOD (separated concerns):");
var goodService = new GoodOrderService(new OrderValidator(), new OrderRepository(), new OrderNotifier());
goodService.ProcessOrder("ORD-2", 149.99m);
Console.WriteLine();

// --- O: Open/Closed ---
Console.WriteLine("=== O — Open/Closed ===\n");
var calc = new DiscountCalculator();
calc.Apply(100m, new PercentageDiscount(15));
calc.Apply(100m, new FixedDiscount(20));
calc.Apply(100m, new BuyTwoGetOneDiscount());
Console.WriteLine();

// --- L: Liskov Substitution ---
Console.WriteLine("=== L — Liskov Substitution ===\n");
Console.WriteLine("BAD (ReadOnlyStorage throws on Write):");
BadStorage storage = new BadReadOnlyStorage();
try { storage.Write("key", "value"); }
catch (NotSupportedException ex) { Console.WriteLine($"  Caught: {ex.Message}"); }
Console.WriteLine();

Console.WriteLine("GOOD (separate interfaces):");
IWritableStorage fullStore = new FullStorage();
fullStore.Write("config", "value");
Console.WriteLine($"  Read: {fullStore.Read("config")}");
IReadableStorage cacheStore = new CacheStorage();
Console.WriteLine($"  Cache read: {cacheStore.Read("config")}");
Console.WriteLine();

// --- I: Interface Segregation ---
Console.WriteLine("=== I — Interface Segregation ===\n");
Console.WriteLine("Developer (ICoder + IMeetingAttendee):");
var dev = new Developer();
dev.WriteCode();
dev.ReviewCode();
dev.AttendMeetings();
Console.WriteLine();

Console.WriteLine("Team Lead (ICoder + IManager + IMeetingAttendee):");
var lead = new TeamLead();
lead.WriteCode();
lead.ManageTeam();
lead.AttendMeetings();
Console.WriteLine();

// --- D: Dependency Inversion ---
Console.WriteLine("=== D — Dependency Inversion ===\n");
Console.WriteLine("Swap channels without changing NotificationService:");
var emailNotifier = new NotificationService(new SmtpChannel());
emailNotifier.Notify("alice@test.com", "Your order shipped!");

var slackNotifier = new NotificationService(new SlackChannel());
slackNotifier.Notify("engineering", "Deploy complete!");
