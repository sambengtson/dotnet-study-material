// ============================================================================
// TOPIC: Singleton Pattern
// ============================================================================
// INTERVIEW ANSWER:
// The Singleton pattern ensures a class has exactly one instance and provides a
// global point of access to it. In C# there are several thread-safe approaches:
// static readonly fields, Lazy<T>, or double-checked locking. However, singletons
// are often considered an anti-pattern because they introduce hidden global state,
// make unit testing harder (you can't easily mock them), and create tight coupling.
// In modern C#, dependency injection with a Singleton lifetime is almost always
// the better approach.
// ============================================================================

// --- Approach 1: Static readonly (simplest, thread-safe) ---

// INTERVIEW ANSWER: The static readonly approach is thread-safe because the CLR
// guarantees that static constructors run exactly once, even under concurrent
// access. The downside is you can't control WHEN it gets created — it happens
// the first time any static member is accessed.
public class AppConfiguration
{
    private static readonly AppConfiguration _instance = new();

    // Private constructor prevents external instantiation
    private AppConfiguration()
    {
        Console.WriteLine("  [AppConfig] Instance created (static readonly)");
        Settings = new Dictionary<string, string>
        {
            ["AppName"] = "MyApp",
            ["Version"] = "2.1.0",
            ["Environment"] = "Production"
        };
    }

    public static AppConfiguration Instance => _instance;

    public Dictionary<string, string> Settings { get; }

    public string Get(string key) =>
        Settings.TryGetValue(key, out var value) ? value : $"<{key} not found>";
}

// --- Approach 2: Lazy<T> (recommended for most cases) ---

// INTERVIEW ANSWER: Lazy<T> is the modern, recommended approach. It's thread-safe
// by default, truly lazy (created only on first access), and the intent is clear
// from the code. The LazyThreadSafetyMode parameter lets you control the exact
// threading behavior.
public class ConnectionPool
{
    private static readonly Lazy<ConnectionPool> _instance = new(
        () => new ConnectionPool(),
        LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly List<string> _connections = [];
    private readonly int _maxSize;

    private ConnectionPool()
    {
        _maxSize = 10;
        Console.WriteLine($"  [ConnectionPool] Instance created (Lazy<T>), max size: {_maxSize}");
    }

    public static ConnectionPool Instance => _instance.Value;

    public string AcquireConnection()
    {
        var connId = $"conn-{_connections.Count + 1}";
        if (_connections.Count < _maxSize)
        {
            _connections.Add(connId);
            return connId;
        }
        throw new InvalidOperationException("Connection pool exhausted");
    }

    public int ActiveConnections => _connections.Count;
}

// --- Approach 3: Double-checked locking (manual, educational) ---

// INTERVIEW ANSWER: Double-checked locking was the traditional way before Lazy<T>
// existed. The first check avoids the lock overhead on subsequent calls. The second
// check (inside the lock) prevents multiple threads from creating instances
// simultaneously. The `volatile` keyword ensures the field isn't cached in CPU
// registers across threads. In practice, just use Lazy<T>.
public class Logger
{
    private static volatile Logger? _instance;
    private static readonly object _lock = new();
    private readonly List<string> _entries = [];

    private Logger()
    {
        Console.WriteLine("  [Logger] Instance created (double-checked locking)");
    }

    public static Logger Instance
    {
        get
        {
            if (_instance is null)                // First check (no lock)
            {
                lock (_lock)
                {
                    _instance ??= new Logger();   // Second check (inside lock)
                }
            }
            return _instance;
        }
    }

    public void Log(string message)
    {
        var entry = $"[{DateTime.UtcNow:HH:mm:ss}] {message}";
        _entries.Add(entry);
    }

    public IReadOnlyList<string> GetEntries() => _entries.AsReadOnly();
}

// --- Why Singletons Are Often an Anti-Pattern ---

// INTERVIEW ANSWER: Singletons are problematic because:
// 1. Hidden dependencies — code uses AppConfig.Instance instead of receiving it,
//    so you can't see dependencies from the constructor signature.
// 2. Hard to test — you can't substitute a mock/fake in unit tests.
// 3. Global mutable state — changes affect everything, hard to reason about.
// 4. Lifecycle issues — you don't control when it's created or destroyed.
//
// The fix is DI with singleton lifetime: you register the service as singleton
// in the container, and it gets injected as a constructor parameter. You get
// the single-instance behavior without the drawbacks.

// This is how you'd do it properly with DI (conceptual):
public interface IAppSettings
{
    string Get(string key);
}

// This class doesn't enforce singleton itself — the DI container manages the lifetime
public class AppSettings : IAppSettings
{
    private readonly Dictionary<string, string> _settings;

    public AppSettings(Dictionary<string, string> settings)
    {
        _settings = settings;
        Console.WriteLine("  [AppSettings] Created via DI (no singleton enforcement)");
    }

    public string Get(string key) =>
        _settings.TryGetValue(key, out var value) ? value : $"<{key} not found>";
}

// A service that DEPENDS on settings instead of reaching for a global
public class UserService(IAppSettings settings)
{
    public string GetWelcomeMessage(string userName) =>
        $"Welcome to {settings.Get("AppName")} v{settings.Get("Version")}, {userName}!";
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== SINGLETON PATTERN DEMO ===\n");

// --- Approach 1: Static readonly ---
Console.WriteLine("--- Approach 1: Static Readonly ---");
var config1 = AppConfiguration.Instance;
var config2 = AppConfiguration.Instance;
Console.WriteLine($"  Same instance? {ReferenceEquals(config1, config2)}");
Console.WriteLine($"  AppName: {config1.Get("AppName")}");
Console.WriteLine($"  Version: {config1.Get("Version")}");
Console.WriteLine();

// --- Approach 2: Lazy<T> ---
Console.WriteLine("--- Approach 2: Lazy<T> ---");
var pool1 = ConnectionPool.Instance;
var pool2 = ConnectionPool.Instance;
Console.WriteLine($"  Same instance? {ReferenceEquals(pool1, pool2)}");
var conn1 = pool1.AcquireConnection();
var conn2 = pool1.AcquireConnection();
Console.WriteLine($"  Acquired: {conn1}, {conn2}");
Console.WriteLine($"  Active connections: {pool1.ActiveConnections}");
Console.WriteLine();

// --- Approach 3: Double-checked locking ---
Console.WriteLine("--- Approach 3: Double-Checked Locking ---");
var log1 = Logger.Instance;
var log2 = Logger.Instance;
Console.WriteLine($"  Same instance? {ReferenceEquals(log1, log2)}");
log1.Log("Application started");
log2.Log("Processing request"); // Same instance, same list
Console.WriteLine($"  Entries ({log1.GetEntries().Count}):");
foreach (var entry in log1.GetEntries())
    Console.WriteLine($"    {entry}");
Console.WriteLine();

// --- Thread safety test ---
Console.WriteLine("--- Thread Safety Test (Lazy<T>) ---");
var tasks = Enumerable.Range(0, 10).Select(_ =>
    Task.Run(() => ConnectionPool.Instance.AcquireConnection()));

var connections = await Task.WhenAll(tasks);
Console.WriteLine($"  All 10 threads got connections from same pool.");
Console.WriteLine($"  Total active: {ConnectionPool.Instance.ActiveConnections}");
Console.WriteLine();

// --- Better Alternative: DI ---
Console.WriteLine("--- Better Alternative: Dependency Injection ---");
var settings = new AppSettings(new Dictionary<string, string>
{
    ["AppName"] = "MyApp",
    ["Version"] = "3.0.0"
});
var userService = new UserService(settings);
Console.WriteLine($"  {userService.GetWelcomeMessage("Alice")}");

Console.WriteLine();
Console.WriteLine("  In real code, you'd register AppSettings as singleton in DI:");
Console.WriteLine("  services.AddSingleton<IAppSettings>(new AppSettings(config));");
Console.WriteLine("  Then inject IAppSettings via constructor — testable, clean, explicit.");
