// ============================================================================
// TOPIC: LINQ (Language Integrated Query)
// ============================================================================
// INTERVIEW ANSWER:
// LINQ provides a consistent query syntax for any data source — collections,
// databases, XML, whatever implements IEnumerable<T> or IQueryable<T>. It comes
// in two flavors: method syntax (extension methods like .Where(), .Select()) and
// query syntax (SQL-like from/where/select). The key thing to understand is
// deferred execution — most LINQ operators don't execute immediately. They build
// up an expression tree or iterator that runs when you actually enumerate the
// results (e.g., with foreach, ToList(), or First()).
// ============================================================================

// --- Sample data ---

public record Employee(
    string Id, string Name, string Department, decimal Salary,
    DateTime HireDate, string? ManagerId = null);

public record Project(string Id, string Name, string LeadId, string Department);

public record Assignment(string EmployeeId, string ProjectId, int HoursPerWeek);

// Build our dataset
var employees = new List<Employee>
{
    new("E1", "Alice Chen", "Engineering", 145_000m, new(2019, 3, 15)),
    new("E2", "Bob Martinez", "Engineering", 130_000m, new(2020, 7, 1), "E1"),
    new("E3", "Carol White", "Engineering", 125_000m, new(2021, 1, 10), "E1"),
    new("E4", "Diana Patel", "Product", 140_000m, new(2018, 11, 20)),
    new("E5", "Eve Johnson", "Product", 115_000m, new(2022, 4, 5), "E4"),
    new("E6", "Frank Lee", "Sales", 95_000m, new(2020, 9, 12)),
    new("E7", "Grace Kim", "Sales", 105_000m, new(2019, 6, 1)),
    new("E8", "Hank Brown", "Engineering", 155_000m, new(2017, 2, 28)),
    new("E9", "Ivy Wilson", "Product", 120_000m, new(2021, 8, 15), "E4"),
    new("E10", "Jack Davis", "Sales", 110_000m, new(2023, 1, 3), "E7"),
};

var projects = new List<Project>
{
    new("P1", "API Rewrite", "E1", "Engineering"),
    new("P2", "Mobile App", "E2", "Engineering"),
    new("P3", "User Research", "E4", "Product"),
    new("P4", "Sales Dashboard", "E7", "Sales"),
};

var assignments = new List<Assignment>
{
    new("E1", "P1", 30), new("E2", "P1", 20), new("E2", "P2", 20),
    new("E3", "P2", 40), new("E4", "P3", 25), new("E5", "P3", 35),
    new("E6", "P4", 40), new("E7", "P4", 15), new("E8", "P1", 10),
    new("E8", "P2", 30), new("E9", "P3", 20), new("E10", "P4", 40),
};

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== LINQ DEMO ===\n");

// --- Where / Select (Filtering and Projection) ---
Console.WriteLine("--- Where + Select (Method Syntax) ---");
var highEarners = employees
    .Where(e => e.Salary > 130_000m)
    .Select(e => new { e.Name, e.Salary, e.Department })
    .OrderByDescending(e => e.Salary);

foreach (var e in highEarners)
    Console.WriteLine($"  {e.Name} — {e.Salary:C} ({e.Department})");
Console.WriteLine();

// --- Query Syntax (same query) ---
Console.WriteLine("--- Query Syntax (equivalent) ---");
var highEarnersQuery =
    from e in employees
    where e.Salary > 130_000m
    orderby e.Salary descending
    select new { e.Name, e.Salary, e.Department };

foreach (var e in highEarnersQuery)
    Console.WriteLine($"  {e.Name} — {e.Salary:C} ({e.Department})");
Console.WriteLine();

// --- GroupBy ---
Console.WriteLine("--- GroupBy ---");

// INTERVIEW ANSWER: GroupBy creates groups of elements that share a common key.
// The result is IEnumerable<IGrouping<TKey, TElement>> — each group has a Key
// property and is itself enumerable.
var byDepartment = employees
    .GroupBy(e => e.Department)
    .Select(g => new
    {
        Department = g.Key,
        Count = g.Count(),
        AvgSalary = g.Average(e => e.Salary),
        TopEarner = g.MaxBy(e => e.Salary)!.Name
    });

foreach (var dept in byDepartment)
    Console.WriteLine($"  {dept.Department}: {dept.Count} people, " +
                      $"Avg: {dept.AvgSalary:C0}, Top: {dept.TopEarner}");
Console.WriteLine();

// --- Join ---
Console.WriteLine("--- Join (Employees ↔ Projects) ---");

// INTERVIEW ANSWER: Join in LINQ is like an inner join in SQL. You specify the
// outer sequence, inner sequence, key selectors for both, and a result selector.
// For left outer joins, you use GroupJoin + SelectMany with DefaultIfEmpty().
var projectLeads = employees
    .Join(projects,
        emp => emp.Id,
        proj => proj.LeadId,
        (emp, proj) => new { emp.Name, ProjectName = proj.Name, emp.Department });

foreach (var lead in projectLeads)
    Console.WriteLine($"  {lead.Name} leads '{lead.ProjectName}' ({lead.Department})");
Console.WriteLine();

// --- SelectMany (flattening) ---
Console.WriteLine("--- SelectMany (Employee → Assignments → Projects) ---");

// INTERVIEW ANSWER: SelectMany flattens a collection of collections into a single
// sequence. It's like a flat map. If each employee has multiple assignments, SelectMany
// gives you one flat list of (employee, assignment) pairs.
var workload = employees
    .SelectMany(
        emp => assignments.Where(a => a.EmployeeId == emp.Id),
        (emp, assignment) => new { emp.Name, assignment.ProjectId, assignment.HoursPerWeek })
    .OrderBy(x => x.Name)
    .ThenBy(x => x.ProjectId);

foreach (var w in workload)
    Console.WriteLine($"  {w.Name} → {w.ProjectId}: {w.HoursPerWeek}h/week");
Console.WriteLine();

// --- Aggregate ---
Console.WriteLine("--- Aggregate ---");
var totalPayroll = employees.Sum(e => e.Salary);
var avgTenure = employees.Average(e => (DateTime.Now - e.HireDate).TotalDays / 365.25);
var seniorMost = employees.MinBy(e => e.HireDate)!;

Console.WriteLine($"  Total payroll: {totalPayroll:C0}");
Console.WriteLine($"  Average tenure: {avgTenure:F1} years");
Console.WriteLine($"  Most senior: {seniorMost.Name} (since {seniorMost.HireDate:yyyy-MM-dd})");

// Custom Aggregate
var nameList = employees
    .Select(e => e.Name.Split(' ')[0])
    .Aggregate((current, next) => $"{current}, {next}");
Console.WriteLine($"  First names: {nameList}");
Console.WriteLine();

// --- Deferred vs Immediate Execution ---
Console.WriteLine("--- Deferred vs Immediate Execution ---");

// INTERVIEW ANSWER: Most LINQ operators use deferred execution — they create an
// iterator but don't run the query until you enumerate. This means the query
// reflects the data at enumeration time, not definition time. Methods like
// ToList(), ToArray(), Count(), First() force immediate execution.
var salaryQuery = employees.Where(e =>
{
    Console.WriteLine($"    Evaluating: {e.Name}");
    return e.Salary > 140_000m;
});

Console.WriteLine("  Query defined — nothing executed yet.");
Console.WriteLine("  Now calling ToList()...");
var results = salaryQuery.ToList();
Console.WriteLine($"  Found {results.Count} employees over $140k");
Console.WriteLine();

// --- Custom Extension Method ---
Console.WriteLine("--- Custom Extension Methods ---");

// INTERVIEW ANSWER: LINQ is built on extension methods. You can add your own
// to IEnumerable<T> to create domain-specific query operators that blend
// seamlessly with built-in LINQ methods.
var medianSalary = employees.Select(e => e.Salary).Median();
Console.WriteLine($"  Median salary: {medianSalary:C0}");

var recentHires = employees.WhereRecent(e => e.HireDate, TimeSpan.FromDays(365 * 3));
Console.WriteLine($"  Hired in last 3 years: {string.Join(", ", recentHires.Select(e => e.Name))}");

// Chaining custom + built-in
var report = employees
    .WhereRecent(e => e.HireDate, TimeSpan.FromDays(365 * 4))
    .Where(e => e.Department == "Engineering")
    .Select(e => $"{e.Name} ({e.HireDate:yyyy})")
    .ToList();
Console.WriteLine($"  Recent Engineering hires: {string.Join(", ", report)}");

// --- Custom extension methods ---

public static class LinqExtensions
{
    public static decimal Median(this IEnumerable<decimal> source)
    {
        var sorted = source.OrderBy(x => x).ToList();
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2m
            : sorted[mid];
    }

    public static IEnumerable<T> WhereRecent<T>(
        this IEnumerable<T> source,
        Func<T, DateTime> dateSelector,
        TimeSpan window)
    {
        var cutoff = DateTime.Now - window;
        return source.Where(item => dateSelector(item) >= cutoff);
    }
}
