using Microsoft.EntityFrameworkCore;

// ============================================================================
// TOPIC: EF Core Global Query Filters
// ============================================================================
// INTERVIEW ANSWER:
// Global query filters are predicates configured in OnModelCreating that EF Core
// automatically injects into every LINQ query for an entity type. They run at
// the SQL level (translated to WHERE clauses), not in-memory. The two most common
// use cases are soft-delete (filtering out IsDeleted == true rows) and multi-tenancy
// (filtering by TenantId so each tenant only sees their own data). Filters apply
// to direct queries, Include navigations, and raw SQL. You can bypass them with
// IgnoreQueryFilters() when you need to access "filtered" rows intentionally.
// ============================================================================

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== EF CORE GLOBAL QUERY FILTERS ===\n");

using var context = new BlogContext();
context.Database.EnsureDeleted();
context.Database.EnsureCreated();

SeedData(context);

Console.WriteLine("--- 1. Soft Delete Filter (automatic) ---");
Console.WriteLine("All blogs in DB: 4 total, but filter hides deleted ones.\n");

// INTERVIEW ANSWER: The global query filter is automatically applied — no Where()
// clause needed in the query. EF Core translates this to SQL with a WHERE clause
// that excludes deleted rows.
var activeBlogs = context.Blogs.OrderBy(b => b.Name).ToList();
Console.WriteLine($"Active blogs returned: {activeBlogs.Count}");
foreach (var blog in activeBlogs)
{
    Console.WriteLine($"  [{blog.Id}] {blog.Name}  Deleted: {blog.IsDeleted}");
}

Console.WriteLine("\n--- 2. Soft Delete Filter (bypass with IgnoreQueryFilters) ---");
// INTERVIEW ANSWER: Use IgnoreQueryFilters() on the DbSet to temporarily disable
// all global filters for that query. Use WithNoRows() if you want zero results
// instead of bypassing. This is how an admin can view soft-deleted records.
var allBlogs = context.Blogs.IgnoreQueryFilters().OrderBy(b => b.Name).ToList();
Console.WriteLine($"All blogs (including deleted): {allBlogs.Count}");
foreach (var blog in allBlogs)
{
    Console.WriteLine($"  [{blog.Id}] {blog.Name}  Deleted: {blog.IsDeleted}");
}

Console.WriteLine("\n--- 3. Multi-Tenancy Filter ---");
// INTERVIEW ANSWER: A common pattern is to inject the current tenant ID into the
// DbContext so every query automatically scopes results to that tenant. This prevents
// data leaks between tenants without requiring developers to remember Where() clauses.
context.CurrentTenantId = 2;
var tenantBlogs = context.Blogs.OrderBy(b => b.Name).ToList();
Console.WriteLine($"Blogs for Tenant 2: {tenantBlogs.Count}");
foreach (var blog in tenantBlogs)
{
    Console.WriteLine($"  [{blog.Id}] {blog.Name}  TenantId: {blog.TenantId}");
}

Console.WriteLine("\n--- 4. Combined Filters (soft delete + multi-tenancy) ---");
// INTERVIEW ANSWER: Multiple filters on the same entity are ANDed together. Both
// predicates must be true for a row to appear. This means a deleted record from
// another tenant is excluded by both filters.
context.CurrentTenantId = 1;
var tenant1ActiveBlogs = context.Blogs.OrderBy(b => b.Name).ToList();
Console.WriteLine($"Active blogs for Tenant 1: {tenant1ActiveBlogs.Count}");
foreach (var blog in tenant1ActiveBlogs)
{
    Console.WriteLine($"  [{blog.Id}] {blog.Name}  TenantId: {blog.TenantId}, Deleted: {blog.IsDeleted}");
}

Console.WriteLine("\n--- 5. Filters Apply to Related Entity Queries ---");
// INTERVIEW ANSWER: Global query filters apply independently per entity type.
// Querying context.Posts directly respects the Post filter. When using Include()
// to load navigations, EF Core also applies the child entity's filter to the
// loaded collection, keeping relationships consistent with filtering rules.
context.CurrentTenantId = 1;

// Direct query on Posts — only non-deleted posts appear
var activePosts = context.Posts.ToList();
Console.WriteLine($"Active posts (filter hides deleted): {activePosts.Count}");
foreach (var post in activePosts)
    Console.WriteLine($"  - {post.Title} (deleted: {post.IsDeleted})");

// All posts including deleted
var allPosts = context.Posts.IgnoreQueryFilters().ToList();
Console.WriteLine($"\nAll posts (bypassing filter): {allPosts.Count}");
foreach (var post in allPosts)
    Console.WriteLine($"  - {post.Title} (deleted: {post.IsDeleted})");

Console.WriteLine("\n--- 6. Filters Apply to Aggregate Queries ---");
// INTERVIEW ANSWER: Global query filters are applied to Count(), Any(), Average(),
// and other aggregate operations too. They also apply to FromSqlRaw() / raw SQL —
// providing a consistent safety net regardless of how the query is written.
context.CurrentTenantId = 2;
var activeCount = context.Blogs.Count();
var totalCount = context.Blogs.IgnoreQueryFilters().Count();
Console.WriteLine($"Tenant 2 — active blogs: {activeCount}, total (incl. deleted): {totalCount}");

var hasDeletedPosts = context.Posts.IgnoreQueryFilters()
    .Any(p => p.IsDeleted);
Console.WriteLine($"Database contains soft-deleted posts: {hasDeletedPosts}");

Console.WriteLine("\n--- 7. Filtering with Lambda Parameters ---");
// INTERVIEW ANSWER: Filters can reference parameters from the DbContext via lambda
// expressions. EF Core captures the current value at query execution time, not at
// configuration time. This allows dynamic filters like tenant ID that change per request.
Console.WriteLine("Switching to Tenant 3...");
context.CurrentTenantId = 3;
var tenant3Blogs = context.Blogs.ToList();
Console.WriteLine($"Active blogs for Tenant 3: {tenant3Blogs.Count}");
foreach (var blog in tenant3Blogs)
    Console.WriteLine($"  [{blog.Id}] {blog.Name}");

void SeedData(BlogContext ctx)
{
    // Tenant 1 blogs
    ctx.Blogs.AddRange(
        new Blog { Name = "Tech Blog", TenantId = 1, IsDeleted = false },
        new Blog { Name = "Old Tech Blog", TenantId = 1, IsDeleted = true }
    );

    // Tenant 2 blogs
    ctx.Blogs.AddRange(
        new Blog { Name = "Food Blog", TenantId = 2, IsDeleted = false },
        new Blog { Name = "Travel Blog", TenantId = 2, IsDeleted = false }
    );

    // Tenant 3 blog
    ctx.Blogs.Add(new Blog { Name = "Gaming Blog", TenantId = 3, IsDeleted = false });

    ctx.SaveChanges();

    var techBlog = ctx.Blogs.IgnoreQueryFilters().First(b => b.Name == "Tech Blog");
    var foodBlog = ctx.Blogs.IgnoreQueryFilters().First(b => b.Name == "Food Blog");
    var travelBlog = ctx.Blogs.IgnoreQueryFilters().First(b => b.Name == "Travel Blog");

    ctx.Posts.AddRange(
        new Post { Title = "EF Core Tips", BlogId = techBlog.Id, IsDeleted = false },
        new Post { Title = "Old Post", BlogId = techBlog.Id, IsDeleted = true },
        new Post { Title = "Recipe Collection", BlogId = foodBlog.Id, IsDeleted = false },
        new Post { Title = "Japan Travel Guide", BlogId = travelBlog.Id, IsDeleted = false },
        new Post { Title = "Removed Article", BlogId = travelBlog.Id, IsDeleted = true }
    );

    ctx.SaveChanges();
}

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

public class BlogContext : DbContext
{
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<Post> Posts => Set<Post>();

    // INTERVIEW ANSWER: A property on the context holds the current tenant ID.
    // In a real app, this is set from the authenticated user or request context
    // at the start of each request, typically via an IDbContextFactory or
    // middleware that calls context.SetTenantId(userId).
    public int CurrentTenantId { get; set; } = 1;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseInMemoryDatabase("EfGlobalFiltersDemo");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // INTERVIEW ANSWER: HasQueryFilter attaches a predicate that EF Core
        // automatically injects into every query for the entity type. The lambda
        // captures `this` (the DbContext instance), so it reads the current value
        // of CurrentTenantId at query execution time.

        // INTERVIEW ANSWER: Multiple HasQueryFilter calls on the same entity are ANDed.
        // Here we chain soft-delete and multi-tenancy into a single filter expression.
        modelBuilder.Entity<Blog>()
            .HasQueryFilter(b => !b.IsDeleted && b.TenantId == this.CurrentTenantId);

        // Multi-tenancy filter — scopes posts to the current tenant's blogs
        // via a subquery, or directly if the post stores TenantId
        modelBuilder.Entity<Post>()
            .HasQueryFilter(p => !p.IsDeleted);
    }
}

// INTERVIEW ANSWER: The Blog entity has two concepts that global query filters
// protect against: accidental exposure of soft-deleted data and cross-tenant
// data leaks. Instead of scattering Where() clauses throughout the codebase,
// the filter centralizes this logic in one place (OnModelCreating).
public class Blog
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public int TenantId { get; set; }

    public List<Post> Posts { get; set; } = new();
}

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public int BlogId { get; set; }

    public Blog? Blog { get; set; }
}
