// ============================================================================
// TOPIC: Prototype Pattern
// ============================================================================
// INTERVIEW ANSWER:
// The Prototype pattern lets you copy existing objects without making your code
// dependent on their concrete classes. You define a Clone() method on the object
// that creates a duplicate. This is useful when object creation is expensive
// (e.g., involves database calls, network requests, or complex initialization)
// and you need many similar objects. C# has built-in support via ICloneable,
// but it's better to define your own typed interface because ICloneable returns
// object and doesn't distinguish between shallow and deep copies.
// ============================================================================

// --- Prototype interface ---

public interface IPrototype<T> where T : IPrototype<T>
{
    T DeepClone();
}

// --- Concrete prototypes ---

// INTERVIEW ANSWER: Shallow copy duplicates value types and references (both
// copies point to the same objects). Deep copy duplicates everything recursively
// so the clone is fully independent. For the Prototype pattern, you almost
// always want deep copy to avoid shared mutable state bugs.
public class DocumentTemplate : IPrototype<DocumentTemplate>
{
    public string Title { get; set; }
    public string Author { get; set; }
    public List<Section> Sections { get; set; }
    public DocumentMetadata Metadata { get; set; }

    public DocumentTemplate(string title, string author)
    {
        Title = title;
        Author = author;
        Sections = [];
        Metadata = new DocumentMetadata();
    }

    // Private constructor for cloning
    private DocumentTemplate(DocumentTemplate source)
    {
        Title = source.Title;
        Author = source.Author;
        // Deep clone the list and its contents
        Sections = source.Sections.Select(s => s.DeepClone()).ToList();
        Metadata = source.Metadata.DeepClone();
    }

    public DocumentTemplate DeepClone() => new(this);

    public void AddSection(string heading, string content)
    {
        Sections.Add(new Section(heading, content));
    }

    public void Describe()
    {
        Console.WriteLine($"  \"{Title}\" by {Author}");
        Console.WriteLine($"  Created: {Metadata.CreatedAt:yyyy-MM-dd}, Version: {Metadata.Version}");
        Console.WriteLine($"  Tags: [{string.Join(", ", Metadata.Tags)}]");
        foreach (var section in Sections)
            Console.WriteLine($"    - {section.Heading}: {section.Content[..Math.Min(section.Content.Length, 50)]}");
    }
}

public class Section : IPrototype<Section>
{
    public string Heading { get; set; }
    public string Content { get; set; }

    public Section(string heading, string content)
    {
        Heading = heading;
        Content = content;
    }

    public Section DeepClone() => new(Heading, Content);
}

public class DocumentMetadata : IPrototype<DocumentMetadata>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0";
    public List<string> Tags { get; set; } = [];

    public DocumentMetadata DeepClone()
    {
        return new DocumentMetadata
        {
            CreatedAt = CreatedAt,
            Version = Version,
            Tags = [.. Tags]  // Deep copy the list
        };
    }
}

// --- Prototype registry ---

// INTERVIEW ANSWER: A prototype registry (or cache) stores pre-built prototypes
// that clients can clone. This is useful when you have a fixed set of template
// objects and want to avoid re-creating them from scratch every time.
public class TemplateRegistry
{
    private readonly Dictionary<string, DocumentTemplate> _templates = [];

    public void Register(string key, DocumentTemplate template)
    {
        _templates[key] = template;
    }

    public DocumentTemplate Create(string key)
    {
        if (!_templates.TryGetValue(key, out var template))
            throw new KeyNotFoundException($"Template '{key}' not found in registry");

        return template.DeepClone();
    }

    public IReadOnlyCollection<string> AvailableTemplates => _templates.Keys.ToList();
}

// --- Showing shallow vs deep copy pitfalls ---

public class ShallowCopyExample
{
    public string Name { get; set; }
    public List<string> Items { get; set; }

    public ShallowCopyExample(string name) { Name = name; Items = []; }

    // MemberwiseClone is shallow — reference types are shared
    public ShallowCopyExample ShallowClone()
    {
        return (ShallowCopyExample)MemberwiseClone();
    }

    public ShallowCopyExample DeepClone()
    {
        var clone = (ShallowCopyExample)MemberwiseClone();
        clone.Items = [.. Items];  // Create independent copy
        return clone;
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== PROTOTYPE PATTERN DEMO ===\n");

// --- Shallow vs Deep Copy ---
Console.WriteLine("--- Shallow vs Deep Copy Pitfall ---");
var original = new ShallowCopyExample("Original");
original.Items.AddRange(["Item A", "Item B"]);

var shallowClone = original.ShallowClone();
shallowClone.Name = "Shallow Clone";
shallowClone.Items.Add("Item C");  // Modifies BOTH because list is shared

Console.WriteLine($"  Original items: [{string.Join(", ", original.Items)}]");
Console.WriteLine($"  Shallow clone items: [{string.Join(", ", shallowClone.Items)}]");
Console.WriteLine($"  Same list? {ReferenceEquals(original.Items, shallowClone.Items)}");

var deepClone = original.DeepClone();
deepClone.Name = "Deep Clone";
deepClone.Items.Add("Item D");

Console.WriteLine($"\n  Original items after deep clone: [{string.Join(", ", original.Items)}]");
Console.WriteLine($"  Deep clone items: [{string.Join(", ", deepClone.Items)}]");
Console.WriteLine($"  Same list? {ReferenceEquals(original.Items, deepClone.Items)}");

// --- Template cloning ---
Console.WriteLine("\n--- Document Template Cloning ---");
var invoiceTemplate = new DocumentTemplate("Invoice", "Finance Dept");
invoiceTemplate.AddSection("Header", "Company Name, Address, Invoice Number");
invoiceTemplate.AddSection("Line Items", "Description | Qty | Price | Total");
invoiceTemplate.AddSection("Footer", "Payment terms, bank details, due date");
invoiceTemplate.Metadata.Tags.AddRange(["finance", "invoice", "template"]);

Console.WriteLine("Original template:");
invoiceTemplate.Describe();

// Clone and customize
var januaryInvoice = invoiceTemplate.DeepClone();
januaryInvoice.Title = "Invoice - January 2025";
januaryInvoice.Sections[0].Content = "Acme Corp, 123 Main St, INV-2025-001";
januaryInvoice.Metadata.Tags.Add("january");
januaryInvoice.Metadata.Version = "1.1";

Console.WriteLine("\nCloned & customized:");
januaryInvoice.Describe();

Console.WriteLine("\nOriginal unchanged:");
invoiceTemplate.Describe();

// --- Prototype Registry ---
Console.WriteLine("\n--- Prototype Registry ---");
var registry = new TemplateRegistry();

var reportTemplate = new DocumentTemplate("Quarterly Report", "Analytics Team");
reportTemplate.AddSection("Executive Summary", "Key findings and metrics overview");
reportTemplate.AddSection("Performance Data", "Charts, graphs, and detailed numbers");
reportTemplate.AddSection("Recommendations", "Action items based on the data");
reportTemplate.Metadata.Tags.AddRange(["report", "quarterly"]);

var memoTemplate = new DocumentTemplate("Internal Memo", "HR Department");
memoTemplate.AddSection("Subject", "Topic of the memo");
memoTemplate.AddSection("Body", "Detailed message content");
memoTemplate.Metadata.Tags.AddRange(["memo", "internal"]);

registry.Register("invoice", invoiceTemplate);
registry.Register("report", reportTemplate);
registry.Register("memo", memoTemplate);

Console.WriteLine($"  Available templates: [{string.Join(", ", registry.AvailableTemplates)}]\n");

// Create documents from templates
var myReport = registry.Create("report");
myReport.Title = "Q4 2024 Performance Report";
myReport.Author = "Data Analytics";
myReport.Metadata.Version = "2.0";
Console.WriteLine("Created from 'report' template:");
myReport.Describe();

Console.WriteLine("\nOriginal report template unchanged:");
reportTemplate.Describe();
