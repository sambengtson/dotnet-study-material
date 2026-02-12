// ============================================================================
// TOPIC: Generics
// ============================================================================
// INTERVIEW ANSWER:
// Generics let you write type-safe code that works with any type without sacrificing
// performance. Instead of using `object` and dealing with boxing/casting, you define
// type parameters (like T) that get filled in by the caller. The compiler and JIT
// generate specialized code for each type, so you get compile-time safety AND runtime
// performance. Constraints (where T : class, new(), etc.) let you restrict what types
// can be used while enabling you to call methods on T.
// ============================================================================

// --- Generic Result<T> Pattern ---

// INTERVIEW ANSWER: The Result<T> pattern is an alternative to exceptions for
// expected failures. Instead of throwing, you return a Result that's either
// Success with a value or Failure with an error message. It makes error handling
// explicit in the type system — the caller MUST handle both cases.
public class Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess { get; }

    private Result(T value) { Value = value; IsSuccess = true; }
    private Result(string error) { Error = error; IsSuccess = false; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);

    // INTERVIEW ANSWER: This Map method lets you transform a Result<T> into
    // a Result<TOut> without unwrapping. If it's a failure, the error propagates
    // automatically. This is inspired by functional programming and monadic composition.
    public Result<TOut> Map<TOut>(Func<T, TOut> transform) =>
        IsSuccess ? Result<TOut>.Success(transform(Value!)) : Result<TOut>.Failure(Error!);

    public override string ToString() =>
        IsSuccess ? $"Success({Value})" : $"Failure({Error})";
}

// --- Generic class with constraints ---

// INTERVIEW ANSWER: Constraints narrow what types can be used with a generic.
// `where T : class` means T must be a reference type. `where T : new()` means
// T must have a parameterless constructor. `where T : IComparable<T>` means T
// must implement that interface. Without constraints, you can only use methods
// from System.Object on T.
public class SortedCache<TKey, TValue>
    where TKey : notnull, IComparable<TKey>
    where TValue : class
{
    private readonly SortedDictionary<TKey, TValue> _store = [];
    private readonly int _maxSize;

    public SortedCache(int maxSize) => _maxSize = maxSize;

    public void Add(TKey key, TValue value)
    {
        if (_store.Count >= _maxSize)
        {
            // Remove the smallest key (first in sorted order)
            var oldest = _store.Keys.First();
            _store.Remove(oldest);
            Console.WriteLine($"  Evicted key: {oldest}");
        }
        _store[key] = value;
    }

    public TValue? Get(TKey key) =>
        _store.TryGetValue(key, out var value) ? value : null;

    public IEnumerable<(TKey Key, TValue Value)> GetAll() =>
        _store.Select(kv => (kv.Key, kv.Value));
}

// --- Generic method ---

public static class CollectionHelpers
{
    // INTERVIEW ANSWER: Generic methods let you parameterize a single method
    // without making the whole class generic. The type parameter is inferred
    // from the arguments in most cases — you don't have to specify it explicitly.
    public static TSource? FindOrDefault<TSource>(
        IEnumerable<TSource> collection,
        Func<TSource, bool> predicate) where TSource : class
    {
        foreach (var item in collection)
        {
            if (predicate(item)) return item;
        }
        return default;
    }

    // Generic method with multiple type parameters
    public static Dictionary<TKey, List<TValue>> GroupToDictionary<TSource, TKey, TValue>(
        IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector)
        where TKey : notnull
    {
        var result = new Dictionary<TKey, List<TValue>>();
        foreach (var item in source)
        {
            var key = keySelector(item);
            if (!result.ContainsKey(key))
                result[key] = [];
            result[key].Add(valueSelector(item));
        }
        return result;
    }
}

// --- Covariance and Contravariance ---

// INTERVIEW ANSWER: Covariance (`out T`) means you can use a more derived type
// than specified. IEnumerable<Dog> can be assigned to IEnumerable<Animal> because
// it only OUTPUTS animals. Contravariance (`in T`) is the opposite — you can use
// a less derived type. Action<Animal> can be assigned to Action<Dog> because it
// only CONSUMES animals. The `out` and `in` keywords tell the compiler which
// direction is safe.

// Covariant interface — only produces T (output)
public interface IEventProducer<out T>
{
    T Produce();
    IEnumerable<T> ProduceBatch(int count);
}

// Contravariant interface — only consumes T (input)
public interface IEventConsumer<in T>
{
    void Consume(T item);
}

public record AuditEvent(string Source, string Message, DateTime Timestamp);
public record SecurityEvent(string Source, string Message, DateTime Timestamp, string Severity)
    : AuditEvent(Source, Message, Timestamp);

public class SecurityEventProducer : IEventProducer<SecurityEvent>
{
    private int _counter;

    public SecurityEvent Produce() =>
        new("AuthService", $"Event #{++_counter}", DateTime.UtcNow, "High");

    public IEnumerable<SecurityEvent> ProduceBatch(int count) =>
        Enumerable.Range(1, count).Select(_ => Produce());
}

public class AuditEventConsumer : IEventConsumer<AuditEvent>
{
    public void Consume(AuditEvent item) =>
        Console.WriteLine($"  [Audit] {item.Source}: {item.Message} at {item.Timestamp:HH:mm:ss}");
}

// --- Generic interface with implementation ---

public interface IValidator<in T>
{
    ValidationResult Validate(T item);
}

public record ValidationResult(bool IsValid, string[] Errors)
{
    public static ValidationResult Ok() => new(true, []);
    public static ValidationResult Fail(params string[] errors) => new(false, errors);
}

public record OrderRequest(string CustomerId, decimal Amount, string Currency);

public class OrderValidator : IValidator<OrderRequest>
{
    public ValidationResult Validate(OrderRequest item)
    {
        List<string> errors = [];
        if (string.IsNullOrWhiteSpace(item.CustomerId)) errors.Add("Customer ID required");
        if (item.Amount <= 0) errors.Add("Amount must be positive");
        if (item.Currency.Length != 3) errors.Add("Currency must be 3-letter code");
        return errors.Count > 0 ? ValidationResult.Fail([.. errors]) : ValidationResult.Ok();
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== GENERICS DEMO ===\n");

// --- Result<T> Pattern ---
Console.WriteLine("--- Result<T> Pattern ---");

Result<int> ParseId(string input)
{
    if (int.TryParse(input, out var id) && id > 0)
        return Result<int>.Success(id);
    return Result<int>.Failure($"'{input}' is not a valid positive integer ID");
}

var good = ParseId("42");
var bad = ParseId("nope");
Console.WriteLine($"  Parse '42': {good}");
Console.WriteLine($"  Parse 'nope': {bad}");

// Map transforms the value inside Result
var doubled = good.Map(id => id * 2);
var failedMap = bad.Map(id => id * 2);
Console.WriteLine($"  Mapped (42 * 2): {doubled}");
Console.WriteLine($"  Mapped (failure): {failedMap}");
Console.WriteLine();

// --- Generic Class with Constraints ---
Console.WriteLine("--- SortedCache<TKey, TValue> ---");
var cache = new SortedCache<int, string>(3);
cache.Add(3, "Three");
cache.Add(1, "One");
cache.Add(2, "Two");
Console.WriteLine("  After adding 3 items:");
foreach (var (key, value) in cache.GetAll())
    Console.WriteLine($"    [{key}] = {value}");

cache.Add(4, "Four"); // Should evict key 1 (smallest)
Console.WriteLine("  After adding 4th item (max size = 3):");
foreach (var (key, value) in cache.GetAll())
    Console.WriteLine($"    [{key}] = {value}");
Console.WriteLine();

// --- Generic Methods ---
Console.WriteLine("--- Generic Methods ---");
string[] names = ["Alice", "Bob", "Charlie", "Diana"];
var found = CollectionHelpers.FindOrDefault(names, n => n.StartsWith("Ch"));
Console.WriteLine($"  FindOrDefault (starts with 'Ch'): {found}");

var orders = new[]
{
    new { Customer = "Alice", Amount = 100m, Region = "US" },
    new { Customer = "Bob", Amount = 200m, Region = "EU" },
    new { Customer = "Alice", Amount = 150m, Region = "US" },
    new { Customer = "Charlie", Amount = 300m, Region = "EU" },
};

var grouped = CollectionHelpers.GroupToDictionary(orders, o => o.Region, o => $"{o.Customer}: {o.Amount:C}");
Console.WriteLine("  Grouped by Region:");
foreach (var (region, items) in grouped)
{
    Console.WriteLine($"    {region}: [{string.Join(", ", items)}]");
}
Console.WriteLine();

// --- Covariance and Contravariance ---
Console.WriteLine("--- Covariance (out T) ---");
IEventProducer<SecurityEvent> securityProducer = new SecurityEventProducer();

// INTERVIEW ANSWER: This works because IEventProducer is covariant (out T).
// SecurityEvent derives from AuditEvent, and the producer only outputs events,
// so it's safe to treat IEventProducer<SecurityEvent> as IEventProducer<AuditEvent>.
IEventProducer<AuditEvent> auditProducer = securityProducer; // Covariance!

var events = auditProducer.ProduceBatch(3);
foreach (var evt in events)
    Console.WriteLine($"  Produced: {evt}");
Console.WriteLine();

Console.WriteLine("--- Contravariance (in T) ---");
IEventConsumer<AuditEvent> auditConsumer = new AuditEventConsumer();

// INTERVIEW ANSWER: This works because IEventConsumer is contravariant (in T).
// A consumer of AuditEvent can handle SecurityEvent (which IS an AuditEvent),
// so it's safe to treat IEventConsumer<AuditEvent> as IEventConsumer<SecurityEvent>.
IEventConsumer<SecurityEvent> securityConsumer = auditConsumer; // Contravariance!

securityConsumer.Consume(new SecurityEvent("Login", "Failed attempt", DateTime.UtcNow, "Critical"));
Console.WriteLine();

// --- Validator ---
Console.WriteLine("--- Generic Validator ---");
IValidator<OrderRequest> validator = new OrderValidator();

OrderRequest[] requests =
[
    new("cust-1", 99.99m, "USD"),
    new("", -10m, "X"),
    new("cust-2", 50m, "EUR"),
];

foreach (var req in requests)
{
    var result = validator.Validate(req);
    Console.WriteLine($"  {req} → {(result.IsValid ? "Valid" : $"Invalid: [{string.Join(", ", result.Errors)}]")}");
}
