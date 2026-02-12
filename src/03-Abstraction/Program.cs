// ============================================================================
// TOPIC: Abstraction
// ============================================================================
// INTERVIEW ANSWER:
// Abstraction is about hiding implementation complexity and exposing only what
// consumers need. In C#, we achieve this with abstract classes (which can have
// both implemented and unimplemented members) and interfaces. The key idea is
// that calling code depends on the WHAT (the abstract contract) not the HOW
// (the specific implementation). This makes systems easier to test, extend,
// and reason about.
// ============================================================================

// INTERVIEW ANSWER: Abstract classes are useful when you have shared behavior
// across related types. Unlike interfaces, they can hold state and provide
// implemented methods. The abstract members define the "holes" that each
// concrete type must fill in.
public abstract class CacheProvider
{
    private int _hitCount;
    private int _missCount;

    public string ProviderName { get; }

    protected CacheProvider(string name) => ProviderName = name;

    // The abstract contract — derived classes MUST implement these
    protected abstract Task<string?> GetFromStoreAsync(string key);
    protected abstract Task SetInStoreAsync(string key, string value, TimeSpan expiry);
    protected abstract Task RemoveFromStoreAsync(string key);

    // INTERVIEW ANSWER: This is the Template Method pattern working with
    // abstraction — the base class defines the algorithm's skeleton (check
    // cache, log stats, handle misses) while derived classes fill in the
    // storage-specific details. Consumers only see GetAsync/SetAsync/RemoveAsync.
    public async Task<(string? Value, bool IsHit)> GetAsync(string key)
    {
        var value = await GetFromStoreAsync(key);
        if (value is not null)
        {
            _hitCount++;
            Console.WriteLine($"  [{ProviderName}] Cache HIT for '{key}'");
            return (value, true);
        }
        _missCount++;
        Console.WriteLine($"  [{ProviderName}] Cache MISS for '{key}'");
        return (null, false);
    }

    public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        var ttl = expiry ?? TimeSpan.FromMinutes(5);
        await SetInStoreAsync(key, value, ttl);
        Console.WriteLine($"  [{ProviderName}] SET '{key}' (TTL: {ttl.TotalSeconds}s)");
    }

    public async Task RemoveAsync(string key)
    {
        await RemoveFromStoreAsync(key);
        Console.WriteLine($"  [{ProviderName}] REMOVED '{key}'");
    }

    // Shared behavior — all cache providers get stats for free
    public CacheStats GetStats() => new(_hitCount, _missCount);
}

public record CacheStats(int Hits, int Misses)
{
    public double HitRate => Hits + Misses == 0 ? 0 : (double)Hits / (Hits + Misses) * 100;
    public override string ToString() => $"Hits: {Hits}, Misses: {Misses}, Hit Rate: {HitRate:F1}%";
}

// --- Concrete implementations ---

public class InMemoryCache : CacheProvider
{
    private readonly Dictionary<string, (string Value, DateTime Expiry)> _store = [];

    public InMemoryCache() : base("InMemory") { }

    protected override Task<string?> GetFromStoreAsync(string key)
    {
        if (_store.TryGetValue(key, out var entry) && entry.Expiry > DateTime.UtcNow)
            return Task.FromResult<string?>(entry.Value);

        _store.Remove(key); // Clean up expired
        return Task.FromResult<string?>(null);
    }

    protected override Task SetInStoreAsync(string key, string value, TimeSpan expiry)
    {
        _store[key] = (value, DateTime.UtcNow + expiry);
        return Task.CompletedTask;
    }

    protected override Task RemoveFromStoreAsync(string key)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }
}

public class FileSystemCache : CacheProvider
{
    // Simulated file system (in-memory for demo, but pretend it's disk)
    private readonly Dictionary<string, (string Value, DateTime Expiry)> _files = [];

    public FileSystemCache() : base("FileSystem") { }

    protected override async Task<string?> GetFromStoreAsync(string key)
    {
        await Task.Delay(5); // Simulate disk I/O latency
        if (_files.TryGetValue(key, out var entry) && entry.Expiry > DateTime.UtcNow)
            return entry.Value;
        return null;
    }

    protected override async Task SetInStoreAsync(string key, string value, TimeSpan expiry)
    {
        await Task.Delay(5);
        _files[key] = (value, DateTime.UtcNow + expiry);
    }

    protected override async Task RemoveFromStoreAsync(string key)
    {
        await Task.Delay(5);
        _files.Remove(key);
    }
}

// --- A service that depends on the abstraction, not the implementation ---

// INTERVIEW ANSWER: This is the real payoff of abstraction. UserProfileService
// doesn't know or care whether it's using Redis, memcached, files, or an
// in-memory dictionary. It depends on CacheProvider (the abstraction). We can
// swap implementations without changing this class at all.
public class UserProfileService(CacheProvider cache)
{
    public async Task<string> GetProfileAsync(string userId)
    {
        var (cached, isHit) = await cache.GetAsync($"profile:{userId}");
        if (isHit) return cached!;

        // Simulate database fetch
        var profile = $"{{\"id\":\"{userId}\",\"name\":\"User {userId}\",\"role\":\"admin\"}}";
        await cache.SetAsync($"profile:{userId}", profile, TimeSpan.FromMinutes(10));
        return profile;
    }

    public async Task InvalidateProfileAsync(string userId)
    {
        await cache.RemoveAsync($"profile:{userId}");
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== ABSTRACTION DEMO ===\n");

// Same service, different cache implementations
CacheProvider[] caches = [new InMemoryCache(), new FileSystemCache()];

foreach (var cache in caches)
{
    Console.WriteLine($"--- Using {cache.ProviderName} Cache ---");
    var service = new UserProfileService(cache);

    // First fetch — cache miss, loads from "database"
    var profile1 = await service.GetProfileAsync("user-42");
    Console.WriteLine($"  Profile: {profile1}");

    // Second fetch — cache hit
    var profile2 = await service.GetProfileAsync("user-42");
    Console.WriteLine($"  Profile: {profile2}");

    // Another user — cache miss
    await service.GetProfileAsync("user-99");

    // Invalidate and re-fetch
    await service.InvalidateProfileAsync("user-42");
    await service.GetProfileAsync("user-42");

    // Stats
    Console.WriteLine($"  Stats: {cache.GetStats()}");
    Console.WriteLine();
}

// INTERVIEW ANSWER: Notice how the demo code above treats InMemoryCache and
// FileSystemCache identically through the CacheProvider abstraction. The
// UserProfileService was written once and works with both — that's abstraction
// in action. If we added a RedisCache tomorrow, zero changes to UserProfileService.
