// ============================================================================
// TOPIC: Interfaces
// ============================================================================
// INTERVIEW ANSWER:
// An interface defines a contract — a set of members that implementing classes
// must provide. Unlike abstract classes, a class can implement multiple interfaces,
// which is how C# achieves a form of multiple inheritance. Since C# 8, interfaces
// can also have default method implementations, which lets you add new members to
// an interface without breaking existing implementors.
// ============================================================================

// --- Core interfaces for a notification system ---

// INTERVIEW ANSWER: Interfaces are pure contracts. They say WHAT a type can do
// without saying HOW. This INotificationSender could be email, SMS, push, Slack —
// calling code doesn't care.
public interface INotificationSender
{
    string ProviderName { get; }
    Task<bool> SendAsync(string recipient, string message);
}

// INTERVIEW ANSWER: You can implement multiple interfaces. This is how you compose
// capabilities. A class might be both an INotificationSender AND an IHealthCheck.
public interface IHealthCheck
{
    Task<HealthStatus> CheckHealthAsync();
}

public record HealthStatus(bool IsHealthy, string Details);

// Generic interface with constraints
// INTERVIEW ANSWER: Generic interfaces let you create type-safe contracts.
// The constraint `where T : class` means T must be a reference type. This
// prevents misuse while keeping the interface flexible.
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task SaveAsync(T entity);
    Task DeleteAsync(string id);
}

// --- Default Interface Methods (C# 8+) ---

// INTERVIEW ANSWER: Default interface methods let you add new methods to an
// interface with a default implementation. Existing classes that implement the
// interface don't break — they automatically get the default behavior. You can
// override it if you need something different. This was a big deal for library
// authors who needed to evolve interfaces without breaking consumers.
public interface IMessageFormatter
{
    string Format(string message);

    // Default implementation — implementors get this for free
    string FormatWithTimestamp(string message) =>
        $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {Format(message)}";

    // Another default method
    string FormatBatch(IEnumerable<string> messages) =>
        string.Join(Environment.NewLine, messages.Select(Format));
}

// --- Implementations ---

public class EmailSender : INotificationSender, IHealthCheck, IMessageFormatter
{
    public string ProviderName => "Email";

    public async Task<bool> SendAsync(string recipient, string message)
    {
        await Task.Delay(10);
        Console.WriteLine($"  [Email] To: {recipient} | Body: {message}");
        return true;
    }

    public Task<HealthStatus> CheckHealthAsync() =>
        Task.FromResult(new HealthStatus(true, "SMTP server reachable"));

    public string Format(string message) => $"<p>{message}</p>";
}

public class SmsSender : INotificationSender, IHealthCheck, IMessageFormatter
{
    public string ProviderName => "SMS";

    public async Task<bool> SendAsync(string recipient, string message)
    {
        await Task.Delay(10);
        var truncated = message.Length > 160 ? message[..160] : message;
        Console.WriteLine($"  [SMS] To: {recipient} | Message: {truncated}");
        return true;
    }

    public Task<HealthStatus> CheckHealthAsync() =>
        Task.FromResult(new HealthStatus(true, "SMS gateway connected"));

    public string Format(string message) => message.ToUpperInvariant();

    // Override the default method
    public string FormatWithTimestamp(string message) =>
        $"{DateTime.UtcNow:HH:mm} {Format(message)}";
}

// --- Explicit Interface Implementation ---

// INTERVIEW ANSWER: Explicit interface implementation means the method is ONLY
// accessible when the object is referenced through the interface type, not through
// the concrete class. This is useful when a class implements two interfaces that
// have conflicting method names, or when you want to hide interface members from
// the class's public API.
public interface IPublicApi
{
    string GetData();
}

public interface IInternalApi
{
    string GetData(); // Same name, different intent
}

public class DataService : IPublicApi, IInternalApi
{
    // Explicit implementation of IPublicApi.GetData
    string IPublicApi.GetData() => "Public: sanitized, safe data";

    // Explicit implementation of IInternalApi.GetData
    string IInternalApi.GetData() => "Internal: raw data with sensitive details";

    // Regular public method — the class's own API
    public string GetSummary() => "DataService summary";
}

// --- IDisposable Pattern ---

// INTERVIEW ANSWER: IDisposable is the standard pattern for deterministic cleanup
// of unmanaged resources (file handles, database connections, network sockets).
// The `using` statement calls Dispose() automatically when the scope ends, even
// if an exception occurs. It's critical for preventing resource leaks.
public class DatabaseConnection : IDisposable
{
    public string ConnectionString { get; }
    public bool IsOpen { get; private set; }
    private bool _disposed;

    public DatabaseConnection(string connectionString)
    {
        ConnectionString = connectionString;
        IsOpen = true;
        Console.WriteLine($"  [DB] Connection opened: {connectionString}");
    }

    public string Query(string sql)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return $"Results for: {sql}";
    }

    // INTERVIEW ANSWER: The dispose pattern with a protected virtual Dispose(bool)
    // method lets derived classes participate in cleanup. The bool distinguishes
    // between explicit disposal (true) and finalizer cleanup (false).
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Clean up managed resources
                IsOpen = false;
                Console.WriteLine($"  [DB] Connection closed: {ConnectionString}");
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

// --- Generic interface with a simple repository ---

public record User(string Id, string Name, string Email);

public class InMemoryUserRepository : IRepository<User>
{
    private readonly Dictionary<string, User> _store = [];

    public Task<User?> GetByIdAsync(string id) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public Task<IReadOnlyList<User>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<User>>(_store.Values.ToList());

    public Task SaveAsync(User entity)
    {
        _store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        _store.Remove(id);
        return Task.CompletedTask;
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== INTERFACES DEMO ===\n");

// --- Multiple Interface Implementation ---
Console.WriteLine("--- Multiple Interface Implementation ---");
INotificationSender[] senders = [new EmailSender(), new SmsSender()];

foreach (var sender in senders)
{
    Console.WriteLine($"Provider: {sender.ProviderName}");
    await sender.SendAsync("user@test.com", "Your order has shipped!");

    // Check if this sender also implements IHealthCheck
    if (sender is IHealthCheck healthCheck)
    {
        var status = await healthCheck.CheckHealthAsync();
        Console.WriteLine($"  Health: {(status.IsHealthy ? "OK" : "FAIL")} — {status.Details}");
    }
    Console.WriteLine();
}

// --- Default Interface Methods ---
Console.WriteLine("--- Default Interface Methods ---");
IMessageFormatter emailFormatter = new EmailSender();
IMessageFormatter smsFormatter = new SmsSender();

Console.WriteLine($"  Email format: {emailFormatter.Format("Hello")}");
Console.WriteLine($"  Email with timestamp: {emailFormatter.FormatWithTimestamp("Hello")}");
Console.WriteLine($"  SMS format: {smsFormatter.Format("Hello")}");
Console.WriteLine($"  SMS with timestamp: {smsFormatter.FormatWithTimestamp("Hello")}");

// Batch formatting uses the default implementation for both
string[] messages = ["Order confirmed", "Payment received", "Shipping soon"];
Console.WriteLine($"\n  Email batch:\n{emailFormatter.FormatBatch(messages)}");
Console.WriteLine();

// --- Explicit Interface Implementation ---
Console.WriteLine("--- Explicit Interface Implementation ---");
var dataService = new DataService();

// Through the class reference — only GetSummary is visible
Console.WriteLine($"  Class: {dataService.GetSummary()}");
// dataService.GetData(); // Won't compile — GetData is explicit

// Through interface references — each sees its own GetData
IPublicApi publicApi = dataService;
IInternalApi internalApi = dataService;
Console.WriteLine($"  IPublicApi: {publicApi.GetData()}");
Console.WriteLine($"  IInternalApi: {internalApi.GetData()}");
Console.WriteLine();

// --- IDisposable ---
Console.WriteLine("--- IDisposable / using ---");
using (var db = new DatabaseConnection("Server=localhost;DB=myapp"))
{
    var result = db.Query("SELECT * FROM Users");
    Console.WriteLine($"  {result}");
} // Dispose called automatically here

Console.WriteLine();

// Using declaration (C# 8) — disposes when the variable goes out of scope
Console.WriteLine("--- Using declaration (C# 8) ---");
{
    using var db2 = new DatabaseConnection("Server=localhost;DB=analytics");
    Console.WriteLine($"  {db2.Query("SELECT COUNT(*) FROM Events")}");
} // db2.Dispose() called here
Console.WriteLine();

// --- Generic Interface (IRepository<T>) ---
Console.WriteLine("--- Generic Interface: IRepository<User> ---");
IRepository<User> repo = new InMemoryUserRepository();

await repo.SaveAsync(new User("1", "Alice", "alice@test.com"));
await repo.SaveAsync(new User("2", "Bob", "bob@test.com"));
await repo.SaveAsync(new User("3", "Charlie", "charlie@test.com"));

var user = await repo.GetByIdAsync("2");
Console.WriteLine($"  Found: {user}");

var all = await repo.GetAllAsync();
Console.WriteLine($"  Total users: {all.Count}");
foreach (var u in all)
    Console.WriteLine($"    {u}");

await repo.DeleteAsync("2");
var afterDelete = await repo.GetAllAsync();
Console.WriteLine($"  After delete: {afterDelete.Count} users");
