// ============================================================================
// TOPIC: Decorator Pattern
// ============================================================================
// INTERVIEW ANSWER:
// The Decorator pattern lets you attach new behaviors to objects by wrapping them
// in special wrapper objects. Each decorator implements the same interface as the
// object it wraps, so decorators can be stacked. This follows the Open/Closed
// principle — you extend behavior without modifying existing classes. It's an
// alternative to subclassing: instead of creating a class for every combination
// of features, you compose them at runtime.
// ============================================================================

// --- Component interface ---

public interface INotificationSender
{
    void Send(string recipient, string message);
}

// --- Concrete component ---

// INTERVIEW ANSWER: The concrete component provides the default behavior.
// Decorators wrap it (and each other) to layer on additional behavior.
public class EmailNotificationSender : INotificationSender
{
    public void Send(string recipient, string message)
    {
        Console.WriteLine($"    [Email] To: {recipient} | Message: {message}");
    }
}

public class SmsNotificationSender : INotificationSender
{
    public void Send(string recipient, string message)
    {
        Console.WriteLine($"    [SMS] To: {recipient} | Message: {message}");
    }
}

// --- Base decorator ---

// INTERVIEW ANSWER: The base decorator holds a reference to the wrapped component
// and delegates all calls to it. Concrete decorators extend this to add behavior
// before or after delegating. The key insight is that the decorator IS-A component
// (implements the interface) and HAS-A component (wraps one via composition).
public abstract class NotificationDecorator : INotificationSender
{
    protected readonly INotificationSender _inner;

    protected NotificationDecorator(INotificationSender inner)
    {
        _inner = inner;
    }

    public virtual void Send(string recipient, string message)
    {
        _inner.Send(recipient, message);
    }
}

// --- Concrete decorators ---

public class LoggingDecorator : NotificationDecorator
{
    public LoggingDecorator(INotificationSender inner) : base(inner) { }

    public override void Send(string recipient, string message)
    {
        Console.WriteLine($"    [Log] Sending notification to {recipient} at {DateTime.UtcNow:HH:mm:ss}");
        base.Send(recipient, message);
        Console.WriteLine($"    [Log] Notification sent successfully");
    }
}

public class RetryDecorator : NotificationDecorator
{
    private readonly int _maxRetries;

    public RetryDecorator(INotificationSender inner, int maxRetries = 3) : base(inner)
    {
        _maxRetries = maxRetries;
    }

    public override void Send(string recipient, string message)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                Console.WriteLine($"    [Retry] Attempt {attempt}/{_maxRetries}");
                base.Send(recipient, message);
                return;  // Success — stop retrying
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                Console.WriteLine($"    [Retry] Attempt {attempt} failed: {ex.Message}, retrying...");
            }
        }
    }
}

public class RateLimitDecorator : NotificationDecorator
{
    private readonly Dictionary<string, DateTime> _lastSent = [];
    private readonly TimeSpan _cooldown;

    public RateLimitDecorator(INotificationSender inner, TimeSpan cooldown) : base(inner)
    {
        _cooldown = cooldown;
    }

    public override void Send(string recipient, string message)
    {
        if (_lastSent.TryGetValue(recipient, out var lastTime)
            && DateTime.UtcNow - lastTime < _cooldown)
        {
            Console.WriteLine($"    [RateLimit] Blocked — {recipient} was notified " +
                              $"{(DateTime.UtcNow - lastTime).TotalSeconds:F0}s ago (cooldown: {_cooldown.TotalSeconds}s)");
            return;
        }

        base.Send(recipient, message);
        _lastSent[recipient] = DateTime.UtcNow;
    }
}

public class EncryptionDecorator : NotificationDecorator
{
    public EncryptionDecorator(INotificationSender inner) : base(inner) { }

    public override void Send(string recipient, string message)
    {
        var encrypted = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(message));
        Console.WriteLine($"    [Encrypt] Message encrypted ({message.Length} chars → {encrypted.Length} chars)");
        base.Send(recipient, encrypted);
    }
}

// --- Real-world example: Stream-like data pipeline ---

// INTERVIEW ANSWER: .NET's Stream class is the canonical example of the
// Decorator pattern. BufferedStream wraps a Stream, GZipStream wraps a Stream,
// CryptoStream wraps a Stream — they all implement Stream and add behavior.
// Here's a similar pattern for data transformation.
public interface IDataReader
{
    string Read();
}

public class FileDataReader : IDataReader
{
    private readonly string _data;
    public FileDataReader(string data) => _data = data;
    public string Read() => _data;
}

public class UpperCaseDecorator : IDataReader
{
    private readonly IDataReader _inner;
    public UpperCaseDecorator(IDataReader inner) => _inner = inner;
    public string Read() => _inner.Read().ToUpperInvariant();
}

public class TrimDecorator : IDataReader
{
    private readonly IDataReader _inner;
    private readonly int _maxLength;
    public TrimDecorator(IDataReader inner, int maxLength) { _inner = inner; _maxLength = maxLength; }
    public string Read()
    {
        var data = _inner.Read();
        return data.Length > _maxLength ? data[.._maxLength] + "..." : data;
    }
}

public class PrefixDecorator : IDataReader
{
    private readonly IDataReader _inner;
    private readonly string _prefix;
    public PrefixDecorator(IDataReader inner, string prefix) { _inner = inner; _prefix = prefix; }
    public string Read() => $"{_prefix}{_inner.Read()}";
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== DECORATOR PATTERN DEMO ===\n");

// --- Plain notification ---
Console.WriteLine("--- Plain Email ---");
INotificationSender sender = new EmailNotificationSender();
sender.Send("alice@example.com", "Your order has shipped");

// --- Single decorator ---
Console.WriteLine("\n--- Email + Logging ---");
sender = new LoggingDecorator(new EmailNotificationSender());
sender.Send("bob@example.com", "Welcome to our platform");

// --- Stacked decorators ---
Console.WriteLine("\n--- SMS + Logging + Retry ---");
sender = new LoggingDecorator(
            new RetryDecorator(
                new SmsNotificationSender(), maxRetries: 2));
sender.Send("+15551234567", "Your verification code is 483921");

// --- Full stack: Encrypt → RateLimit → Log → Email ---
Console.WriteLine("\n--- Full Stack: Encrypt → RateLimit → Log → Email ---");
sender = new EncryptionDecorator(
            new RateLimitDecorator(
                new LoggingDecorator(
                    new EmailNotificationSender()),
                cooldown: TimeSpan.FromSeconds(5)));

sender.Send("secure@example.com", "Secret message");

Console.WriteLine("\n  Sending again immediately (should be rate-limited):");
sender.Send("secure@example.com", "Another secret");

Console.WriteLine("\n  Sending to different recipient (should go through):");
sender.Send("other@example.com", "Different recipient");

// --- Data pipeline ---
Console.WriteLine("\n--- Data Pipeline (Stream-like) ---");
IDataReader reader = new PrefixDecorator(
                        new TrimDecorator(
                            new UpperCaseDecorator(
                                new FileDataReader("  hello world, this is a long message from the file system  ")),
                            maxLength: 30),
                        prefix: "[DATA] ");

Console.WriteLine($"  Result: {reader.Read()}");

// Show how composition order matters
IDataReader reader2 = new UpperCaseDecorator(
                        new PrefixDecorator(
                            new TrimDecorator(
                                new FileDataReader("  hello world, this is a long message from the file system  "),
                                maxLength: 30),
                            prefix: "[data] "));

Console.WriteLine($"  Different order: {reader2.Read()}");
