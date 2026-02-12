// ============================================================================
// TOPIC: Exception Handling
// ============================================================================
// INTERVIEW ANSWER:
// Exceptions are for exceptional, unexpected situations — not for normal control
// flow. C# uses try/catch/finally for structured exception handling. The `when`
// keyword adds exception filters that run BEFORE the stack unwinds, which is useful
// for conditional catches. Custom exceptions should derive from Exception (not
// ApplicationException). In performance-critical paths, consider the Result<T>
// pattern instead of exceptions, since throw/catch has significant overhead.
// ============================================================================

using System.Runtime.ExceptionServices;

// --- Custom Exception Hierarchy ---

// INTERVIEW ANSWER: Custom exceptions should have a meaningful name that describes
// the problem, include relevant context as properties, and always include the
// standard constructors (message, inner exception). Derive from Exception directly
// — ApplicationException is a .NET 1.0 relic that adds no value.
public class ApiException : Exception
{
    public int StatusCode { get; }
    public string? RequestId { get; }

    public ApiException(string message, int statusCode, string? requestId = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        RequestId = requestId;
    }
}

public class RateLimitException : ApiException
{
    public TimeSpan RetryAfter { get; }

    public RateLimitException(TimeSpan retryAfter, string? requestId = null)
        : base($"Rate limited. Retry after {retryAfter.TotalSeconds}s", 429, requestId)
    {
        RetryAfter = retryAfter;
    }
}

public class NotFoundException : ApiException
{
    public string ResourceType { get; }
    public string ResourceId { get; }

    public NotFoundException(string resourceType, string resourceId)
        : base($"{resourceType} '{resourceId}' not found", 404)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}

// --- Simulated API Client ---

public class ApiClient
{
    private int _requestCount;

    public async Task<string> GetResourceAsync(string resourceId)
    {
        _requestCount++;
        var requestId = $"req-{_requestCount:D3}";

        await Task.Delay(10);

        return resourceId switch
        {
            "rate-limited" => throw new RateLimitException(TimeSpan.FromSeconds(30), requestId),
            "not-found" => throw new NotFoundException("User", resourceId),
            "server-error" => throw new ApiException("Internal server error", 500, requestId),
            "timeout" => throw new TimeoutException("Request timed out"),
            _ => $"{{\"id\":\"{resourceId}\",\"data\":\"OK\",\"requestId\":\"{requestId}\"}}"
        };
    }
}

// --- Result<T> Alternative ---

// INTERVIEW ANSWER: For expected failures (like validation errors or "not found"),
// a Result type is often better than exceptions. Exceptions have overhead from
// stack unwinding and should be for truly unexpected situations. Result makes
// the failure case explicit in the type system — the caller MUST handle it.
public class Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess { get; }

    private Result(T value) { Value = value; IsSuccess = true; }
    private Result(string error) { Error = error; IsSuccess = false; }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(string error) => new(error);
}

public class UserValidator
{
    public Result<string> ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<string>.Fail("Email is required");
        if (!email.Contains('@'))
            return Result<string>.Fail("Email must contain @");
        if (email.Length > 254)
            return Result<string>.Fail("Email too long");
        return Result<string>.Ok(email.ToLowerInvariant());
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== EXCEPTION HANDLING DEMO ===\n");

var client = new ApiClient();

// --- Basic try/catch/finally ---
Console.WriteLine("--- Basic try/catch/finally ---");
try
{
    var result = await client.GetResourceAsync("user-123");
    Console.WriteLine($"  Success: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"  Error: {ex.Message}");
}
finally
{
    // INTERVIEW ANSWER: `finally` ALWAYS runs — whether there was an exception
    // or not, whether the catch re-throws or not. It's guaranteed cleanup. Use
    // it for releasing resources (though `using` is usually cleaner for IDisposable).
    Console.WriteLine("  Finally: cleanup runs regardless");
}
Console.WriteLine();

// --- Exception Filters (when) ---
Console.WriteLine("--- Exception Filters (when keyword) ---");

// INTERVIEW ANSWER: Exception filters with `when` are evaluated BEFORE the stack
// unwinds. If the filter returns false, the runtime keeps looking for a matching
// catch. This is better than catching and re-throwing because: (1) the original
// stack trace is preserved, (2) debuggers can break at the throw site, and
// (3) finally blocks in intermediate frames haven't run yet.
try
{
    await client.GetResourceAsync("rate-limited");
}
catch (RateLimitException ex) when (ex.RetryAfter.TotalSeconds < 60)
{
    Console.WriteLine($"  Short rate limit ({ex.RetryAfter.TotalSeconds}s) — will retry");
}
catch (RateLimitException ex) when (ex.RetryAfter.TotalSeconds >= 60)
{
    Console.WriteLine($"  Long rate limit ({ex.RetryAfter.TotalSeconds}s) — giving up");
}
catch (ApiException ex) when (ex.StatusCode >= 500)
{
    Console.WriteLine($"  Server error ({ex.StatusCode}) — should retry");
}
catch (ApiException ex) when (ex.StatusCode >= 400)
{
    Console.WriteLine($"  Client error ({ex.StatusCode}) — don't retry");
}
Console.WriteLine();

// --- Catch hierarchy ---
Console.WriteLine("--- Exception Hierarchy (most specific first) ---");
string[] testCases = ["not-found", "server-error", "timeout", "valid-user"];

foreach (var testCase in testCases)
{
    try
    {
        var result = await client.GetResourceAsync(testCase);
        Console.WriteLine($"  [{testCase}] Success: {result}");
    }
    catch (NotFoundException ex)
    {
        Console.WriteLine($"  [{testCase}] Not found: {ex.ResourceType} '{ex.ResourceId}'");
    }
    catch (ApiException ex)
    {
        Console.WriteLine($"  [{testCase}] API Error {ex.StatusCode}: {ex.Message} (Request: {ex.RequestId})");
    }
    catch (TimeoutException ex)
    {
        Console.WriteLine($"  [{testCase}] Timeout: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  [{testCase}] Unexpected: {ex.GetType().Name}: {ex.Message}");
    }
}
Console.WriteLine();

// --- ExceptionDispatchInfo ---
Console.WriteLine("--- ExceptionDispatchInfo (preserve stack trace) ---");

// INTERVIEW ANSWER: If you catch an exception, do some work, and then need to
// re-throw it later (maybe on a different thread), `throw ex` resets the stack
// trace. ExceptionDispatchInfo.Capture() preserves the original stack trace
// so the re-thrown exception looks like it came from the original throw site.
ExceptionDispatchInfo? captured = null;

try
{
    await client.GetResourceAsync("server-error");
}
catch (ApiException ex)
{
    captured = ExceptionDispatchInfo.Capture(ex);
    Console.WriteLine("  Captured exception for later re-throw");
}

if (captured is not null)
{
    try
    {
        Console.WriteLine("  Re-throwing with preserved stack trace...");
        captured.Throw(); // Preserves original stack trace!
    }
    catch (ApiException ex)
    {
        Console.WriteLine($"  Re-caught: {ex.Message}");
        Console.WriteLine($"  Stack trace preserved: {ex.StackTrace?.Contains("GetResourceAsync") == true}");
    }
}
Console.WriteLine();

// --- Result<T> vs Exceptions ---
Console.WriteLine("--- Result<T> vs Exceptions ---");

var validator = new UserValidator();

string[] emails = ["alice@example.com", "", "bad-email", "valid@test.org"];
foreach (var email in emails)
{
    var result = validator.ValidateEmail(email);
    Console.WriteLine(result.IsSuccess
        ? $"  '{email}' → Valid: {result.Value}"
        : $"  '{email}' → Invalid: {result.Error}");
}

Console.WriteLine();
Console.WriteLine("  INTERVIEW ANSWER: Use exceptions for truly unexpected failures");
Console.WriteLine("  (network errors, corrupt data, bugs). Use Result<T> for expected");
Console.WriteLine("  failures (validation, not-found, business rules). Exceptions have");
Console.WriteLine("  real perf cost from stack unwinding — don't use them for control flow.");
