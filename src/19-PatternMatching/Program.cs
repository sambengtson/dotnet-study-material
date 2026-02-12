// ============================================================================
// TOPIC: Pattern Matching
// ============================================================================
// INTERVIEW ANSWER:
// Pattern matching in C# lets you test a value against a pattern and extract
// information from it in a single expression. It's evolved significantly — from
// basic `is` type checks in C# 7, through switch expressions and property patterns
// in C# 8-9, to list patterns in C# 11. The key benefit is writing concise,
// declarative code that's often clearer than chains of if/else. Switch expressions
// also enable exhaustiveness checking — the compiler can warn you if you miss a case.
// ============================================================================

// --- Types for pattern matching examples ---

public abstract record HttpResponse(int StatusCode, string Body);
public record SuccessResponse(int StatusCode, string Body, string ContentType) : HttpResponse(StatusCode, Body);
public record ErrorResponse(int StatusCode, string Body, string ErrorCode) : HttpResponse(StatusCode, Body);
public record RedirectResponse(int StatusCode, string Body, string Location) : HttpResponse(StatusCode, Body);

public record Order(string Id, decimal Amount, string Status, string? CouponCode, Address ShippingAddress);
public record Address(string Country, string State, string City, string Zip);

public record SensorReading(string SensorId, string Type, double Value, DateTime Timestamp);

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== PATTERN MATCHING DEMO ===\n");

// --- Type Patterns (is) ---
Console.WriteLine("--- Type Patterns ---");

HttpResponse[] responses =
[
    new SuccessResponse(200, "{\"data\":\"ok\"}", "application/json"),
    new ErrorResponse(404, "Not found", "RESOURCE_NOT_FOUND"),
    new RedirectResponse(301, "", "https://newsite.com"),
    new ErrorResponse(500, "Internal error", "SERVER_ERROR"),
    new SuccessResponse(204, "", "none"),
];

foreach (var response in responses)
{
    // INTERVIEW ANSWER: Type patterns with `is` let you check the type AND
    // extract the typed value in one step. This replaced the old pattern of
    // `if (x is Type) { var y = (Type)x; ... }` with something much cleaner.
    string description = response switch
    {
        SuccessResponse { StatusCode: 200 } s => $"OK ({s.ContentType}): {s.Body}",
        SuccessResponse { StatusCode: 204 } => "No Content",
        ErrorResponse { StatusCode: >= 500 } e => $"Server Error [{e.ErrorCode}]: {e.Body}",
        ErrorResponse e => $"Client Error {e.StatusCode} [{e.ErrorCode}]: {e.Body}",
        RedirectResponse r => $"Redirect → {r.Location}",
        _ => $"Unknown response: {response.StatusCode}"
    };
    Console.WriteLine($"  [{response.StatusCode}] {description}");
}
Console.WriteLine();

// --- Property Patterns ---
Console.WriteLine("--- Property Patterns ---");

Order[] orders =
[
    new("ORD-1", 150m, "Pending", "SAVE10", new("US", "CA", "LA", "90001")),
    new("ORD-2", 2500m, "Pending", null, new("US", "TX", "Austin", "78701")),
    new("ORD-3", 50m, "Shipped", null, new("CA", "ON", "Toronto", "M5V")),
    new("ORD-4", 999m, "Pending", "VIP50", new("US", "NY", "NYC", "10001")),
    new("ORD-5", 0m, "Cancelled", null, new("MX", "CDMX", "Mexico City", "06600")),
];

// INTERVIEW ANSWER: Property patterns let you match against the shape of an
// object — checking multiple properties in a single expression. Nested property
// patterns (like ShippingAddress.Country) make this even more powerful. It's
// declarative: you describe WHAT you're looking for, not HOW to find it.
foreach (var order in orders)
{
    var shippingCost = order switch
    {
        { Status: "Cancelled" } => 0m,
        { Amount: 0 } => 0m,
        { ShippingAddress.Country: "US", Amount: > 100m } => 0m,  // Free US shipping over $100
        { ShippingAddress.Country: "US" } => 5.99m,
        { ShippingAddress.Country: "CA" } => 12.99m,
        _ => 24.99m // International
    };
    Console.WriteLine($"  {order.Id}: {order.Amount:C} to {order.ShippingAddress.Country} → Shipping: {shippingCost:C}");
}
Console.WriteLine();

// --- Tuple Patterns ---
Console.WriteLine("--- Tuple Patterns ---");

// INTERVIEW ANSWER: Tuple patterns let you match on multiple values simultaneously.
// Instead of nested if/else checking two variables, you match the tuple of both.
// This is great for state machines or decisions that depend on multiple inputs.
(string Role, string Action)[] requests =
[
    ("Admin", "Delete"),
    ("Editor", "Edit"),
    ("Viewer", "Edit"),
    ("Admin", "Edit"),
    ("Viewer", "View"),
];

foreach (var (role, action) in requests)
{
    var allowed = (role, action) switch
    {
        ("Admin", _) => true,               // Admins can do anything
        ("Editor", "Edit" or "View") => true,
        ("Viewer", "View") => true,
        _ => false
    };
    Console.WriteLine($"  {role} → {action}: {(allowed ? "ALLOWED" : "DENIED")}");
}
Console.WriteLine();

// --- Relational Patterns ---
Console.WriteLine("--- Relational Patterns ---");

SensorReading[] readings =
[
    new("TEMP-1", "temperature", 22.5, DateTime.UtcNow),
    new("TEMP-2", "temperature", 38.7, DateTime.UtcNow),
    new("TEMP-3", "temperature", -5.2, DateTime.UtcNow),
    new("HUM-1", "humidity", 45.0, DateTime.UtcNow),
    new("HUM-2", "humidity", 92.3, DateTime.UtcNow),
];

// INTERVIEW ANSWER: Relational patterns (<, >, <=, >=) combined with `and`, `or`,
// `not` let you express ranges and compound conditions directly in pattern syntax.
// This is more readable than chained && and || operators in traditional if statements.
foreach (var reading in readings)
{
    var status = reading switch
    {
        { Type: "temperature", Value: < 0 } => "FREEZING",
        { Type: "temperature", Value: >= 0 and < 18 } => "Cold",
        { Type: "temperature", Value: >= 18 and <= 26 } => "Comfortable",
        { Type: "temperature", Value: > 26 and <= 35 } => "Warm",
        { Type: "temperature", Value: > 35 } => "OVERHEATING",
        { Type: "humidity", Value: < 30 } => "Too Dry",
        { Type: "humidity", Value: >= 30 and <= 60 } => "Optimal",
        { Type: "humidity", Value: > 60 } => "Too Humid",
        _ => "Unknown sensor type"
    };
    Console.WriteLine($"  {reading.SensorId} ({reading.Type}): {reading.Value} → {status}");
}
Console.WriteLine();

// --- List Patterns (C# 11) ---
Console.WriteLine("--- List Patterns (C# 11) ---");

// INTERVIEW ANSWER: List patterns match against the elements of an array or list.
// You can match specific elements, use discard (..) for "the rest", and combine
// with other patterns. They're especially powerful for parsing command structures,
// handling variable-length inputs, or pattern-matching on sequences.
string[][] commands =
[
    ["GET", "/api/users"],
    ["POST", "/api/users", "{\"name\":\"Alice\"}"],
    ["DELETE", "/api/users", "42"],
    ["GET"],
    ["PUT", "/api/users", "42", "{\"name\":\"Bob\"}"],
    [],
];

foreach (var cmd in commands)
{
    var result = cmd switch
    {
        ["GET", var path] => $"Fetching {path}",
        ["POST", var path, var body] => $"Creating at {path} with {body}",
        ["DELETE", var path, var id] => $"Deleting {id} from {path}",
        ["PUT", var path, var id, var body] => $"Updating {id} at {path} with {body}",
        [var method, ..] => $"Unknown method: {method}",
        [] => "Empty command",
    };
    Console.WriteLine($"  [{string.Join(", ", cmd)}] → {result}");
}
Console.WriteLine();

// --- and/or/not Combinators ---
Console.WriteLine("--- and/or/not Combinators ---");

object[] values = [42, "hello", null!, 3.14, -7, "", 100, 0];

foreach (var value in values)
{
    var description = value switch
    {
        null => "null value",
        string { Length: 0 } => "empty string",
        string s => $"string: \"{s}\"",
        int i and > 0 and < 100 => $"small positive int: {i}",
        int i and (< 0 or 0) => $"non-positive int: {i}",
        int i => $"large int: {i}",
        double d and not 0.0 => $"non-zero double: {d}",
        _ => $"other: {value}"
    };
    Console.WriteLine($"  {value?.ToString() ?? "null",-10} → {description}");
}
Console.WriteLine();

// --- Exhaustiveness ---
Console.WriteLine("--- Exhaustiveness Example ---");

// INTERVIEW ANSWER: Switch expressions should be exhaustive — covering all possible
// cases. The compiler warns you if you miss a case (especially with enums). The
// discard pattern (_) acts as a catch-all, but it's better to be explicit when
// possible so the compiler can help you catch bugs when new enum values are added.
var statuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
foreach (var status in statuses)
{
    var emoji = status switch
    {
        "Pending" => "[PENDING]",
        "Processing" => "[PROCESSING]",
        "Shipped" => "[SHIPPED]",
        "Delivered" => "[DELIVERED]",
        "Cancelled" => "[CANCELLED]",
        _ => "[UNKNOWN]" // Catch-all for safety
    };
    Console.WriteLine($"  {status} → {emoji}");
}
