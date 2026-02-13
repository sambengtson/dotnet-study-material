// ============================================================================
// TOPIC: Facade Pattern
// ============================================================================
// INTERVIEW ANSWER:
// The Facade pattern provides a simplified interface to a complex subsystem. It
// doesn't add new functionality — it just wraps a set of complicated classes,
// libraries, or APIs behind one easy-to-use class. The subsystem classes still
// exist and can be used directly if needed, but for common use cases, the facade
// makes things much simpler. Think of it like a hotel concierge: you just say
// what you want, and they coordinate all the details with different departments.
// ============================================================================

// --- Complex subsystem classes ---

// INTERVIEW ANSWER: These subsystem classes have complex interfaces and
// interdependencies. Each does one thing well, but coordinating them requires
// understanding how they fit together. The facade hides this complexity.

public class InventoryService
{
    private readonly Dictionary<string, int> _stock = new()
    {
        ["SKU-001"] = 50,
        ["SKU-002"] = 3,
        ["SKU-003"] = 0
    };

    public bool CheckAvailability(string sku, int quantity)
    {
        var available = _stock.GetValueOrDefault(sku, 0);
        Console.WriteLine($"    [Inventory] {sku}: {available} in stock, need {quantity}");
        return available >= quantity;
    }

    public void ReserveStock(string sku, int quantity)
    {
        _stock[sku] -= quantity;
        Console.WriteLine($"    [Inventory] Reserved {quantity}x {sku} ({_stock[sku]} remaining)");
    }

    public void ReleaseStock(string sku, int quantity)
    {
        _stock[sku] += quantity;
        Console.WriteLine($"    [Inventory] Released {quantity}x {sku} ({_stock[sku]} remaining)");
    }
}

public class PaymentService
{
    public PaymentConfirmation ProcessPayment(string customerId, decimal amount, string paymentMethod)
    {
        Console.WriteLine($"    [Payment] Charging {customerId}: {amount:C} via {paymentMethod}");
        return new PaymentConfirmation($"PAY-{Random.Shared.Next(10000, 99999)}", true);
    }

    public void RefundPayment(string paymentId, decimal amount)
    {
        Console.WriteLine($"    [Payment] Refunding {amount:C} for {paymentId}");
    }
}

public record PaymentConfirmation(string PaymentId, bool Success);

public class ShippingService
{
    public string CreateShipment(string orderId, ShippingAddress address, string shippingMethod)
    {
        var trackingNumber = $"TRACK-{Random.Shared.Next(100000, 999999)}";
        Console.WriteLine($"    [Shipping] Created shipment for {orderId}");
        Console.WriteLine($"    [Shipping] Method: {shippingMethod}, To: {address.City}, {address.State}");
        Console.WriteLine($"    [Shipping] Tracking: {trackingNumber}");
        return trackingNumber;
    }

    public decimal CalculateShippingCost(ShippingAddress address, string method, decimal orderWeight)
    {
        var baseCost = method switch
        {
            "express" => 15.99m,
            "standard" => 5.99m,
            "overnight" => 29.99m,
            _ => 7.99m
        };
        return baseCost + (orderWeight * 0.50m);
    }
}

public record ShippingAddress(string Street, string City, string State, string Zip);

public class NotificationService
{
    public void SendOrderConfirmation(string email, string orderId, string trackingNumber)
    {
        Console.WriteLine($"    [Notification] Email to {email}: Order {orderId} confirmed (tracking: {trackingNumber})");
    }

    public void SendShipmentUpdate(string email, string trackingNumber, string status)
    {
        Console.WriteLine($"    [Notification] Email to {email}: Shipment {trackingNumber} - {status}");
    }
}

public class TaxCalculator
{
    public decimal CalculateTax(decimal subtotal, string state)
    {
        var rate = state switch
        {
            "CA" => 0.0725m,
            "NY" => 0.08m,
            "TX" => 0.0625m,
            "OR" => 0.0m,
            _ => 0.05m
        };
        var tax = subtotal * rate;
        Console.WriteLine($"    [Tax] {state} rate: {rate:P1}, tax on {subtotal:C}: {tax:C}");
        return tax;
    }
}

// --- Facade ---

// INTERVIEW ANSWER: The facade coordinates all subsystems to fulfill high-level
// operations. The client only needs to know about OrderFacade and its simple
// methods. If the subsystem changes (e.g., we switch payment providers), only
// the facade needs updating — client code stays the same.
public class OrderFacade
{
    private readonly InventoryService _inventory;
    private readonly PaymentService _payment;
    private readonly ShippingService _shipping;
    private readonly NotificationService _notification;
    private readonly TaxCalculator _tax;

    public OrderFacade(
        InventoryService inventory,
        PaymentService payment,
        ShippingService shipping,
        NotificationService notification,
        TaxCalculator tax)
    {
        _inventory = inventory;
        _payment = payment;
        _shipping = shipping;
        _notification = notification;
        _tax = tax;
    }

    public OrderResult PlaceOrder(OrderRequest request)
    {
        Console.WriteLine($"  Processing order for {request.CustomerEmail}...\n");

        // Step 1: Check inventory
        if (!_inventory.CheckAvailability(request.Sku, request.Quantity))
            return OrderResult.Failure("Item out of stock");

        // Step 2: Calculate costs
        var subtotal = request.UnitPrice * request.Quantity;
        var tax = _tax.CalculateTax(subtotal, request.ShippingAddress.State);
        var shippingCost = _shipping.CalculateShippingCost(
            request.ShippingAddress, request.ShippingMethod, request.Quantity * 0.5m);
        var total = subtotal + tax + shippingCost;

        Console.WriteLine($"    [Order] Subtotal: {subtotal:C}, Tax: {tax:C}, " +
                          $"Shipping: {shippingCost:C}, Total: {total:C}\n");

        // Step 3: Process payment
        var payment = _payment.ProcessPayment(request.CustomerId, total, request.PaymentMethod);
        if (!payment.Success)
        {
            return OrderResult.Failure("Payment declined");
        }

        // Step 4: Reserve inventory
        _inventory.ReserveStock(request.Sku, request.Quantity);

        // Step 5: Create shipment
        var orderId = $"ORD-{Random.Shared.Next(10000, 99999)}";
        var tracking = _shipping.CreateShipment(orderId, request.ShippingAddress, request.ShippingMethod);

        // Step 6: Send confirmation
        _notification.SendOrderConfirmation(request.CustomerEmail, orderId, tracking);

        Console.WriteLine();
        return OrderResult.Successful(orderId, tracking, total);
    }
}

public record OrderRequest(
    string CustomerId,
    string CustomerEmail,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    ShippingAddress ShippingAddress,
    string ShippingMethod,
    string PaymentMethod);

public class OrderResult
{
    public bool Success { get; init; }
    public string? OrderId { get; init; }
    public string? TrackingNumber { get; init; }
    public decimal Total { get; init; }
    public string? ErrorMessage { get; init; }

    public static OrderResult Successful(string orderId, string tracking, decimal total) =>
        new() { Success = true, OrderId = orderId, TrackingNumber = tracking, Total = total };

    public static OrderResult Failure(string error) =>
        new() { Success = false, ErrorMessage = error };
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== FACADE PATTERN DEMO ===\n");

// Create subsystems
var inventory = new InventoryService();
var payment = new PaymentService();
var shipping = new ShippingService();
var notification = new NotificationService();
var tax = new TaxCalculator();

// Create facade
var orderFacade = new OrderFacade(inventory, payment, shipping, notification, tax);

// --- Successful order (one simple call hides all complexity) ---
Console.WriteLine("--- Successful Order ---");
var result = orderFacade.PlaceOrder(new OrderRequest(
    CustomerId: "CUST-100",
    CustomerEmail: "alice@example.com",
    Sku: "SKU-001",
    Quantity: 5,
    UnitPrice: 29.99m,
    ShippingAddress: new ShippingAddress("123 Main St", "San Francisco", "CA", "94105"),
    ShippingMethod: "express",
    PaymentMethod: "credit_card"));

Console.WriteLine($"  Order Result: {(result.Success ? "SUCCESS" : "FAILED")}");
if (result.Success)
    Console.WriteLine($"  Order: {result.OrderId}, Tracking: {result.TrackingNumber}, Total: {result.Total:C}");

// --- Failed order (out of stock) ---
Console.WriteLine("\n--- Out of Stock Order ---");
var failedResult = orderFacade.PlaceOrder(new OrderRequest(
    CustomerId: "CUST-200",
    CustomerEmail: "bob@example.com",
    Sku: "SKU-003",
    Quantity: 1,
    UnitPrice: 49.99m,
    ShippingAddress: new ShippingAddress("456 Oak Ave", "Portland", "OR", "97201"),
    ShippingMethod: "standard",
    PaymentMethod: "debit_card"));

Console.WriteLine($"  Order Result: {(failedResult.Success ? "SUCCESS" : "FAILED")}");
if (!failedResult.Success)
    Console.WriteLine($"  Reason: {failedResult.ErrorMessage}");
