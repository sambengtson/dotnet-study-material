// ============================================================================
// TOPIC: Template Method Pattern
// ============================================================================
// INTERVIEW ANSWER:
// The Template Method pattern defines the skeleton of an algorithm in a base
// class, but lets subclasses override specific steps without changing the
// algorithm's structure. The base class method calls a series of steps, some
// of which are abstract (subclasses must implement) and some have default
// implementations (subclasses can optionally override). This enforces a
// consistent algorithm structure while allowing variation in the details.
// It's the "don't call us, we'll call you" principle (Hollywood Principle).
// ============================================================================

// --- Abstract class with template method ---

// INTERVIEW ANSWER: The template method is a non-virtual method in the base
// class that defines the algorithm structure. It calls abstract/virtual methods
// that subclasses override. Making the template method non-virtual prevents
// subclasses from changing the overall flow — they can only customize the steps.
public abstract class DataExporter
{
    // Template method — defines the algorithm skeleton
    public void Export(string[] data)
    {
        Console.WriteLine($"    Starting {FormatName} export...");

        var header = CreateHeader();
        var formattedRows = data.Select(FormatRow).ToList();
        var footer = CreateFooter(formattedRows.Count);

        var output = Combine(header, formattedRows, footer);

        // Hook: subclasses can optionally add post-processing
        output = PostProcess(output);

        WriteOutput(output);

        Console.WriteLine($"    {FormatName} export complete ({formattedRows.Count} rows)\n");
    }

    // Abstract steps — subclasses MUST implement these
    protected abstract string FormatName { get; }
    protected abstract string CreateHeader();
    protected abstract string FormatRow(string data);
    protected abstract string CreateFooter(int rowCount);
    protected abstract string Combine(string header, List<string> rows, string footer);

    // Hook method — subclasses CAN override (default does nothing)
    // INTERVIEW ANSWER: Hooks are optional override points in the template.
    // They have a default (usually empty) implementation so subclasses aren't
    // forced to override them. This gives subclasses fine-grained control
    // over the algorithm without requiring them to implement everything.
    protected virtual string PostProcess(string output) => output;

    // Concrete step — same for all subclasses
    protected virtual void WriteOutput(string output)
    {
        Console.WriteLine($"    Output ({output.Length} chars):");
        Console.WriteLine($"    {output[..Math.Min(output.Length, 200)]}");
        if (output.Length > 200)
            Console.WriteLine("    ...");
    }
}

// --- Concrete implementations ---

public class CsvExporter : DataExporter
{
    protected override string FormatName => "CSV";

    protected override string CreateHeader() => "Name,Value,Status";

    protected override string FormatRow(string data) => $"{data},100,Active";

    protected override string CreateFooter(int rowCount) => $"# Total rows: {rowCount}";

    protected override string Combine(string header, List<string> rows, string footer)
    {
        var lines = new List<string> { header };
        lines.AddRange(rows);
        lines.Add(footer);
        return string.Join("\n", lines);
    }
}

public class JsonExporter : DataExporter
{
    protected override string FormatName => "JSON";

    protected override string CreateHeader() => """{"data":[""";

    protected override string FormatRow(string data) =>
        $$"""{"name":"{{data}}","value":100,"status":"active"}""";

    protected override string CreateFooter(int rowCount) =>
        $"""],"meta":{{"count":{rowCount}}}}}""";

    protected override string Combine(string header, List<string> rows, string footer)
    {
        return header + string.Join(",", rows) + footer;
    }

    // Override hook to pretty-format (simplified)
    protected override string PostProcess(string output)
    {
        Console.WriteLine("    [Hook] Post-processing: formatting JSON");
        return output;
    }
}

public class HtmlExporter : DataExporter
{
    protected override string FormatName => "HTML";

    protected override string CreateHeader() =>
        "<table><thead><tr><th>Name</th><th>Value</th><th>Status</th></tr></thead><tbody>";

    protected override string FormatRow(string data) =>
        $"<tr><td>{data}</td><td>100</td><td>Active</td></tr>";

    protected override string CreateFooter(int rowCount) =>
        $"</tbody><tfoot><tr><td colspan=\"3\">Total: {rowCount}</td></tr></tfoot></table>";

    protected override string Combine(string header, List<string> rows, string footer)
    {
        return header + string.Join("", rows) + footer;
    }
}

// --- Second example: Report generator ---

public abstract class ReportGenerator
{
    public void GenerateReport(string title, Dictionary<string, decimal> data)
    {
        Console.WriteLine($"    Generating {ReportType} report: \"{title}\"\n");

        PrintTitle(title);
        PrintSummary(data);
        PrintDetails(data);

        if (ShouldIncludeChart())
            PrintChart(data);

        PrintConclusion(data);
        Console.WriteLine();
    }

    protected abstract string ReportType { get; }
    protected abstract void PrintTitle(string title);
    protected abstract void PrintDetails(Dictionary<string, decimal> data);

    // Default implementations that can be overridden
    protected virtual void PrintSummary(Dictionary<string, decimal> data)
    {
        Console.WriteLine($"    Summary: {data.Count} items, Total: {data.Values.Sum():C0}");
    }

    protected virtual void PrintConclusion(Dictionary<string, decimal> data)
    {
        Console.WriteLine($"    --- End of {ReportType} Report ---");
    }

    // Hook
    protected virtual bool ShouldIncludeChart() => false;

    protected virtual void PrintChart(Dictionary<string, decimal> data)
    {
        var max = data.Values.Max();
        foreach (var (key, value) in data)
        {
            var barLength = (int)(value / max * 20);
            Console.WriteLine($"    {key,-15} {new string('█', barLength)} {value:C0}");
        }
    }
}

public class ExecutiveReport : ReportGenerator
{
    protected override string ReportType => "Executive";

    protected override void PrintTitle(string title)
    {
        Console.WriteLine($"    ╔══════════════════════════════════╗");
        Console.WriteLine($"    ║  {title,-30}  ║");
        Console.WriteLine($"    ╚══════════════════════════════════╝");
    }

    protected override void PrintDetails(Dictionary<string, decimal> data)
    {
        // Executive report: high-level only
        var top3 = data.OrderByDescending(kv => kv.Value).Take(3);
        Console.WriteLine("    Top 3:");
        foreach (var (key, value) in top3)
            Console.WriteLine($"      • {key}: {value:C0}");
    }

    protected override bool ShouldIncludeChart() => true;
}

public class DetailedReport : ReportGenerator
{
    protected override string ReportType => "Detailed";

    protected override void PrintTitle(string title)
    {
        Console.WriteLine($"    === {title.ToUpperInvariant()} ===");
    }

    protected override void PrintDetails(Dictionary<string, decimal> data)
    {
        Console.WriteLine("    All items:");
        foreach (var (key, value) in data.OrderBy(kv => kv.Key))
            Console.WriteLine($"      {key,-20} {value,12:C2}");

        Console.WriteLine($"    {"Average:",-20} {data.Values.Average(),12:C2}");
        Console.WriteLine($"    {"Min:",-20} {data.Values.Min(),12:C2}");
        Console.WriteLine($"    {"Max:",-20} {data.Values.Max(),12:C2}");
    }

    protected override void PrintConclusion(Dictionary<string, decimal> data)
    {
        Console.WriteLine($"    Grand Total: {data.Values.Sum():C2}");
        base.PrintConclusion(data);
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== TEMPLATE METHOD PATTERN DEMO ===\n");

var sampleData = new[] { "Widget", "Gadget", "Sprocket" };

// --- Different exporters, same algorithm ---
Console.WriteLine("--- CSV Export ---");
new CsvExporter().Export(sampleData);

Console.WriteLine("--- JSON Export ---");
new JsonExporter().Export(sampleData);

Console.WriteLine("--- HTML Export ---");
new HtmlExporter().Export(sampleData);

// --- Report generators ---
var salesData = new Dictionary<string, decimal>
{
    ["North Region"] = 450_000m,
    ["South Region"] = 320_000m,
    ["East Region"] = 580_000m,
    ["West Region"] = 410_000m,
    ["Central"] = 290_000m
};

Console.WriteLine("--- Executive Report ---");
new ExecutiveReport().GenerateReport("Q4 Sales", salesData);

Console.WriteLine("--- Detailed Report ---");
new DetailedReport().GenerateReport("Q4 Sales", salesData);
