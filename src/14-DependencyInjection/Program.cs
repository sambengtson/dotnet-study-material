// ============================================================================
// TOPIC: Dependency Injection
// ============================================================================
// INTERVIEW ANSWER:
// Dependency Injection is a design pattern where a class receives its dependencies
// from the outside rather than creating them itself. Instead of `new`-ing up a
// database client inside your service, you declare it as a constructor parameter
// and let a DI container provide it. This makes your code testable (you can inject
// mocks), loosely coupled (you depend on interfaces, not implementations), and
// easier to maintain (wiring is centralized in one place).
// ============================================================================

using Microsoft.Extensions.DependencyInjection;

// --- Interfaces (abstractions) ---

public interface IUserRepository
{
    Task<UserDto?> GetByIdAsync(string id);
    Task<IReadOnlyList<UserDto>> GetAllAsync();
    Task SaveAsync(UserDto user);
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

public interface ILogger
{
    void Info(string message);
    void Error(string message, Exception? ex = null);
}

public record UserDto(string Id, string Name, string Email);

// --- Implementations ---

public class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<string, UserDto> _users = [];
    private readonly ILogger _logger;

    // INTERVIEW ANSWER: This is constructor injection — the most common form
    // of DI. The class declares what it needs in its constructor, and the DI
    // container provides instances automatically. The class doesn't know or care
    // where the logger comes from.
    public InMemoryUserRepository(ILogger logger)
    {
        _logger = logger;
        _logger.Info("InMemoryUserRepository created");
    }

    public Task<UserDto?> GetByIdAsync(string id)
    {
        _logger.Info($"Getting user {id}");
        return Task.FromResult(_users.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<UserDto>> GetAllAsync()
    {
        _logger.Info($"Getting all users ({_users.Count} total)");
        return Task.FromResult<IReadOnlyList<UserDto>>(_users.Values.ToList());
    }

    public Task SaveAsync(UserDto user)
    {
        _users[user.Id] = user;
        _logger.Info($"Saved user {user.Id}: {user.Name}");
        return Task.CompletedTask;
    }
}

public class ConsoleEmailService : IEmailService
{
    private readonly ILogger _logger;

    public ConsoleEmailService(ILogger logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string body)
    {
        _logger.Info($"Sending email to {to}");
        Console.WriteLine($"  [Email] To: {to} | Subject: {subject} | Body: {body}");
        return Task.CompletedTask;
    }
}

public class ConsoleLogger : ILogger
{
    private readonly string _instanceId = Guid.NewGuid().ToString()[..4];

    public ConsoleLogger() =>
        Console.WriteLine($"  [Logger:{_instanceId}] Created");

    public void Info(string message) =>
        Console.WriteLine($"  [Logger:{_instanceId}] INFO: {message}");

    public void Error(string message, Exception? ex = null) =>
        Console.WriteLine($"  [Logger:{_instanceId}] ERROR: {message} {ex?.Message}");
}

// --- Service that depends on multiple abstractions ---

public class UserRegistrationService
{
    private readonly IUserRepository _userRepo;
    private readonly IEmailService _emailService;
    private readonly ILogger _logger;

    // INTERVIEW ANSWER: Look at this constructor — it takes interfaces, not
    // concrete classes. This service has NO idea if it's using a real database
    // or an in-memory fake, a real SMTP client or a console stub. That's the
    // power of DI: the service is completely decoupled from its dependencies'
    // implementations.
    public UserRegistrationService(
        IUserRepository userRepo,
        IEmailService emailService,
        ILogger logger)
    {
        _userRepo = userRepo;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<bool> RegisterAsync(string name, string email)
    {
        _logger.Info($"Registering user: {name} ({email})");

        var userId = $"user-{Guid.NewGuid().ToString()[..6]}";
        var user = new UserDto(userId, name, email);

        await _userRepo.SaveAsync(user);
        await _emailService.SendAsync(email, "Welcome!", $"Hi {name}, welcome aboard!");

        _logger.Info($"Registration complete for {userId}");
        return true;
    }

    public async Task<UserDto?> GetUserAsync(string id)
    {
        return await _userRepo.GetByIdAsync(id);
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== DEPENDENCY INJECTION DEMO ===\n");

// --- Setting up the DI Container ---
Console.WriteLine("--- Setting Up DI Container ---\n");

// INTERVIEW ANSWER: The DI container manages object creation and lifetimes.
// You register services with their interfaces, specifying the lifetime:
// - Transient: new instance every time it's requested
// - Scoped: one instance per scope (per request in web apps)
// - Singleton: one instance for the entire application lifetime

var services = new ServiceCollection();

// Singleton — one instance shared everywhere
services.AddSingleton<ILogger, ConsoleLogger>();

// Scoped — one instance per scope (we'll create scopes manually)
services.AddScoped<IUserRepository, InMemoryUserRepository>();

// Transient — new instance every time
services.AddTransient<IEmailService, ConsoleEmailService>();

// Register the service itself
services.AddScoped<UserRegistrationService>();

var serviceProvider = services.BuildServiceProvider();

// --- Demonstrate Lifetimes ---
Console.WriteLine("\n--- Lifetime Differences ---\n");

Console.WriteLine("Singleton (same instance everywhere):");
var logger1 = serviceProvider.GetRequiredService<ILogger>();
var logger2 = serviceProvider.GetRequiredService<ILogger>();
Console.WriteLine($"  Same instance? {ReferenceEquals(logger1, logger2)}\n");

// INTERVIEW ANSWER: Scoped means "one per scope." In ASP.NET Core, a scope
// is typically one HTTP request. In a console app, you create scopes manually.
// Within a scope, you always get the same instance. Different scopes get
// different instances.
Console.WriteLine("Scoped (same within scope, different across scopes):");
using (var scope1 = serviceProvider.CreateScope())
{
    var repo1a = scope1.ServiceProvider.GetRequiredService<IUserRepository>();
    var repo1b = scope1.ServiceProvider.GetRequiredService<IUserRepository>();
    Console.WriteLine($"  Scope 1 — Same instance? {ReferenceEquals(repo1a, repo1b)}");
}
using (var scope2 = serviceProvider.CreateScope())
{
    var repo2 = scope2.ServiceProvider.GetRequiredService<IUserRepository>();
    Console.WriteLine($"  Scope 2 — New instance created");
}
Console.WriteLine();

Console.WriteLine("Transient (new instance every time):");
using (var scope = serviceProvider.CreateScope())
{
    var email1 = scope.ServiceProvider.GetRequiredService<IEmailService>();
    var email2 = scope.ServiceProvider.GetRequiredService<IEmailService>();
    Console.WriteLine($"  Same instance? {ReferenceEquals(email1, email2)}\n");
}

// --- Using the registered services ---
Console.WriteLine("--- Using Registered Services ---\n");

using (var scope = serviceProvider.CreateScope())
{
    var registration = scope.ServiceProvider.GetRequiredService<UserRegistrationService>();

    await registration.RegisterAsync("Alice Chen", "alice@example.com");
    Console.WriteLine();
    await registration.RegisterAsync("Bob Martinez", "bob@example.com");
}

Console.WriteLine();

// --- Show that scoped state doesn't leak ---
Console.WriteLine("--- Scoped State Isolation ---\n");

Console.WriteLine("Scope A: Register users...");
using (var scopeA = serviceProvider.CreateScope())
{
    var regA = scopeA.ServiceProvider.GetRequiredService<UserRegistrationService>();
    await regA.RegisterAsync("Scope-A User", "a@test.com");
    var repoA = scopeA.ServiceProvider.GetRequiredService<IUserRepository>();
    var allA = await repoA.GetAllAsync();
    Console.WriteLine($"  Scope A user count: {allA.Count}");
}

Console.WriteLine("\nScope B: Fresh repository (new scope)...");
using (var scopeB = serviceProvider.CreateScope())
{
    var repoB = scopeB.ServiceProvider.GetRequiredService<IUserRepository>();
    var allB = await repoB.GetAllAsync();
    Console.WriteLine($"  Scope B user count: {allB.Count} (fresh — different scope!)");
}

// INTERVIEW ANSWER: Notice how Scope B has zero users even though Scope A
// registered one. That's because IUserRepository is scoped — each scope gets
// its own InMemoryUserRepository instance. In a web app, this means each HTTP
// request gets its own DbContext, preventing data leaks between requests.
