// ============================================================================
// TOPIC: Command Pattern
// ============================================================================
// INTERVIEW ANSWER:
// The Command pattern turns a request into a stand-alone object that contains all
// the information about the request. This lets you parameterize methods with
// different requests, queue or log requests, and support undo/redo operations.
// The key participants are: Command (interface), ConcreteCommand (encapsulates
// action + receiver), Invoker (triggers commands), and Receiver (performs the
// actual work). It decouples the object that invokes the operation from the one
// that knows how to perform it.
// ============================================================================

// --- Command interface ---

public interface ICommand
{
    string Description { get; }
    void Execute();
    void Undo();
}

// --- Receiver: Text editor document ---

// INTERVIEW ANSWER: The receiver is the object that actually performs the work.
// Commands delegate to the receiver rather than containing the logic themselves.
// This keeps commands thin and focused on capturing intent.
public class TextDocument
{
    private string _content = "";

    public string Content => _content;
    public int Length => _content.Length;

    public void InsertText(int position, string text)
    {
        _content = _content.Insert(position, text);
    }

    public void DeleteText(int position, int length)
    {
        _content = _content.Remove(position, length);
    }

    public void ReplaceText(int position, int length, string newText)
    {
        _content = _content.Remove(position, length).Insert(position, newText);
    }

    public override string ToString() => _content.Length > 60
        ? _content[..60] + "..."
        : _content;
}

// --- Concrete commands ---

public class InsertTextCommand : ICommand
{
    private readonly TextDocument _doc;
    private readonly int _position;
    private readonly string _text;

    public string Description => $"Insert \"{_text}\" at position {_position}";

    public InsertTextCommand(TextDocument doc, int position, string text)
    {
        _doc = doc;
        _position = position;
        _text = text;
    }

    public void Execute() => _doc.InsertText(_position, _text);
    public void Undo() => _doc.DeleteText(_position, _text.Length);
}

public class DeleteTextCommand : ICommand
{
    private readonly TextDocument _doc;
    private readonly int _position;
    private readonly int _length;
    private string _deletedText = "";

    public string Description => $"Delete {_length} chars at position {_position}";

    public DeleteTextCommand(TextDocument doc, int position, int length)
    {
        _doc = doc;
        _position = position;
        _length = length;
    }

    public void Execute()
    {
        _deletedText = _doc.Content.Substring(_position, _length);
        _doc.DeleteText(_position, _length);
    }

    public void Undo() => _doc.InsertText(_position, _deletedText);
}

public class ReplaceTextCommand : ICommand
{
    private readonly TextDocument _doc;
    private readonly int _position;
    private readonly string _oldText;
    private readonly string _newText;

    public string Description => $"Replace \"{_oldText}\" with \"{_newText}\"";

    public ReplaceTextCommand(TextDocument doc, string oldText, string newText)
    {
        _doc = doc;
        _position = doc.Content.IndexOf(oldText, StringComparison.Ordinal);
        _oldText = oldText;
        _newText = newText;

        if (_position < 0)
            throw new InvalidOperationException($"Text \"{oldText}\" not found");
    }

    public void Execute() => _doc.ReplaceText(_position, _oldText.Length, _newText);
    public void Undo() => _doc.ReplaceText(_position, _newText.Length, _oldText);
}

// --- Macro command (composite of commands) ---

// INTERVIEW ANSWER: A macro command groups multiple commands into one. When
// executed, it runs all its sub-commands. When undone, it reverses them.
// This is a natural combination of Command + Composite patterns.
public class MacroCommand : ICommand
{
    private readonly List<ICommand> _commands;
    public string Description { get; }

    public MacroCommand(string description, params ICommand[] commands)
    {
        Description = description;
        _commands = [.. commands];
    }

    public void Execute()
    {
        foreach (var cmd in _commands)
            cmd.Execute();
    }

    public void Undo()
    {
        // Undo in reverse order
        for (int i = _commands.Count - 1; i >= 0; i--)
            _commands[i].Undo();
    }
}

// --- Invoker: Command history with undo/redo ---

// INTERVIEW ANSWER: The invoker stores and executes commands. By maintaining
// a history stack, it enables undo/redo. The invoker doesn't know what the
// commands do — it just calls Execute() and Undo(). This is exactly how
// text editors, drawing apps, and IDEs implement undo/redo.
public class CommandHistory
{
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();

    public void Execute(ICommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear();  // New action invalidates redo history
        Console.WriteLine($"    Executed: {command.Description}");
    }

    public bool Undo()
    {
        if (_undoStack.Count == 0) return false;
        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
        Console.WriteLine($"    Undone: {command.Description}");
        return true;
    }

    public bool Redo()
    {
        if (_redoStack.Count == 0) return false;
        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
        Console.WriteLine($"    Redone: {command.Description}");
        return true;
    }

    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;
}

// --- Command queue (deferred execution) ---

// INTERVIEW ANSWER: Commands can also be queued for later execution, which is
// useful for job queues, task scheduling, and transaction management. Since
// commands are objects, they can be serialized, stored, and executed later.
public class CommandQueue
{
    private readonly Queue<ICommand> _queue = new();

    public void Enqueue(ICommand command)
    {
        _queue.Enqueue(command);
        Console.WriteLine($"    Queued: {command.Description}");
    }

    public void ProcessAll()
    {
        Console.WriteLine($"    Processing {_queue.Count} queued commands...");
        while (_queue.Count > 0)
        {
            var cmd = _queue.Dequeue();
            cmd.Execute();
            Console.WriteLine($"    Processed: {cmd.Description}");
        }
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== COMMAND PATTERN DEMO ===\n");

// --- Basic undo/redo ---
Console.WriteLine("--- Text Editor with Undo/Redo ---");
var doc = new TextDocument();
var history = new CommandHistory();

history.Execute(new InsertTextCommand(doc, 0, "Hello World"));
Console.WriteLine($"  Doc: \"{doc}\"\n");

history.Execute(new InsertTextCommand(doc, 5, ", Beautiful"));
Console.WriteLine($"  Doc: \"{doc}\"\n");

history.Execute(new ReplaceTextCommand(doc, "World", "C# Developers"));
Console.WriteLine($"  Doc: \"{doc}\"\n");

history.Execute(new DeleteTextCommand(doc, 0, 6));
Console.WriteLine($"  Doc: \"{doc}\"\n");

// Undo chain
Console.WriteLine("--- Undo x3 ---");
history.Undo();
Console.WriteLine($"  Doc: \"{doc}\"");
history.Undo();
Console.WriteLine($"  Doc: \"{doc}\"");
history.Undo();
Console.WriteLine($"  Doc: \"{doc}\"");

// Redo
Console.WriteLine($"\n--- Redo x2 ---");
history.Redo();
Console.WriteLine($"  Doc: \"{doc}\"");
history.Redo();
Console.WriteLine($"  Doc: \"{doc}\"");

Console.WriteLine($"\n  Undo stack: {history.UndoCount}, Redo stack: {history.RedoCount}");

// --- Macro command ---
Console.WriteLine("\n--- Macro Command ---");
var doc2 = new TextDocument();
var history2 = new CommandHistory();

var macro = new MacroCommand("Insert formatted header",
    new InsertTextCommand(doc2, 0, "=== "),
    new InsertTextCommand(doc2, 4, "DESIGN PATTERNS"),
    new InsertTextCommand(doc2, 19, " ==="));

history2.Execute(macro);
Console.WriteLine($"  Doc: \"{doc2}\"\n");

history2.Undo();
Console.WriteLine($"  After undo macro: \"{doc2}\"");

history2.Redo();
Console.WriteLine($"  After redo macro: \"{doc2}\"");

// --- Command queue ---
Console.WriteLine("\n--- Command Queue (Deferred Execution) ---");
var doc3 = new TextDocument();
var queue = new CommandQueue();

queue.Enqueue(new InsertTextCommand(doc3, 0, "First. "));
queue.Enqueue(new InsertTextCommand(doc3, 7, "Second. "));
queue.Enqueue(new InsertTextCommand(doc3, 15, "Third."));

Console.WriteLine($"\n  Doc before processing: \"{doc3}\"");
queue.ProcessAll();
Console.WriteLine($"  Doc after processing: \"{doc3}\"");
