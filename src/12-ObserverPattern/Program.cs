// ============================================================================
// TOPIC: Observer Pattern
// ============================================================================
// INTERVIEW ANSWER:
// The Observer pattern establishes a one-to-many relationship between objects.
// When a subject (publisher) changes state, all its observers (subscribers) are
// notified automatically. This decouples the publisher from its subscribers —
// the publisher doesn't need to know who's listening or what they do with the
// notification. In C#, this pattern is built into the language via events and
// delegates, but it's valuable to understand the manual implementation too.
// ============================================================================

// --- Manual Observer Implementation ---

// INTERVIEW ANSWER: The manual implementation uses interfaces — IObserver and
// IObservable (not the Rx ones). This shows the raw mechanics: the subject
// maintains a list of observers and iterates through them when something happens.
// In practice, C# events do this for you, but understanding the underlying
// pattern helps you design better event-driven systems.
public interface IStockObserver
{
    void OnPriceChanged(string symbol, decimal oldPrice, decimal newPrice);
}

public class StockTicker
{
    private readonly Dictionary<string, decimal> _prices = [];
    private readonly List<IStockObserver> _observers = [];

    public void Subscribe(IStockObserver observer)
    {
        _observers.Add(observer);
        Console.WriteLine($"  [Ticker] Observer subscribed: {observer.GetType().Name}");
    }

    public void Unsubscribe(IStockObserver observer)
    {
        _observers.Remove(observer);
        Console.WriteLine($"  [Ticker] Observer unsubscribed: {observer.GetType().Name}");
    }

    public void UpdatePrice(string symbol, decimal newPrice)
    {
        var oldPrice = _prices.GetValueOrDefault(symbol, 0m);
        _prices[symbol] = newPrice;

        // Notify all observers
        foreach (var observer in _observers)
        {
            observer.OnPriceChanged(symbol, oldPrice, newPrice);
        }
    }
}

public class PriceDisplay : IStockObserver
{
    public void OnPriceChanged(string symbol, decimal oldPrice, decimal newPrice)
    {
        var change = newPrice - oldPrice;
        var arrow = change >= 0 ? "▲" : "▼";
        Console.WriteLine($"  [Display] {symbol}: {newPrice:C} ({arrow} {Math.Abs(change):C})");
    }
}

public class PriceAlert : IStockObserver
{
    private readonly decimal _threshold;

    public PriceAlert(decimal thresholdPercent) => _threshold = thresholdPercent;

    public void OnPriceChanged(string symbol, decimal oldPrice, decimal newPrice)
    {
        if (oldPrice == 0) return;
        var changePercent = Math.Abs((newPrice - oldPrice) / oldPrice * 100m);
        if (changePercent >= _threshold)
        {
            Console.WriteLine($"  [ALERT] {symbol} moved {changePercent:F1}% " +
                            $"(threshold: {_threshold}%)!");
        }
    }
}

public class TradeLogger : IStockObserver
{
    private readonly List<string> _log = [];

    public void OnPriceChanged(string symbol, decimal oldPrice, decimal newPrice)
    {
        var entry = $"{DateTime.UtcNow:HH:mm:ss} {symbol} {oldPrice:F2} → {newPrice:F2}";
        _log.Add(entry);
    }

    public void PrintLog()
    {
        Console.WriteLine("  [TradeLog] Entries:");
        foreach (var entry in _log)
            Console.WriteLine($"    {entry}");
    }
}

// --- C# Events Implementation (idiomatic approach) ---

// INTERVIEW ANSWER: C# events are the language's built-in observer pattern.
// They use delegates under the hood but add safety: only the declaring class
// can raise the event, subscribers can only += and -=. The EventHandler<T>
// convention and EventArgs pattern is the standard way to do pub/sub in C#.
public class InventoryEventArgs(string productId, int oldQuantity, int newQuantity) : EventArgs
{
    public string ProductId { get; } = productId;
    public int OldQuantity { get; } = oldQuantity;
    public int NewQuantity { get; } = newQuantity;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class InventoryManager
{
    private readonly Dictionary<string, int> _stock = [];

    public event EventHandler<InventoryEventArgs>? StockChanged;
    public event EventHandler<InventoryEventArgs>? LowStockDetected;

    private const int LowStockThreshold = 5;

    public void UpdateStock(string productId, int newQuantity)
    {
        var oldQuantity = _stock.GetValueOrDefault(productId, 0);
        _stock[productId] = newQuantity;

        // Raise StockChanged
        StockChanged?.Invoke(this, new InventoryEventArgs(productId, oldQuantity, newQuantity));

        // Raise LowStockDetected if applicable
        if (newQuantity <= LowStockThreshold && oldQuantity > LowStockThreshold)
        {
            LowStockDetected?.Invoke(this, new InventoryEventArgs(productId, oldQuantity, newQuantity));
        }
    }

    public int GetStock(string productId) => _stock.GetValueOrDefault(productId, 0);
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== OBSERVER PATTERN DEMO ===\n");

// --- Manual Implementation ---
Console.WriteLine("--- Manual Observer: Stock Ticker ---\n");

var ticker = new StockTicker();
var display = new PriceDisplay();
var alert = new PriceAlert(thresholdPercent: 5m);
var logger = new TradeLogger();

ticker.Subscribe(display);
ticker.Subscribe(alert);
ticker.Subscribe(logger);
Console.WriteLine();

ticker.UpdatePrice("MSFT", 380.50m);
ticker.UpdatePrice("AAPL", 175.25m);
Console.WriteLine();

ticker.UpdatePrice("MSFT", 395.00m); // ~3.8% change — no alert
ticker.UpdatePrice("AAPL", 160.00m); // ~8.7% change — triggers alert!
Console.WriteLine();

// Unsubscribe the display
ticker.Unsubscribe(display);
Console.WriteLine();

ticker.UpdatePrice("MSFT", 400.00m);
Console.WriteLine();

logger.PrintLog();
Console.WriteLine();

// --- C# Events Implementation ---
Console.WriteLine("--- C# Events: Inventory Manager ---\n");

var inventory = new InventoryManager();

// Subscribe using events (lambda subscribers)
inventory.StockChanged += (sender, e) =>
    Console.WriteLine($"  [Stock] {e.ProductId}: {e.OldQuantity} → {e.NewQuantity}");

inventory.LowStockDetected += (sender, e) =>
    Console.WriteLine($"  [LOW STOCK WARNING] {e.ProductId} is at {e.NewQuantity} units!");

// Named method subscriber
void OnStockChanged(object? sender, InventoryEventArgs e)
{
    if (e.NewQuantity == 0)
        Console.WriteLine($"  [OUT OF STOCK] {e.ProductId} — trigger reorder!");
}
inventory.StockChanged += OnStockChanged;

inventory.UpdateStock("WIDGET-A", 50);
inventory.UpdateStock("WIDGET-B", 100);
Console.WriteLine();

inventory.UpdateStock("WIDGET-A", 3);  // Triggers low stock
inventory.UpdateStock("WIDGET-B", 25);
Console.WriteLine();

inventory.UpdateStock("WIDGET-A", 0);  // Out of stock!
Console.WriteLine();

// INTERVIEW ANSWER: The key difference between the manual observer and C# events:
// With manual, you have full control (can enumerate observers, control order, etc.)
// but more boilerplate. With events, you get language-level safety (only the declaring
// class can invoke) and cleaner syntax, but less control over the invocation process.
// For most C# code, events are the right choice. Use the manual pattern when you
// need custom behavior like filtering, priority ordering, or async notification.

// Unsubscribe named method
inventory.StockChanged -= OnStockChanged;
Console.WriteLine("  (OnStockChanged handler removed)");
inventory.UpdateStock("WIDGET-A", 0); // No "OUT OF STOCK" message this time
