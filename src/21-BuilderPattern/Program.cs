// ============================================================================
// TOPIC: Builder Pattern
// ============================================================================
// INTERVIEW ANSWER:
// The Builder pattern lets you construct complex objects step by step. Instead
// of a constructor with many parameters (telescoping constructor problem), you
// chain method calls that each configure one aspect of the object. The pattern
// separates construction from representation so the same building process can
// create different representations. It's especially useful when an object has
// many optional parameters or requires a specific construction order.
// ============================================================================

// --- Product ---

// INTERVIEW ANSWER: The product is the complex object being built. It should
// be immutable once constructed — the builder handles all the mutation during
// construction, then produces a finished object.
public class HttpRequest
{
    public string Method { get; }
    public string Url { get; }
    public Dictionary<string, string> Headers { get; }
    public Dictionary<string, string> QueryParams { get; }
    public string? Body { get; }
    public TimeSpan Timeout { get; }
    public int MaxRetries { get; }
    public bool FollowRedirects { get; }

    // Internal constructor — only the builder can create instances
    internal HttpRequest(
        string method, string url,
        Dictionary<string, string> headers,
        Dictionary<string, string> queryParams,
        string? body, TimeSpan timeout,
        int maxRetries, bool followRedirects)
    {
        Method = method;
        Url = url;
        Headers = headers;
        QueryParams = queryParams;
        Body = body;
        Timeout = timeout;
        MaxRetries = maxRetries;
        FollowRedirects = followRedirects;
    }

    public void Describe()
    {
        Console.WriteLine($"  {Method} {Url}");
        if (QueryParams.Count > 0)
            Console.WriteLine($"  Query: {string.Join("&", QueryParams.Select(kv => $"{kv.Key}={kv.Value}"))}");
        foreach (var header in Headers)
            Console.WriteLine($"  Header: {header.Key}: {header.Value}");
        if (Body is not null)
            Console.WriteLine($"  Body: {Body[..Math.Min(Body.Length, 80)]}");
        Console.WriteLine($"  Timeout: {Timeout.TotalSeconds}s, Retries: {MaxRetries}, Follow Redirects: {FollowRedirects}");
    }
}

// --- Builder ---

// INTERVIEW ANSWER: The builder provides a fluent API for constructing the
// product. Each method returns 'this' so calls can be chained. The Build()
// method performs final validation and creates the product.
public class HttpRequestBuilder
{
    private string _method = "GET";
    private string _url = "";
    private readonly Dictionary<string, string> _headers = [];
    private readonly Dictionary<string, string> _queryParams = [];
    private string? _body;
    private TimeSpan _timeout = TimeSpan.FromSeconds(30);
    private int _maxRetries = 0;
    private bool _followRedirects = true;

    public HttpRequestBuilder SetMethod(string method)
    {
        _method = method.ToUpperInvariant();
        return this;
    }

    public HttpRequestBuilder SetUrl(string url)
    {
        _url = url;
        return this;
    }

    public HttpRequestBuilder AddHeader(string name, string value)
    {
        _headers[name] = value;
        return this;
    }

    public HttpRequestBuilder AddQueryParam(string key, string value)
    {
        _queryParams[key] = value;
        return this;
    }

    public HttpRequestBuilder SetBody(string body)
    {
        _body = body;
        return this;
    }

    public HttpRequestBuilder SetTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    public HttpRequestBuilder SetMaxRetries(int retries)
    {
        _maxRetries = retries;
        return this;
    }

    public HttpRequestBuilder SetFollowRedirects(bool follow)
    {
        _followRedirects = follow;
        return this;
    }

    // INTERVIEW ANSWER: Build() is where validation happens. The builder
    // accumulates configuration, then validates everything at construction
    // time. This is better than validating in the product constructor because
    // the builder can give clear error messages about what's missing.
    public HttpRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_url))
            throw new InvalidOperationException("URL is required");

        if (_method is "GET" or "DELETE" && _body is not null)
            throw new InvalidOperationException($"{_method} requests should not have a body");

        return new HttpRequest(
            _method, _url,
            new Dictionary<string, string>(_headers),
            new Dictionary<string, string>(_queryParams),
            _body, _timeout, _maxRetries, _followRedirects);
    }
}

// --- Director (optional) ---

// INTERVIEW ANSWER: The Director is an optional piece that encapsulates common
// building recipes. It uses a builder to construct frequently needed configurations.
// In practice, static factory methods or extension methods often play this role.
public static class HttpRequestDirector
{
    public static HttpRequest CreateJsonGet(string url)
    {
        return new HttpRequestBuilder()
            .SetMethod("GET")
            .SetUrl(url)
            .AddHeader("Accept", "application/json")
            .AddHeader("User-Agent", "StudyApp/1.0")
            .SetTimeout(TimeSpan.FromSeconds(15))
            .SetMaxRetries(3)
            .Build();
    }

    public static HttpRequest CreateJsonPost(string url, string jsonBody)
    {
        return new HttpRequestBuilder()
            .SetMethod("POST")
            .SetUrl(url)
            .AddHeader("Content-Type", "application/json")
            .AddHeader("Accept", "application/json")
            .SetBody(jsonBody)
            .SetTimeout(TimeSpan.FromSeconds(30))
            .SetMaxRetries(1)
            .Build();
    }

    public static HttpRequest CreateAuthenticatedRequest(string url, string token)
    {
        return new HttpRequestBuilder()
            .SetMethod("GET")
            .SetUrl(url)
            .AddHeader("Authorization", $"Bearer {token}")
            .AddHeader("Accept", "application/json")
            .SetFollowRedirects(false)
            .Build();
    }
}

// --- Separate builder for a different product (shows reusability) ---

public class ConnectionString
{
    public string Server { get; }
    public string Database { get; }
    public int Port { get; }
    public string? Username { get; }
    public string? Password { get; }
    public bool Encrypt { get; }
    public int ConnectionTimeout { get; }

    internal ConnectionString(string server, string database, int port,
        string? username, string? password, bool encrypt, int connectionTimeout)
    {
        Server = server;
        Database = database;
        Port = port;
        Username = username;
        Password = password;
        Encrypt = encrypt;
        ConnectionTimeout = connectionTimeout;
    }

    public override string ToString()
    {
        var parts = new List<string>
        {
            $"Server={Server},{Port}",
            $"Database={Database}",
            $"Encrypt={Encrypt}",
            $"Connection Timeout={ConnectionTimeout}"
        };
        if (Username is not null)
            parts.Add($"User Id={Username}");
        if (Password is not null)
            parts.Add($"Password=***");
        return string.Join(";", parts);
    }
}

public class ConnectionStringBuilder
{
    private string _server = "localhost";
    private string _database = "";
    private int _port = 1433;
    private string? _username;
    private string? _password;
    private bool _encrypt = true;
    private int _connectionTimeout = 30;

    public ConnectionStringBuilder Server(string server) { _server = server; return this; }
    public ConnectionStringBuilder Database(string db) { _database = db; return this; }
    public ConnectionStringBuilder Port(int port) { _port = port; return this; }
    public ConnectionStringBuilder Credentials(string user, string pass) { _username = user; _password = pass; return this; }
    public ConnectionStringBuilder Encrypt(bool encrypt) { _encrypt = encrypt; return this; }
    public ConnectionStringBuilder Timeout(int seconds) { _connectionTimeout = seconds; return this; }

    public ConnectionString Build()
    {
        if (string.IsNullOrWhiteSpace(_database))
            throw new InvalidOperationException("Database name is required");

        return new ConnectionString(_server, _database, _port,
            _username, _password, _encrypt, _connectionTimeout);
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== BUILDER PATTERN DEMO ===\n");

// --- Basic builder usage ---
Console.WriteLine("--- Basic Builder Usage ---");
var request = new HttpRequestBuilder()
    .SetMethod("GET")
    .SetUrl("https://api.example.com/users")
    .AddHeader("Accept", "application/json")
    .AddQueryParam("page", "1")
    .AddQueryParam("limit", "25")
    .SetTimeout(TimeSpan.FromSeconds(10))
    .SetMaxRetries(3)
    .Build();

request.Describe();

// --- POST with body ---
Console.WriteLine("\n--- POST Request ---");
var postRequest = new HttpRequestBuilder()
    .SetMethod("POST")
    .SetUrl("https://api.example.com/orders")
    .AddHeader("Content-Type", "application/json")
    .AddHeader("X-Correlation-Id", Guid.NewGuid().ToString())
    .SetBody("""{"item": "Widget", "quantity": 5}""")
    .SetMaxRetries(1)
    .Build();

postRequest.Describe();

// --- Director pre-built recipes ---
Console.WriteLine("\n--- Director Recipes ---");
Console.WriteLine("\nJSON GET:");
HttpRequestDirector.CreateJsonGet("https://api.example.com/products").Describe();

Console.WriteLine("\nJSON POST:");
HttpRequestDirector.CreateJsonPost(
    "https://api.example.com/orders",
    """{"item": "Gadget", "qty": 10}""").Describe();

Console.WriteLine("\nAuthenticated Request:");
HttpRequestDirector.CreateAuthenticatedRequest(
    "https://api.example.com/me",
    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9").Describe();

// --- Validation ---
Console.WriteLine("\n--- Builder Validation ---");
try
{
    new HttpRequestBuilder()
        .SetMethod("GET")
        .SetBody("should fail")
        .SetUrl("https://example.com")
        .Build();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"  Caught: {ex.Message}");
}

// --- Connection string builder ---
Console.WriteLine("\n--- Connection String Builder ---");
var connStr = new ConnectionStringBuilder()
    .Server("db.production.internal")
    .Database("OrdersDb")
    .Port(5432)
    .Credentials("app_user", "s3cret")
    .Encrypt(true)
    .Timeout(15)
    .Build();

Console.WriteLine($"  {connStr}");

var localConn = new ConnectionStringBuilder()
    .Database("TestDb")
    .Encrypt(false)
    .Build();

Console.WriteLine($"  {localConn}");
