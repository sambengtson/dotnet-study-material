// ============================================================================
// TOPIC: Composite Pattern
// ============================================================================
// INTERVIEW ANSWER:
// The Composite pattern lets you compose objects into tree structures, then work
// with those structures as if they were individual objects. Both leaf nodes and
// composite nodes implement the same interface, so client code treats them
// uniformly. The classic example is a file system: files and folders both have
// a GetSize() method, but a folder's size is the sum of its children. You use
// this pattern whenever you have a part-whole hierarchy and need to treat
// parts and wholes the same way.
// ============================================================================

// --- Component interface ---

// INTERVIEW ANSWER: The component interface declares operations that make sense
// for both simple and complex elements of the tree. The key is that clients
// program to this interface and don't need to know if they're dealing with a
// leaf or a composite.
public interface IFileSystemEntry
{
    string Name { get; }
    long GetSize();
    void Display(string indent = "");
    int CountFiles();
}

// --- Leaf ---

public class File : IFileSystemEntry
{
    public string Name { get; }
    public long SizeInBytes { get; }

    public File(string name, long sizeInBytes)
    {
        Name = name;
        SizeInBytes = sizeInBytes;
    }

    public long GetSize() => SizeInBytes;

    public void Display(string indent = "")
    {
        Console.WriteLine($"{indent}📄 {Name} ({FormatSize(SizeInBytes)})");
    }

    public int CountFiles() => 1;

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_000_000 => $"{bytes / 1_000_000.0:F1} MB",
        >= 1_000 => $"{bytes / 1_000.0:F1} KB",
        _ => $"{bytes} B"
    };
}

// --- Composite ---

// INTERVIEW ANSWER: The composite stores child components and implements the
// component interface by delegating to its children. It can hold both leaves
// and other composites, forming a tree. Operations like GetSize() recursively
// aggregate results from all children.
public class Directory : IFileSystemEntry
{
    public string Name { get; }
    private readonly List<IFileSystemEntry> _children = [];

    public Directory(string name)
    {
        Name = name;
    }

    public Directory Add(IFileSystemEntry entry)
    {
        _children.Add(entry);
        return this;
    }

    public long GetSize() => _children.Sum(c => c.GetSize());

    public void Display(string indent = "")
    {
        Console.WriteLine($"{indent}📁 {Name}/");
        foreach (var child in _children)
            child.Display(indent + "  ");
    }

    public int CountFiles() => _children.Sum(c => c.CountFiles());
}

// --- Second example: Organization hierarchy ---

public interface IOrgUnit
{
    string Name { get; }
    decimal GetTotalBudget();
    int GetHeadcount();
    void Print(string indent = "");
}

public class Employee : IOrgUnit
{
    public string Name { get; }
    public string Role { get; }
    public decimal Salary { get; }

    public Employee(string name, string role, decimal salary)
    {
        Name = name;
        Role = role;
        Salary = salary;
    }

    public decimal GetTotalBudget() => Salary;
    public int GetHeadcount() => 1;
    public void Print(string indent = "") =>
        Console.WriteLine($"{indent}👤 {Name} ({Role}) - {Salary:C0}");
}

public class Department : IOrgUnit
{
    public string Name { get; }
    private readonly List<IOrgUnit> _units = [];

    public Department(string name) => Name = name;

    public Department Add(IOrgUnit unit) { _units.Add(unit); return this; }

    public decimal GetTotalBudget() => _units.Sum(u => u.GetTotalBudget());
    public int GetHeadcount() => _units.Sum(u => u.GetHeadcount());

    public void Print(string indent = "")
    {
        Console.WriteLine($"{indent}🏢 {Name} (Headcount: {GetHeadcount()}, Budget: {GetTotalBudget():C0})");
        foreach (var unit in _units)
            unit.Print(indent + "  ");
    }
}

// --- Helper: operations that work uniformly on any component ---

public static class FileSystemOperations
{
    public static IEnumerable<string> Search(IFileSystemEntry entry, string pattern)
    {
        if (entry.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            yield return entry.Name;

        if (entry is Directory dir)
        {
            // Access children through the public interface
            // In a real implementation, Directory might expose an enumerator
        }
    }

    public static void PrintSummary(IFileSystemEntry entry)
    {
        Console.WriteLine($"  Entry: {entry.Name}");
        Console.WriteLine($"  Total Size: {entry.GetSize():N0} bytes");
        Console.WriteLine($"  Total Files: {entry.CountFiles()}");
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== COMPOSITE PATTERN DEMO ===\n");

// --- File system tree ---
Console.WriteLine("--- File System Tree ---");

var root = new Directory("project")
    .Add(new File("README.md", 2_400))
    .Add(new File(".gitignore", 350))
    .Add(new Directory("src")
        .Add(new File("Program.cs", 8_500))
        .Add(new File("Startup.cs", 3_200))
        .Add(new Directory("Models")
            .Add(new File("User.cs", 1_200))
            .Add(new File("Order.cs", 2_100))
            .Add(new File("Product.cs", 1_800)))
        .Add(new Directory("Services")
            .Add(new File("UserService.cs", 4_500))
            .Add(new File("OrderService.cs", 6_200))))
    .Add(new Directory("tests")
        .Add(new File("UserTests.cs", 5_400))
        .Add(new File("OrderTests.cs", 4_800)));

root.Display();

// --- Uniform operations on any node ---
Console.WriteLine("\n--- Uniform Operations ---");
Console.WriteLine("\nEntire project:");
FileSystemOperations.PrintSummary(root);

// Same method works on a subdirectory
var srcDir = new Directory("src")
    .Add(new File("App.cs", 3_000))
    .Add(new File("Config.cs", 1_500));

Console.WriteLine("\nJust src/:");
FileSystemOperations.PrintSummary(srcDir);

// Same method works on a single file
Console.WriteLine("\nSingle file:");
FileSystemOperations.PrintSummary(new File("data.json", 15_000));

// --- Organization hierarchy ---
Console.WriteLine("\n--- Organization Hierarchy ---");

var company = new Department("Acme Corp")
    .Add(new Department("Engineering")
        .Add(new Department("Backend")
            .Add(new Employee("Alice", "Staff Engineer", 180_000))
            .Add(new Employee("Bob", "Senior Engineer", 155_000))
            .Add(new Employee("Carol", "Engineer", 120_000)))
        .Add(new Department("Frontend")
            .Add(new Employee("Dave", "Lead Engineer", 165_000))
            .Add(new Employee("Eve", "Engineer", 125_000))))
    .Add(new Department("Product")
        .Add(new Employee("Frank", "VP Product", 200_000))
        .Add(new Employee("Grace", "Product Manager", 140_000)))
    .Add(new Department("Sales")
        .Add(new Employee("Heidi", "Sales Director", 160_000))
        .Add(new Employee("Ivan", "Account Exec", 110_000))
        .Add(new Employee("Judy", "Account Exec", 105_000)));

company.Print();

Console.WriteLine($"\n  Company headcount: {company.GetHeadcount()}");
Console.WriteLine($"  Total budget: {company.GetTotalBudget():C0}");
