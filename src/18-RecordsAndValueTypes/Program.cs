// ============================================================================
// TOPIC: Records and Value Types
// ============================================================================
// INTERVIEW ANSWER:
// Records (C# 9+) are types designed for immutable data with value-based equality.
// `record` (or `record class`) is a reference type on the heap; `record struct`
// is a value type on the stack. Regular classes use reference equality (same object
// in memory), but records compare by value (same data = equal). Records also get
// built-in `ToString()`, deconstruction, and `with` expressions for non-destructive
// mutation. Choosing between class, record, struct, and record struct depends on
// whether you need identity vs. value semantics, mutability, and allocation behavior.
// ============================================================================

// --- record class (reference type, value equality) ---

// INTERVIEW ANSWER: A positional record like this gives you a constructor,
// properties, deconstruction, value equality, and a nice ToString() — all in
// one line. Use records for DTOs, events, API responses — any immutable data.
public record UserProfile(string Id, string Name, string Email, string Role);

// Record with additional members
public record AuditEvent(string Action, string UserId, DateTime Timestamp)
{
    // You can add extra properties and methods to records
    public string Summary => $"[{Timestamp:HH:mm:ss}] {UserId}: {Action}";
}

// --- record struct (value type, value equality) ---

// INTERVIEW ANSWER: `record struct` combines the allocation benefits of structs
// (stack-allocated, no GC pressure for small types) with the convenience of
// records (value equality, with expressions). Use for small, frequently-created
// value objects like coordinates, money, or measurements.
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot add {a.Currency} and {b.Currency}");
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator *(Money m, int quantity) =>
        new(m.Amount * quantity, m.Currency);

    public override string ToString() => $"{Amount:F2} {Currency}";
}

public readonly record struct GeoCoordinate(double Latitude, double Longitude)
{
    public double DistanceTo(GeoCoordinate other)
    {
        // Simplified distance calculation
        var dLat = Math.Abs(Latitude - other.Latitude);
        var dLon = Math.Abs(Longitude - other.Longitude);
        return Math.Sqrt(dLat * dLat + dLon * dLon);
    }
}

// --- class vs record comparison ---

public class UserClass
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public record UserRecord(string Id, string Name);

// --- struct vs readonly struct ---

// INTERVIEW ANSWER: A regular struct is mutable on the stack, which can lead to
// subtle bugs when copied. A `readonly struct` guarantees immutability — the
// compiler enforces that no fields or properties change after construction.
// Always prefer `readonly struct` unless you specifically need mutation.
public struct MutablePoint
{
    public double X;
    public double Y;

    public void Move(double dx, double dy)  // Mutates in place
    {
        X += dx;
        Y += dy;
    }
}

public readonly struct ImmutablePoint(double X, double Y)
{
    // INTERVIEW ANSWER: Primary constructor parameters on a readonly struct
    // become readonly fields. You can't have a Move method that modifies them.
    // Instead, you'd return a new instance.
    public double X { get; } = X;
    public double Y { get; } = Y;

    public ImmutablePoint MovedBy(double dx, double dy) => new(X + dx, Y + dy);

    public override string ToString() => $"({X}, {Y})";
}

// --- ref struct (stack-only, can't escape to heap) ---

// INTERVIEW ANSWER: `ref struct` is a stack-only type that can NEVER be on the
// heap. You can't box it, use it as an interface, store it in a class field, or
// use it in async methods. Span<T> is a ref struct — this restriction is what
// makes it safe and fast for working with contiguous memory without GC pressure.
public ref struct TokenParser
{
    private ReadOnlySpan<char> _remaining;
    public int TokenCount { get; private set; }

    public TokenParser(ReadOnlySpan<char> input) => _remaining = input;

    public ReadOnlySpan<char> NextToken(char delimiter)
    {
        var idx = _remaining.IndexOf(delimiter);
        ReadOnlySpan<char> token;
        if (idx < 0)
        {
            token = _remaining;
            _remaining = [];
        }
        else
        {
            token = _remaining[..idx];
            _remaining = _remaining[(idx + 1)..];
        }
        TokenCount++;
        return token;
    }

    public bool HasMore => !_remaining.IsEmpty;
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== RECORDS AND VALUE TYPES DEMO ===\n");

// --- Record Value Equality ---
Console.WriteLine("--- Record Value Equality ---");
var user1 = new UserProfile("U1", "Alice", "alice@test.com", "Admin");
var user2 = new UserProfile("U1", "Alice", "alice@test.com", "Admin");
var user3 = user1 with { Role = "User" };

Console.WriteLine($"  user1: {user1}");
Console.WriteLine($"  user2: {user2}");
Console.WriteLine($"  user1 == user2: {user1 == user2} (value equality — same data)");
Console.WriteLine($"  ReferenceEquals: {ReferenceEquals(user1, user2)} (different objects)");
Console.WriteLine();

// --- `with` Expressions ---
Console.WriteLine("--- with Expressions (non-destructive mutation) ---");

// INTERVIEW ANSWER: `with` creates a copy of a record with specified properties
// changed. The original is untouched. This is how you do "immutable updates" —
// you never mutate, you create a new version with changes applied.
Console.WriteLine($"  Original: {user1}");
Console.WriteLine($"  Modified: {user3}");
Console.WriteLine($"  Original unchanged: {user1.Role}");
Console.WriteLine();

// --- Class vs Record Equality ---
Console.WriteLine("--- Class vs Record Equality ---");
var classA = new UserClass { Id = "1", Name = "Bob" };
var classB = new UserClass { Id = "1", Name = "Bob" };
var recordA = new UserRecord("1", "Bob");
var recordB = new UserRecord("1", "Bob");

Console.WriteLine($"  Class:  classA == classB:   {classA == classB} (reference equality)");
Console.WriteLine($"  Record: recordA == recordB: {recordA == recordB} (value equality)");
Console.WriteLine();

// --- Record Deconstruction ---
Console.WriteLine("--- Record Deconstruction ---");
var (id, name, email, role) = user1;
Console.WriteLine($"  Deconstructed: id={id}, name={name}, email={email}, role={role}");
Console.WriteLine();

// --- Record Struct ---
Console.WriteLine("--- Record Struct (value type + record features) ---");
var price1 = new Money(29.99m, "USD");
var price2 = new Money(29.99m, "USD");
var price3 = new Money(15.00m, "USD");

Console.WriteLine($"  price1: {price1}");
Console.WriteLine($"  price1 == price2: {price1 == price2} (value equality)");
Console.WriteLine($"  price1 + price3: {price1 + price3}");
Console.WriteLine($"  price1 * 3: {price1 * 3}");
Console.WriteLine();

var coord1 = new GeoCoordinate(40.7128, -74.0060); // NYC
var coord2 = new GeoCoordinate(34.0522, -118.2437); // LA
Console.WriteLine($"  NYC: {coord1}");
Console.WriteLine($"  LA: {coord2}");
Console.WriteLine($"  Distance: {coord1.DistanceTo(coord2):F2} degrees");
Console.WriteLine();

// --- Readonly Struct ---
Console.WriteLine("--- Struct vs Readonly Struct ---");
var mutable = new MutablePoint { X = 1, Y = 2 };
mutable.Move(3, 4);
Console.WriteLine($"  MutablePoint after Move: ({mutable.X}, {mutable.Y})");

var immutable = new ImmutablePoint(1, 2);
var moved = immutable.MovedBy(3, 4);
Console.WriteLine($"  ImmutablePoint original: {immutable}");
Console.WriteLine($"  ImmutablePoint moved: {moved} (new instance)");
Console.WriteLine();

// --- Ref Struct (Span<T>) ---
Console.WriteLine("--- Ref Struct and Span<T> ---");

// Span<T> usage — zero-allocation string manipulation
Span<int> numbers = stackalloc int[] { 10, 20, 30, 40, 50 };
var slice = numbers[1..4]; // [20, 30, 40] — no copy
Console.WriteLine($"  Span slice [1..4]: [{string.Join(", ", slice.ToArray())}]");

// Modify through span — changes the original
slice[0] = 99;
Console.WriteLine($"  After modifying slice[0]=99, original[1]: {numbers[1]}");
Console.WriteLine();

// Custom ref struct
Console.WriteLine("--- Custom Ref Struct: TokenParser ---");
var parser = new TokenParser("host=localhost;port=5432;db=myapp;user=admin".AsSpan());
while (parser.HasMore)
{
    var token = parser.NextToken(';');
    Console.WriteLine($"  Token: {token}");
}
Console.WriteLine($"  Total tokens: {parser.TokenCount}");

// INTERVIEW ANSWER: ref structs are a trade-off. You get zero GC pressure and
// direct memory access, but you can't use them in async code, store them in
// collections, or box them. Use them in hot paths where allocation matters.
