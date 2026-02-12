// ============================================================================
// TOPIC: Strategy Pattern
// ============================================================================
// INTERVIEW ANSWER:
// The Strategy pattern defines a family of algorithms, encapsulates each one behind
// a common interface, and makes them interchangeable at runtime. The key benefit is
// that the context class (the one using the algorithm) doesn't know or care which
// specific algorithm it's using — it just calls the interface. This follows the
// Open/Closed principle: you can add new strategies without modifying existing code.
// ============================================================================

// --- Strategy interface ---

public interface IPricingStrategy
{
    string Name { get; }
    decimal CalculatePrice(decimal basePrice, int quantity, CustomerProfile customer);
}

public record CustomerProfile(string Id, string Name, string Tier, int TotalOrders, decimal LifetimeSpend);

// --- Concrete strategies ---

public class StandardPricing : IPricingStrategy
{
    public string Name => "Standard";

    public decimal CalculatePrice(decimal basePrice, int quantity, CustomerProfile customer)
    {
        return basePrice * quantity;
    }
}

// INTERVIEW ANSWER: Each strategy encapsulates a different algorithm. The calling
// code doesn't need if/else chains or switch statements — it just calls
// CalculatePrice on whatever strategy it has. Adding a new pricing tier is just
// adding a new class, not modifying existing code.
public class BulkDiscountPricing : IPricingStrategy
{
    public string Name => "Bulk Discount";

    public decimal CalculatePrice(decimal basePrice, int quantity, CustomerProfile customer)
    {
        var discount = quantity switch
        {
            >= 100 => 0.25m,
            >= 50 => 0.15m,
            >= 10 => 0.10m,
            _ => 0m
        };

        var total = basePrice * quantity;
        return total - (total * discount);
    }
}

public class LoyaltyPricing : IPricingStrategy
{
    public string Name => "Loyalty";

    public decimal CalculatePrice(decimal basePrice, int quantity, CustomerProfile customer)
    {
        var discount = customer.Tier switch
        {
            "Platinum" => 0.20m,
            "Gold" => 0.15m,
            "Silver" => 0.10m,
            _ => 0.05m
        };

        // Extra discount for high-value customers
        if (customer.LifetimeSpend > 10_000m)
            discount += 0.05m;

        var total = basePrice * quantity;
        return total - (total * discount);
    }
}

public class PromotionalPricing : IPricingStrategy
{
    public decimal PromotionDiscount { get; }
    public string PromotionCode { get; }

    public string Name => $"Promo ({PromotionCode})";

    public PromotionalPricing(string code, decimal discount)
    {
        PromotionCode = code;
        PromotionDiscount = discount;
    }

    public decimal CalculatePrice(decimal basePrice, int quantity, CustomerProfile customer)
    {
        var total = basePrice * quantity;
        return total - (total * PromotionDiscount);
    }
}

// --- Context class ---

// INTERVIEW ANSWER: The context class (ShoppingCart here) holds a reference to
// a strategy interface, not a concrete implementation. You can swap the strategy
// at runtime without the cart knowing or caring. This is the essence of the
// Strategy pattern — decouple the algorithm from the code that uses it.
public class ShoppingCart
{
    private readonly List<(string Item, decimal Price, int Quantity)> _items = [];
    private IPricingStrategy _pricingStrategy;

    public ShoppingCart(IPricingStrategy pricingStrategy)
    {
        _pricingStrategy = pricingStrategy;
    }

    public void SetPricingStrategy(IPricingStrategy strategy)
    {
        Console.WriteLine($"  Switched pricing to: {strategy.Name}");
        _pricingStrategy = strategy;
    }

    public void AddItem(string name, decimal price, int quantity)
    {
        _items.Add((name, price, quantity));
    }

    public decimal CalculateTotal(CustomerProfile customer)
    {
        decimal total = 0;
        foreach (var (item, price, qty) in _items)
        {
            var lineTotal = _pricingStrategy.CalculatePrice(price, qty, customer);
            Console.WriteLine($"    {item}: {qty}x {price:C} = {lineTotal:C} ({_pricingStrategy.Name})");
            total += lineTotal;
        }
        return total;
    }
}

// --- Strategy factory (common companion to the pattern) ---

// INTERVIEW ANSWER: A factory that selects the strategy based on runtime conditions
// is a natural companion to the Strategy pattern. The factory encapsulates the
// selection logic so the client doesn't need to know about all the strategies.
public static class PricingStrategyFactory
{
    public static IPricingStrategy Create(CustomerProfile customer, string? promoCode = null)
    {
        // Promo code takes priority
        if (promoCode is not null)
        {
            var discount = promoCode.ToUpperInvariant() switch
            {
                "SUMMER25" => 0.25m,
                "WELCOME10" => 0.10m,
                "FLASH50" => 0.50m,
                _ => 0m
            };
            if (discount > 0)
                return new PromotionalPricing(promoCode, discount);
        }

        // High-value customers get loyalty pricing
        if (customer.Tier is "Gold" or "Platinum" || customer.LifetimeSpend > 5_000m)
            return new LoyaltyPricing();

        return new StandardPricing();
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== STRATEGY PATTERN DEMO ===\n");

var customer = new CustomerProfile("C-001", "Alice Chen", "Gold", 47, 12_500m);
Console.WriteLine($"Customer: {customer.Name} (Tier: {customer.Tier}, " +
                  $"Orders: {customer.TotalOrders}, Lifetime: {customer.LifetimeSpend:C})\n");

// --- Standard Pricing ---
Console.WriteLine("--- Standard Pricing ---");
var cart = new ShoppingCart(new StandardPricing());
cart.AddItem("Widget Pro", 29.99m, 5);
cart.AddItem("Gadget Plus", 49.99m, 2);
var total = cart.CalculateTotal(customer);
Console.WriteLine($"  Total: {total:C}\n");

// --- Switch to Bulk Pricing at Runtime ---
Console.WriteLine("--- Bulk Discount Pricing (runtime swap) ---");
cart.SetPricingStrategy(new BulkDiscountPricing());
total = cart.CalculateTotal(customer);
Console.WriteLine($"  Total: {total:C}\n");

// --- Loyalty Pricing ---
Console.WriteLine("--- Loyalty Pricing ---");
cart.SetPricingStrategy(new LoyaltyPricing());
total = cart.CalculateTotal(customer);
Console.WriteLine($"  Total: {total:C}\n");

// --- Promotional Pricing ---
Console.WriteLine("--- Promotional Pricing (SUMMER25) ---");
cart.SetPricingStrategy(new PromotionalPricing("SUMMER25", 0.25m));
total = cart.CalculateTotal(customer);
Console.WriteLine($"  Total: {total:C}\n");

// --- Factory-based Strategy Selection ---
Console.WriteLine("--- Factory-based Strategy Selection ---\n");

var customers = new[]
{
    new CustomerProfile("C-002", "Bob (New)", "Bronze", 2, 150m),
    new CustomerProfile("C-003", "Carol (VIP)", "Platinum", 100, 25_000m),
    new CustomerProfile("C-004", "Dave (Promo)", "Silver", 10, 800m),
};

string?[] promoCodes = [null, null, "FLASH50"];

for (int i = 0; i < customers.Length; i++)
{
    var c = customers[i];
    var strategy = PricingStrategyFactory.Create(c, promoCodes[i]);
    Console.WriteLine($"  {c.Name}: Strategy = {strategy.Name}");

    var testCart = new ShoppingCart(strategy);
    testCart.AddItem("Test Item", 100m, 1);
    var t = testCart.CalculateTotal(c);
    Console.WriteLine($"  {c.Name} pays: {t:C}\n");
}
