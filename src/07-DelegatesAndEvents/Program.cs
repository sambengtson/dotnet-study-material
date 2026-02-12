// ============================================================================
// TOPIC: Delegates and Events
// ============================================================================
// INTERVIEW ANSWER:
// A delegate is a type-safe function pointer — it holds a reference to a method
// with a specific signature. C# provides built-in delegates like Action<T> (void
// return), Func<T,TResult> (with return value), and Predicate<T> (returns bool).
// Events are built on delegates but add restrictions: only the declaring class
// can invoke them. This is the foundation for the observer pattern in C# and
// enables loose coupling between publishers and subscribers.
// ============================================================================

// --- Custom delegate type ---

// INTERVIEW ANSWER: You'd define a custom delegate when the built-in ones don't
// clearly express your intent, or when you want a named type for documentation
// purposes. In practice, most code uses Action/Func because they're more concise.
public delegate bool RetryPolicy(int attemptNumber, Exception exception);

// --- Event-based order processing system ---

public class OrderEventArgs(string orderId, decimal amount, string status) : EventArgs
{
    public string OrderId { get; } = orderId;
    public decimal Amount { get; } = amount;
    public string Status { get; } = status;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class OrderProcessor
{
    // INTERVIEW ANSWER: Events use EventHandler<T> by convention. The `event`
    // keyword restricts external code to only += and -= (subscribe/unsubscribe).
    // Only the OrderProcessor itself can invoke (raise) these events. This
    // prevents external code from faking events.
    public event EventHandler<OrderEventArgs>? OrderReceived;
    public event EventHandler<OrderEventArgs>? OrderProcessed;
    public event EventHandler<OrderEventArgs>? OrderFailed;

    public async Task ProcessOrderAsync(string orderId, decimal amount)
    {
        Console.WriteLine($"\n  Processing order {orderId} for {amount:C}...");

        // Raise OrderReceived
        OrderReceived?.Invoke(this, new OrderEventArgs(orderId, amount, "Received"));

        await Task.Delay(50); // Simulate processing

        if (amount > 10_000m)
        {
            OrderFailed?.Invoke(this, new OrderEventArgs(orderId, amount, "Amount exceeds limit"));
            return;
        }

        OrderProcessed?.Invoke(this, new OrderEventArgs(orderId, amount, "Completed"));
    }
}

// --- Subscribers ---

public class AuditLogger
{
    public void OnOrderEvent(object? sender, OrderEventArgs e) =>
        Console.WriteLine($"  [Audit] Order {e.OrderId}: {e.Status} at {e.Timestamp:HH:mm:ss.fff}");
}

public class NotificationService
{
    public void OnOrderProcessed(object? sender, OrderEventArgs e) =>
        Console.WriteLine($"  [Notify] Order {e.OrderId} completed — notifying customer");

    public void OnOrderFailed(object? sender, OrderEventArgs e) =>
        Console.WriteLine($"  [Notify] Order {e.OrderId} FAILED: {e.Status} — alerting support");
}

// --- Multicast delegates ---

// INTERVIEW ANSWER: Delegates in C# are multicast — you can combine multiple
// methods into a single delegate using +=. When you invoke it, all subscribed
// methods are called in order. Events are built on this mechanism.
public class MetricsCollector
{
    public void OnOrderReceived(object? sender, OrderEventArgs e) =>
        Console.WriteLine($"  [Metrics] Received: {e.OrderId}, Amount: {e.Amount:C}");
}

// --- Pipeline using Func/Action ---

// INTERVIEW ANSWER: One of the most powerful uses of delegates is building
// pipelines — chains of transformations where each step is a function. This
// is the same concept behind middleware in ASP.NET Core.
public class RequestPipeline<T>
{
    private readonly List<Func<T, T>> _steps = [];

    public RequestPipeline<T> Use(Func<T, T> step)
    {
        _steps.Add(step);
        return this;
    }

    public T Execute(T input)
    {
        var current = input;
        foreach (var step in _steps)
        {
            current = step(current);
        }
        return current;
    }
}

public record HttpRequest(string Path, Dictionary<string, string> Headers, string Body)
{
    public override string ToString() =>
        $"Path: {Path}, Headers: [{string.Join(", ", Headers.Select(h => $"{h.Key}={h.Value}"))}], Body: {Body}";
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== DELEGATES AND EVENTS DEMO ===\n");

// --- Built-in delegates ---
Console.WriteLine("--- Built-in Delegates: Action, Func, Predicate ---");

// Action<T> — takes parameters, returns void
Action<string> log = message => Console.WriteLine($"  [LOG] {message}");
log("Application started");

// Func<T, TResult> — takes parameters, returns a value
Func<decimal, decimal, decimal> calculateTax = (amount, rate) => amount * rate;
Console.WriteLine($"  Tax on $100 at 8.5%: {calculateTax(100m, 0.085m):C}");

// Predicate<T> — takes a parameter, returns bool
Predicate<string> isValidEmail = email => email.Contains('@') && email.Contains('.');
Console.WriteLine($"  Is 'test@mail.com' valid? {isValidEmail("test@mail.com")}");
Console.WriteLine($"  Is 'bad-email' valid? {isValidEmail("bad-email")}");
Console.WriteLine();

// --- Custom delegate ---
Console.WriteLine("--- Custom Delegate: RetryPolicy ---");
RetryPolicy exponentialBackoff = (attempt, ex) =>
{
    if (attempt > 3) return false;
    Console.WriteLine($"  Retry #{attempt} after {ex.GetType().Name} — waiting {Math.Pow(2, attempt)}s");
    return true;
};

// Simulate retries
var testEx = new TimeoutException("Connection timed out");
for (int i = 1; i <= 4; i++)
{
    if (!exponentialBackoff(i, testEx))
    {
        Console.WriteLine($"  Giving up after attempt #{i}");
        break;
    }
}
Console.WriteLine();

// --- Events ---
Console.WriteLine("--- Events: OrderProcessor ---");
var processor = new OrderProcessor();
var audit = new AuditLogger();
var notifications = new NotificationService();
var metrics = new MetricsCollector();

// Subscribe to events (multicast — multiple handlers per event)
processor.OrderReceived += audit.OnOrderEvent;
processor.OrderReceived += metrics.OnOrderReceived;
processor.OrderProcessed += audit.OnOrderEvent;
processor.OrderProcessed += notifications.OnOrderProcessed;
processor.OrderFailed += audit.OnOrderEvent;
processor.OrderFailed += notifications.OnOrderFailed;

await processor.ProcessOrderAsync("ORD-001", 99.99m);
await processor.ProcessOrderAsync("ORD-002", 15_000m); // Will fail

// Unsubscribe metrics and process another
processor.OrderReceived -= metrics.OnOrderReceived;
Console.WriteLine("\n  (Metrics unsubscribed)");
await processor.ProcessOrderAsync("ORD-003", 250m);
Console.WriteLine();

// --- Lambda expressions ---
Console.WriteLine("--- Lambda Expressions ---");

// INTERVIEW ANSWER: Lambdas are anonymous functions — concise syntax for defining
// inline delegate instances. They can capture variables from the enclosing scope
// (closures). The compiler converts them to delegate instances or expression trees.
List<string> names = ["Charlie", "Alice", "Bob", "Diana", "Eve"];

// Lambda with List methods
var sorted = names.OrderBy(n => n).ToList();
Console.WriteLine($"  Sorted: {string.Join(", ", sorted)}");

var filtered = names.Where(n => n.Length > 3).ToList();
Console.WriteLine($"  Length > 3: {string.Join(", ", filtered)}");

// Multi-line lambda
Func<string, string> processName = name =>
{
    var trimmed = name.Trim().ToUpperInvariant();
    return $"[{trimmed}]";
};
Console.WriteLine($"  Processed: {processName("  alice  ")}");
Console.WriteLine();

// --- Pipeline using delegates ---
Console.WriteLine("--- Request Pipeline (Func<T,T> chain) ---");
var pipeline = new RequestPipeline<HttpRequest>()
    .Use(req =>
    {
        // Add correlation ID
        req.Headers["X-Correlation-Id"] = Guid.NewGuid().ToString()[..8];
        Console.WriteLine($"  Step 1: Added correlation ID");
        return req;
    })
    .Use(req =>
    {
        // Normalize path
        var normalized = req with { Path = req.Path.ToLowerInvariant().TrimEnd('/') };
        Console.WriteLine($"  Step 2: Normalized path to '{normalized.Path}'");
        return normalized;
    })
    .Use(req =>
    {
        // Add auth header
        req.Headers["Authorization"] = "Bearer token-xyz";
        Console.WriteLine($"  Step 3: Added auth header");
        return req;
    });

var request = new HttpRequest("/API/Users/", new Dictionary<string, string>(), "{}");
Console.WriteLine($"  Input:  {request}");
var result = pipeline.Execute(request);
Console.WriteLine($"  Output: {result}");
