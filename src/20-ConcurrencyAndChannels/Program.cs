// ============================================================================
// TOPIC: Concurrency and Channels
// ============================================================================
// INTERVIEW ANSWER:
// Concurrency in .NET goes beyond async/await. Channel<T> provides a thread-safe
// producer/consumer pipeline. SemaphoreSlim limits concurrent access to a resource.
// ConcurrentDictionary is a thread-safe dictionary that doesn't need external locking.
// Interlocked provides atomic operations on shared variables. The key principle is:
// prefer higher-level abstractions (channels, concurrent collections) over manual
// locking. Locks are error-prone — deadlocks, forgotten unlocks, lock convoys.
// Channels and concurrent collections handle synchronization internally.
// ============================================================================

using System.Collections.Concurrent;
using System.Threading.Channels;

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== CONCURRENCY AND CHANNELS DEMO ===\n");

// --- Channel<T>: Producer/Consumer ---
Console.WriteLine("--- Channel<T>: Producer/Consumer Pipeline ---\n");

// INTERVIEW ANSWER: Channel<T> is a thread-safe, async-ready queue for passing
// data between producers and consumers. Bounded channels apply backpressure —
// the producer blocks when the channel is full, which prevents memory from
// growing unbounded. This is the .NET equivalent of Go channels.
var channel = Channel.CreateBounded<WorkItem>(new BoundedChannelOptions(5)
{
    FullMode = BoundedChannelFullMode.Wait // Producer waits when channel is full
});

// Producer
var producer = Task.Run(async () =>
{
    for (int i = 1; i <= 10; i++)
    {
        var item = new WorkItem(i, $"Task-{i:D3}", DateTime.UtcNow);
        await channel.Writer.WriteAsync(item);
        Console.WriteLine($"  [Producer] Enqueued: {item.Name}");
        await Task.Delay(50); // Simulate work to produce items
    }
    channel.Writer.Complete(); // Signal no more items
    Console.WriteLine("  [Producer] Completed");
});

// Multiple consumers
var consumers = Enumerable.Range(1, 3).Select(consumerId =>
    Task.Run(async () =>
    {
        await foreach (var item in channel.Reader.ReadAllAsync())
        {
            Console.WriteLine($"  [Consumer-{consumerId}] Processing: {item.Name}");
            await Task.Delay(100); // Simulate processing time
        }
        Console.WriteLine($"  [Consumer-{consumerId}] Done");
    }));

await Task.WhenAll(consumers.Append(producer));
Console.WriteLine();

// --- SemaphoreSlim: Limiting Concurrency ---
Console.WriteLine("--- SemaphoreSlim: Rate Limiting ---\n");

// INTERVIEW ANSWER: SemaphoreSlim limits how many threads/tasks can access a
// resource concurrently. Unlike a lock (which allows only 1), a semaphore allows
// N concurrent accessors. This is perfect for rate limiting API calls, limiting
// database connection usage, or capping parallel I/O operations.
var semaphore = new SemaphoreSlim(3); // Max 3 concurrent operations
var apiTasks = Enumerable.Range(1, 8).Select(async i =>
{
    await semaphore.WaitAsync();
    try
    {
        Console.WriteLine($"  [API-{i}] Started (concurrent: {3 - semaphore.CurrentCount}/3)");
        await Task.Delay(200); // Simulate API call
        Console.WriteLine($"  [API-{i}] Completed");
    }
    finally
    {
        semaphore.Release();
    }
});

await Task.WhenAll(apiTasks);
Console.WriteLine();

// --- ConcurrentDictionary ---
Console.WriteLine("--- ConcurrentDictionary: Thread-Safe State ---\n");

// INTERVIEW ANSWER: ConcurrentDictionary is a thread-safe dictionary that uses
// fine-grained locking internally (lock striping). The key methods are
// GetOrAdd (atomic get-or-create), AddOrUpdate (atomic upsert), and
// TryRemove. These compound operations are atomic, which you CAN'T achieve
// with a regular Dictionary + lock without holding the lock across the entire
// read-modify-write sequence.
var requestCounts = new ConcurrentDictionary<string, int>();

var countTasks = Enumerable.Range(1, 100).Select(i =>
    Task.Run(() =>
    {
        var endpoint = $"/api/{(i % 5 == 0 ? "users" : i % 3 == 0 ? "orders" : "products")}";
        requestCounts.AddOrUpdate(endpoint, 1, (_, count) => count + 1);
    }));

await Task.WhenAll(countTasks);

Console.WriteLine("  Request counts (from 100 concurrent requests):");
foreach (var (endpoint, count) in requestCounts.OrderByDescending(kv => kv.Value))
    Console.WriteLine($"    {endpoint}: {count} requests");
Console.WriteLine();

// --- Interlocked: Atomic Operations ---
Console.WriteLine("--- Interlocked: Atomic Counter ---\n");

// INTERVIEW ANSWER: Interlocked provides atomic operations — Increment, Decrement,
// Exchange, CompareExchange. These are lock-free and use CPU-level atomic instructions.
// They're the fastest synchronization primitive but only work for simple operations
// on single values. For anything more complex, you need locks or concurrent collections.
int unsafeCounter = 0;
int safeCounter = 0;

var incrementTasks = Enumerable.Range(0, 1000).Select(_ =>
    Task.Run(() =>
    {
        unsafeCounter++;                        // NOT thread-safe!
        Interlocked.Increment(ref safeCounter); // Thread-safe
    }));

await Task.WhenAll(incrementTasks);

Console.WriteLine($"  Unsafe counter: {unsafeCounter} (expected 1000 — likely wrong due to race conditions)");
Console.WriteLine($"  Safe counter:   {safeCounter} (always 1000 — Interlocked guarantees it)");
Console.WriteLine();

// --- Parallel.ForEachAsync ---
Console.WriteLine("--- Parallel.ForEachAsync: Controlled Parallelism ---\n");

// INTERVIEW ANSWER: Parallel.ForEachAsync (.NET 6+) is the modern way to process
// a collection in parallel with controlled concurrency. It respects async/await
// and lets you set MaxDegreeOfParallelism. Unlike Task.WhenAll on a LINQ Select
// (which starts ALL tasks at once), this limits how many run simultaneously.
var urls = Enumerable.Range(1, 8).Select(i => $"https://api.example.com/data/{i}");

await Parallel.ForEachAsync(
    urls,
    new ParallelOptions { MaxDegreeOfParallelism = 3 },
    async (url, ct) =>
    {
        Console.WriteLine($"  [Fetch] Starting: {url}");
        await Task.Delay(150, ct); // Simulate HTTP request
        Console.WriteLine($"  [Fetch] Completed: {url}");
    });
Console.WriteLine();

// --- Thread Safety Concepts Summary ---
Console.WriteLine("--- Thread Safety Summary ---");
Console.WriteLine("""
  | Mechanism              | Use Case                      | Overhead |
  |------------------------|-------------------------------|----------|
  | Interlocked            | Single variable, atomic ops   | Lowest   |
  | lock / Monitor         | Protecting code sections      | Low      |
  | SemaphoreSlim          | Limiting concurrency (N)      | Low      |
  | ConcurrentDictionary   | Shared key-value state        | Medium   |
  | Channel<T>             | Producer/consumer pipelines   | Medium   |
  | Parallel.ForEachAsync  | Parallel collection processing| Medium   |

  General rules:
  - Prefer immutable data (no sync needed if nothing changes)
  - Use channels for data flow between tasks
  - Use concurrent collections for shared mutable state
  - Use SemaphoreSlim to limit resource access
  - Use Interlocked for simple counters/flags
  - Use lock only when simpler tools don't fit
""");

// --- Supporting types ---

public record WorkItem(int Id, string Name, DateTime CreatedAt);
