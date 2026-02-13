// ============================================================================
// TOPIC: Proxy Pattern
// ============================================================================
// INTERVIEW ANSWER:
// The Proxy pattern provides a substitute or placeholder for another object. The
// proxy controls access to the original object, letting you perform something
// before or after the request reaches it. Common types: virtual proxy (lazy
// initialization), protection proxy (access control), logging proxy (request
// tracking), and caching proxy (result caching). The proxy implements the same
// interface as the real object, so clients don't know they're using a proxy.
// ============================================================================

// --- Subject interface ---

public interface IDocumentService
{
    Document GetDocument(string id);
    void SaveDocument(Document doc);
    void DeleteDocument(string id);
}

public record Document(string Id, string Title, string Content, string Owner);

// --- Real subject ---

// INTERVIEW ANSWER: The real subject contains the actual business logic. The
// proxy wraps it and adds cross-cutting concerns (caching, auth, logging)
// without the real subject knowing about them.
public class DocumentService : IDocumentService
{
    private readonly Dictionary<string, Document> _store = new()
    {
        ["doc-1"] = new Document("doc-1", "Architecture Guide", "Microservices best practices...", "alice"),
        ["doc-2"] = new Document("doc-2", "API Specification", "REST API endpoints...", "bob"),
        ["doc-3"] = new Document("doc-3", "Confidential Report", "Financial projections...", "admin")
    };

    public Document GetDocument(string id)
    {
        Console.WriteLine($"    [Service] Loading document {id} from database...");
        Thread.Sleep(100);  // Simulate slow database call
        return _store.TryGetValue(id, out var doc)
            ? doc
            : throw new KeyNotFoundException($"Document {id} not found");
    }

    public void SaveDocument(Document doc)
    {
        Console.WriteLine($"    [Service] Saving document {doc.Id} to database...");
        _store[doc.Id] = doc;
    }

    public void DeleteDocument(string id)
    {
        Console.WriteLine($"    [Service] Deleting document {id} from database...");
        _store.Remove(id);
    }
}

// --- Caching proxy ---

// INTERVIEW ANSWER: A caching proxy stores results from expensive operations
// and returns cached copies for subsequent identical requests. This is
// transparent to the client — they call the same interface and get faster
// results. The proxy handles cache invalidation on writes.
public class CachingDocumentProxy : IDocumentService
{
    private readonly IDocumentService _inner;
    private readonly Dictionary<string, Document> _cache = [];
    private int _cacheHits;
    private int _cacheMisses;

    public CachingDocumentProxy(IDocumentService inner)
    {
        _inner = inner;
    }

    public Document GetDocument(string id)
    {
        if (_cache.TryGetValue(id, out var cached))
        {
            _cacheHits++;
            Console.WriteLine($"    [Cache] HIT for {id} (hits: {_cacheHits}, misses: {_cacheMisses})");
            return cached;
        }

        _cacheMisses++;
        Console.WriteLine($"    [Cache] MISS for {id} (hits: {_cacheHits}, misses: {_cacheMisses})");
        var doc = _inner.GetDocument(id);
        _cache[id] = doc;
        return doc;
    }

    public void SaveDocument(Document doc)
    {
        _inner.SaveDocument(doc);
        _cache[doc.Id] = doc;  // Update cache
        Console.WriteLine($"    [Cache] Updated cache for {doc.Id}");
    }

    public void DeleteDocument(string id)
    {
        _inner.DeleteDocument(id);
        _cache.Remove(id);  // Invalidate cache
        Console.WriteLine($"    [Cache] Invalidated cache for {id}");
    }
}

// --- Access control proxy ---

// INTERVIEW ANSWER: A protection proxy checks whether the caller has the
// required permissions before forwarding the request. This separates
// authorization logic from business logic — the real service doesn't need
// to know about permissions.
public class AccessControlProxy : IDocumentService
{
    private readonly IDocumentService _inner;
    private readonly UserContext _user;

    public AccessControlProxy(IDocumentService inner, UserContext user)
    {
        _inner = inner;
        _user = user;
    }

    public Document GetDocument(string id)
    {
        Console.WriteLine($"    [Auth] Checking read access for {_user.Username} (role: {_user.Role})");
        return _inner.GetDocument(id);
    }

    public void SaveDocument(Document doc)
    {
        Console.WriteLine($"    [Auth] Checking write access for {_user.Username} (role: {_user.Role})");
        if (_user.Role is not ("admin" or "editor"))
            throw new UnauthorizedAccessException($"User {_user.Username} cannot write documents");

        _inner.SaveDocument(doc);
    }

    public void DeleteDocument(string id)
    {
        Console.WriteLine($"    [Auth] Checking delete access for {_user.Username} (role: {_user.Role})");
        if (_user.Role is not "admin")
            throw new UnauthorizedAccessException($"User {_user.Username} cannot delete documents");

        _inner.DeleteDocument(id);
    }
}

public record UserContext(string Username, string Role);

// --- Logging proxy ---

public class LoggingDocumentProxy : IDocumentService
{
    private readonly IDocumentService _inner;

    public LoggingDocumentProxy(IDocumentService inner)
    {
        _inner = inner;
    }

    public Document GetDocument(string id)
    {
        Console.WriteLine($"    [Log] GetDocument({id}) called at {DateTime.UtcNow:HH:mm:ss}");
        var result = _inner.GetDocument(id);
        Console.WriteLine($"    [Log] GetDocument({id}) returned \"{result.Title}\"");
        return result;
    }

    public void SaveDocument(Document doc)
    {
        Console.WriteLine($"    [Log] SaveDocument({doc.Id}) called at {DateTime.UtcNow:HH:mm:ss}");
        _inner.SaveDocument(doc);
        Console.WriteLine($"    [Log] SaveDocument({doc.Id}) completed");
    }

    public void DeleteDocument(string id)
    {
        Console.WriteLine($"    [Log] DeleteDocument({id}) called at {DateTime.UtcNow:HH:mm:ss}");
        _inner.DeleteDocument(id);
        Console.WriteLine($"    [Log] DeleteDocument({id}) completed");
    }
}

// --- Virtual proxy (lazy initialization) ---

public class LazyDocumentService : IDocumentService
{
    private IDocumentService? _realService;

    private IDocumentService RealService
    {
        get
        {
            if (_realService is null)
            {
                Console.WriteLine("    [Lazy] Initializing real service (first use)...");
                _realService = new DocumentService();
            }
            return _realService;
        }
    }

    public Document GetDocument(string id) => RealService.GetDocument(id);
    public void SaveDocument(Document doc) => RealService.SaveDocument(doc);
    public void DeleteDocument(string id) => RealService.DeleteDocument(id);
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== PROXY PATTERN DEMO ===\n");

// --- Caching Proxy ---
Console.WriteLine("--- Caching Proxy ---");
IDocumentService service = new CachingDocumentProxy(new DocumentService());

var doc1 = service.GetDocument("doc-1");  // Cache miss
Console.WriteLine($"  Got: {doc1.Title}\n");

var doc1Again = service.GetDocument("doc-1");  // Cache hit
Console.WriteLine($"  Got again: {doc1Again.Title}\n");

service.SaveDocument(doc1 with { Title = "Updated Architecture Guide" });
var updated = service.GetDocument("doc-1");  // Cache hit (updated)
Console.WriteLine($"  After update: {updated.Title}\n");

// --- Access Control Proxy ---
Console.WriteLine("--- Access Control Proxy ---");

var adminService = new AccessControlProxy(new DocumentService(), new UserContext("admin_user", "admin"));
adminService.SaveDocument(new Document("doc-4", "New Doc", "Content", "admin_user"));
Console.WriteLine();

var viewerService = new AccessControlProxy(new DocumentService(), new UserContext("viewer_user", "viewer"));
try
{
    viewerService.DeleteDocument("doc-1");
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($"  Access denied: {ex.Message}\n");
}

// --- Stacked proxies: Log → Auth → Cache → Service ---
Console.WriteLine("--- Stacked Proxies (Log → Auth → Cache → Service) ---");
IDocumentService fullStack =
    new LoggingDocumentProxy(
        new AccessControlProxy(
            new CachingDocumentProxy(
                new DocumentService()),
            new UserContext("editor_user", "editor")));

var result = fullStack.GetDocument("doc-2");
Console.WriteLine($"\n  Final result: {result.Title}\n");

// Second call hits cache (no database access)
Console.WriteLine("  Second call (cached):");
result = fullStack.GetDocument("doc-2");
Console.WriteLine($"  Final result: {result.Title}\n");

// --- Lazy Proxy ---
Console.WriteLine("--- Lazy (Virtual) Proxy ---");
IDocumentService lazyService = new LazyDocumentService();
Console.WriteLine("  Service created but NOT initialized yet");
Console.WriteLine("  Calling GetDocument for the first time...");
var lazyDoc = lazyService.GetDocument("doc-1");
Console.WriteLine($"  Got: {lazyDoc.Title}\n");

Console.WriteLine("  Calling again (already initialized)...");
lazyDoc = lazyService.GetDocument("doc-2");
Console.WriteLine($"  Got: {lazyDoc.Title}");
