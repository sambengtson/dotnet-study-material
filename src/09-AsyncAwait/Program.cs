// ============================================================================
// TOPIC: Async/Await
// ============================================================================
// INTERVIEW ANSWER:
// Async/await is C#'s way of writing non-blocking code that reads like synchronous
// code. When you `await` a Task, the current method is suspended and the thread is
// freed to do other work. When the awaited operation completes, execution resumes
// where it left off. It's NOT about creating new threads — it's about not BLOCKING
// threads while waiting for I/O. The compiler transforms async methods into state
// machines behind the scenes.
// ============================================================================

using System.Runtime.CompilerServices;

// --- Simulated HTTP Client ---

public class ApiClient
{
    private readonly HttpClient _http = new();
    private readonly string _baseUrl;

    public ApiClient(string baseUrl) => _baseUrl = baseUrl;

    // INTERVIEW ANSWER: Task<T> represents a future value. The method returns
    // immediately with a Task, and the actual value becomes available when the
    // I/O completes. The caller can await it or compose multiple tasks together.
    public async Task<string> GetUserAsync(string userId)
    {
        await Task.Delay(100); // Simulate HTTP latency
        return $"{{\"id\":\"{userId}\",\"name\":\"User {userId}\",\"active\":true}}";
    }

    public async Task<string> GetOrdersAsync(string userId)
    {
        await Task.Delay(150);
        return $"[{{\"orderId\":\"ORD-1\",\"userId\":\"{userId}\",\"total\":99.99}}]";
    }

    // INTERVIEW ANSWER: ValueTask<T> is a struct-based alternative to Task<T>.
    // Use it when the method frequently returns synchronously (like a cache hit).
    // It avoids the heap allocation of a Task object in the fast path. Don't use
    // it everywhere — only when profiling shows the Task allocation matters.
    private readonly Dictionary<string, string> _cache = [];

    public ValueTask<string> GetCachedProfileAsync(string userId)
    {
        if (_cache.TryGetValue(userId, out var cached))
        {
            Console.WriteLine($"  [Cache HIT] {userId}");
            return ValueTask.FromResult(cached); // No Task allocation!
        }

        return LoadAndCacheAsync(userId);
    }

    private async ValueTask<string> LoadAndCacheAsync(string userId)
    {
        Console.WriteLine($"  [Cache MISS] {userId}");
        var profile = await GetUserAsync(userId);
        _cache[userId] = profile;
        return profile;
    }
}

// --- Cancellation ---

public class DataProcessor
{
    // INTERVIEW ANSWER: CancellationToken is how you cooperatively cancel async
    // operations. The caller creates a CancellationTokenSource, passes the token
    // to the async method, and the method checks it periodically. It's cooperative
    // because the method has to actually check — you can't force-kill an async
    // operation from outside.
    public async Task ProcessBatchAsync(
        IEnumerable<string> items,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            // Check for cancellation before each unit of work
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine($"  Processing: {item}");
            await Task.Delay(200, cancellationToken);
        }
    }
}

// --- Async Streams ---

public class EventStream
{
    // INTERVIEW ANSWER: IAsyncEnumerable<T> is the async version of IEnumerable<T>.
    // It lets you produce items asynchronously one at a time, and the consumer can
    // `await foreach` over them. This is perfect for streaming data — database cursors,
    // event streams, paginated APIs — where you don't want to load everything into
    // memory at once.
    public async IAsyncEnumerable<string> GetEventsAsync(
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100, cancellationToken);
            yield return $"Event-{i:D3} at {DateTime.UtcNow:HH:mm:ss.fff}";
        }
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== ASYNC/AWAIT DEMO ===\n");

var client = new ApiClient("https://api.example.com");

// --- Sequential vs Parallel ---
Console.WriteLine("--- Sequential Execution ---");
var sw = System.Diagnostics.Stopwatch.StartNew();
var user1 = await client.GetUserAsync("user-1");
var orders1 = await client.GetOrdersAsync("user-1");
sw.Stop();
Console.WriteLine($"  User: {user1}");
Console.WriteLine($"  Orders: {orders1}");
Console.WriteLine($"  Time: {sw.ElapsedMilliseconds}ms (sequential)\n");

// INTERVIEW ANSWER: Task.WhenAll runs multiple tasks concurrently and waits for
// ALL of them to complete. Use it when you have independent async operations —
// instead of awaiting each one sequentially (which is N * latency), you run them
// all at once (which is max(latency)).
Console.WriteLine("--- Parallel with Task.WhenAll ---");
sw.Restart();
var userTask = client.GetUserAsync("user-2");
var ordersTask = client.GetOrdersAsync("user-2");
var results = await Task.WhenAll(userTask, ordersTask);
sw.Stop();
Console.WriteLine($"  User: {results[0]}");
Console.WriteLine($"  Orders: {results[1]}");
Console.WriteLine($"  Time: {sw.ElapsedMilliseconds}ms (parallel — should be ~half)\n");

// --- Task.WhenAny (first to complete wins) ---
Console.WriteLine("--- Task.WhenAny (fastest response) ---");
// INTERVIEW ANSWER: Task.WhenAny returns as soon as ANY task completes. It's
// useful for timeout patterns, redundant requests, or "first result wins" scenarios.
var fast = client.GetUserAsync("fast");
var slow = Task.Delay(500).ContinueWith(_ => "slow response");
var winner = await Task.WhenAny(fast, slow);
Console.WriteLine($"  Winner: {await winner}");
Console.WriteLine();

// --- ValueTask ---
Console.WriteLine("--- ValueTask<T> (cache optimization) ---");
var profile1 = await client.GetCachedProfileAsync("user-42");
Console.WriteLine($"  Result: {profile1}");
var profile2 = await client.GetCachedProfileAsync("user-42"); // Cache hit — no allocation
Console.WriteLine($"  Result: {profile2}");
Console.WriteLine();

// --- Cancellation ---
Console.WriteLine("--- CancellationToken ---");
var processor = new DataProcessor();
using var cts = new CancellationTokenSource();

// Cancel after 500ms
cts.CancelAfter(500);

string[] batch = ["Item-A", "Item-B", "Item-C", "Item-D", "Item-E"];
try
{
    await processor.ProcessBatchAsync(batch, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("  Operation was cancelled!");
}
Console.WriteLine();

// --- Async Streams (IAsyncEnumerable<T>) ---
Console.WriteLine("--- Async Streams (IAsyncEnumerable<T>) ---");
var stream = new EventStream();
using var streamCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(550));

try
{
    await foreach (var evt in stream.GetEventsAsync(10, streamCts.Token))
    {
        Console.WriteLine($"  Received: {evt}");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("  Stream cancelled after timeout");
}
Console.WriteLine();

// --- Exception handling in async ---
Console.WriteLine("--- Async Exception Handling ---");

// INTERVIEW ANSWER: When an async method throws, the exception is captured and
// placed on the returned Task. It's re-thrown when you await that Task. If you
// use Task.WhenAll, the first exception is thrown and the rest are in the
// AggregateException accessible via the Task.Exception property.
async Task<string> FailingOperationAsync(string name)
{
    await Task.Delay(50);
    throw new InvalidOperationException($"{name} failed!");
}

try
{
    var t1 = FailingOperationAsync("Op-1");
    var t2 = FailingOperationAsync("Op-2");
    var allTask = Task.WhenAll(t1, t2);

    try { await allTask; }
    catch
    {
        // The first exception is thrown, but all are available
        Console.WriteLine($"  Caught: {allTask.Exception!.InnerExceptions.Count} exceptions:");
        foreach (var ex in allTask.Exception.InnerExceptions)
            Console.WriteLine($"    - {ex.Message}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"  Unexpected: {ex.Message}");
}
