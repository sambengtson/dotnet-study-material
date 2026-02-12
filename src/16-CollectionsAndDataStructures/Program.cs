// ============================================================================
// TOPIC: Collections and Data Structures
// ============================================================================
// INTERVIEW ANSWER:
// .NET provides a rich set of collection types, each optimized for different
// access patterns. The key is knowing the Big-O characteristics: List<T> is O(1)
// for index access but O(n) for search; Dictionary<K,V> is O(1) for key lookup;
// HashSet<T> is O(1) for contains checks; SortedDictionary is O(log n) for
// everything. Choosing the right collection based on your access pattern is one
// of the most impactful performance decisions you can make.
// ============================================================================

using System.Collections.Frozen;

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== COLLECTIONS AND DATA STRUCTURES DEMO ===\n");

// --- List<T> ---
Console.WriteLine("--- List<T> — Dynamic array, O(1) index, O(n) search ---");

// INTERVIEW ANSWER: List<T> is backed by an array that doubles in capacity
// when full. Index access is O(1), Add is amortized O(1), Insert/Remove at
// arbitrary positions is O(n) because elements must shift. It's the go-to
// collection when you need ordered, indexable storage.
List<string> usernames = ["alice", "bob", "charlie"];
usernames.Add("diana");
usernames.Insert(1, "alex"); // O(n) — shifts bob, charlie, diana right
usernames.Remove("bob");     // O(n) — finds and shifts

Console.WriteLine($"  Users: [{string.Join(", ", usernames)}]");
Console.WriteLine($"  Index 0: {usernames[0]} (O(1))");
Console.WriteLine($"  Contains 'charlie': {usernames.Contains("charlie")} (O(n) scan)");
Console.WriteLine($"  Count: {usernames.Count}, Capacity: {usernames.Capacity}");
Console.WriteLine();

// --- Dictionary<TKey, TValue> ---
Console.WriteLine("--- Dictionary<TKey,TValue> — Hash table, O(1) lookup ---");

// INTERVIEW ANSWER: Dictionary uses a hash table internally. Key lookup, insert,
// and delete are all O(1) average case. The key must implement GetHashCode() and
// Equals() correctly. It's unordered — don't rely on enumeration order.
Dictionary<string, decimal> productPrices = new()
{
    ["laptop"] = 1299.99m,
    ["mouse"] = 29.99m,
    ["keyboard"] = 79.99m,
};

productPrices["monitor"] = 499.99m; // Add — O(1)

Console.WriteLine($"  Laptop price: {productPrices["laptop"]:C} (O(1) lookup)");

// TryGetValue — safe lookup without exceptions
if (productPrices.TryGetValue("phone", out var price))
    Console.WriteLine($"  Phone: {price:C}");
else
    Console.WriteLine("  Phone: not found (TryGetValue returned false)");

Console.WriteLine($"  Total products: {productPrices.Count}");
Console.WriteLine();

// --- HashSet<T> ---
Console.WriteLine("--- HashSet<T> — Unique elements, O(1) contains ---");

// INTERVIEW ANSWER: HashSet<T> is optimized for fast membership testing — Contains()
// is O(1). It automatically handles duplicates (they're silently ignored). It also
// supports set operations: union, intersection, difference. Use it when you need
// unique elements and fast "does this exist?" checks.
HashSet<string> permissions = ["read", "write", "execute"];
permissions.Add("read");  // Duplicate — ignored
permissions.Add("admin");

HashSet<string> requiredPermissions = ["read", "write"];

Console.WriteLine($"  Permissions: {{{string.Join(", ", permissions)}}}");
Console.WriteLine($"  Has 'write': {permissions.Contains("write")} (O(1))");
Console.WriteLine($"  Is superset of required: {permissions.IsSupersetOf(requiredPermissions)}");

permissions.IntersectWith(requiredPermissions);
Console.WriteLine($"  After intersect: {{{string.Join(", ", permissions)}}}");
Console.WriteLine();

// --- Queue<T> ---
Console.WriteLine("--- Queue<T> — FIFO, O(1) enqueue/dequeue ---");

// INTERVIEW ANSWER: Queue<T> is first-in, first-out. Both Enqueue and Dequeue
// are O(1). Use it for job queues, BFS traversals, or any "first come, first
// served" pattern.
Queue<string> jobQueue = new();
jobQueue.Enqueue("send-email-001");
jobQueue.Enqueue("process-payment-002");
jobQueue.Enqueue("generate-report-003");

Console.WriteLine($"  Queue: [{string.Join(" → ", jobQueue)}]");
Console.WriteLine($"  Peek: {jobQueue.Peek()} (doesn't remove)");
Console.WriteLine($"  Dequeue: {jobQueue.Dequeue()} (removes front)");
Console.WriteLine($"  After dequeue: [{string.Join(" → ", jobQueue)}]");
Console.WriteLine();

// --- Stack<T> ---
Console.WriteLine("--- Stack<T> — LIFO, O(1) push/pop ---");

// INTERVIEW ANSWER: Stack<T> is last-in, first-out. Push and Pop are O(1).
// Classic use cases: undo operations, expression parsing, DFS traversal,
// call stack simulation.
Stack<string> undoStack = new();
undoStack.Push("typed 'Hello'");
undoStack.Push("formatted bold");
undoStack.Push("changed font");

Console.WriteLine($"  Stack: [{string.Join(" | ", undoStack)}]");
Console.WriteLine($"  Undo: {undoStack.Pop()}");
Console.WriteLine($"  Undo: {undoStack.Pop()}");
Console.WriteLine($"  Remaining: [{string.Join(" | ", undoStack)}]");
Console.WriteLine();

// --- LinkedList<T> ---
Console.WriteLine("--- LinkedList<T> — O(1) insert/remove at known position ---");

// INTERVIEW ANSWER: LinkedList<T> is a doubly-linked list. Insert/remove at a
// known node is O(1), but finding that node is O(n). It's rarely used in practice
// because List<T>'s cache-friendly array beats it for most workloads. Use it when
// you need frequent insert/remove at arbitrary positions and already have a reference
// to the node.
LinkedList<string> playlist = new();
var song1 = playlist.AddLast("Song A");
var song2 = playlist.AddLast("Song B");
playlist.AddLast("Song C");
playlist.AddAfter(song1, "Song A.5"); // O(1) — we have the node reference
playlist.Remove(song2);               // O(1) — we have the node reference

Console.WriteLine($"  Playlist: [{string.Join(" → ", playlist)}]");
Console.WriteLine();

// --- PriorityQueue<TElement, TPriority> ---
Console.WriteLine("--- PriorityQueue<T, TPriority> — O(log n) enqueue/dequeue ---");

// INTERVIEW ANSWER: PriorityQueue (added in .NET 6) is a min-heap. Elements
// come out in priority order, not insertion order. Enqueue and Dequeue are
// O(log n). Use it for task scheduling, Dijkstra's algorithm, or any scenario
// where you always need the "most important" item next.
PriorityQueue<string, int> taskQueue = new();
taskQueue.Enqueue("Fix production bug", 1);     // Highest priority
taskQueue.Enqueue("Update docs", 5);
taskQueue.Enqueue("Code review", 3);
taskQueue.Enqueue("Security patch", 1);
taskQueue.Enqueue("Refactor tests", 4);

Console.WriteLine("  Tasks by priority (lower = higher priority):");
while (taskQueue.TryDequeue(out var task, out var priority))
    Console.WriteLine($"    [{priority}] {task}");
Console.WriteLine();

// --- FrozenDictionary / FrozenSet (.NET 8+) ---
Console.WriteLine("--- FrozenDictionary / FrozenSet — Optimized for reads ---");

// INTERVIEW ANSWER: Frozen collections (.NET 8+) are immutable collections
// optimized for read performance. You take a hit at creation time (they analyze
// the data and pick optimal internal strategies), but subsequent reads are
// faster than regular Dictionary/HashSet. Perfect for configuration, lookup
// tables, or any data that's built once and read many times.
var config = new Dictionary<string, string>
{
    ["db:host"] = "localhost",
    ["db:port"] = "5432",
    ["cache:ttl"] = "300",
    ["app:name"] = "MyService",
    ["app:env"] = "production"
};

var frozenConfig = config.ToFrozenDictionary();
Console.WriteLine($"  Type: {frozenConfig.GetType().Name}");
Console.WriteLine($"  db:host = {frozenConfig["db:host"]}");
Console.WriteLine($"  Keys: [{string.Join(", ", frozenConfig.Keys)}]");

var frozenTags = new[] { "dotnet", "csharp", "aspnet", "blazor" }.ToFrozenSet();
Console.WriteLine($"  FrozenSet contains 'csharp': {frozenTags.Contains("csharp")}");
Console.WriteLine();

// --- ReadOnlySpan<T> ---
Console.WriteLine("--- ReadOnlySpan<T> — Stack-only, zero-copy slice ---");

// INTERVIEW ANSWER: Span<T> and ReadOnlySpan<T> are stack-only types that
// provide a view into contiguous memory without copying. You can slice arrays,
// strings, or native memory with zero allocations. They can't be stored on
// the heap (no class fields, no async methods), which is what makes them safe
// and fast. They're the foundation of high-performance .NET code.
var data = "2024-01-15T14:30:00Z";
ReadOnlySpan<char> span = data.AsSpan();
var year = span[..4];       // No allocation — just a view
var month = span[5..7];
var day = span[8..10];
Console.WriteLine($"  Original: {data}");
Console.WriteLine($"  Year: {year}, Month: {month}, Day: {day} (zero-copy slices)");

// Parsing without string allocation
ReadOnlySpan<char> csvLine = "Alice,Engineering,145000".AsSpan();
var firstComma = csvLine.IndexOf(',');
var lastComma = csvLine.LastIndexOf(',');
var name = csvLine[..firstComma];
var dept = csvLine[(firstComma + 1)..lastComma];
var salary = csvLine[(lastComma + 1)..];
Console.WriteLine($"  CSV parsed: Name={name}, Dept={dept}, Salary={salary}");
Console.WriteLine();

// --- Big-O Summary ---
Console.WriteLine("--- Big-O Summary ---");
Console.WriteLine("""
  | Collection              | Access | Search  | Insert  | Delete  |
  |------------------------|--------|---------|---------|---------|
  | List<T>                | O(1)   | O(n)    | O(n)*   | O(n)    |
  | Dictionary<K,V>        | O(1)   | O(1)    | O(1)    | O(1)    |
  | HashSet<T>             | —      | O(1)    | O(1)    | O(1)    |
  | SortedDictionary<K,V>  | O(logn)| O(logn) | O(logn) | O(logn) |
  | Queue<T>               | O(1)†  | O(n)    | O(1)    | O(1)†   |
  | Stack<T>               | O(1)†  | O(n)    | O(1)    | O(1)†   |
  | LinkedList<T>          | O(n)   | O(n)    | O(1)‡   | O(1)‡   |
  | PriorityQueue<T,P>     | O(1)†  | O(n)    | O(logn) | O(logn) |
  | FrozenDictionary<K,V>  | O(1)   | O(1)    | —       | —       |

  * List<T> Add is amortized O(1), Insert at index is O(n)
  † Front/top element only
  ‡ With reference to the node
""");
