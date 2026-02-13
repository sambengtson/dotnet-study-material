// ============================================================================
// TOPIC: Adapter Pattern
// ============================================================================
// INTERVIEW ANSWER:
// The Adapter pattern allows objects with incompatible interfaces to collaborate.
// It wraps an existing class with a new interface so it can work with code that
// expects a different interface. Think of it like a power adapter when traveling —
// the outlet (existing system) hasn't changed, but the adapter lets your device
// (new code) plug into it. There are two forms: object adapter (uses composition,
// preferred) and class adapter (uses inheritance, less flexible).
// ============================================================================

// --- Target interface (what our application expects) ---

public interface IPaymentProcessor
{
    string ProcessorName { get; }
    PaymentResult Charge(string customerId, decimal amount, string currency);
    PaymentResult Refund(string transactionId, decimal amount);
}

public record PaymentResult(bool Success, string TransactionId, string Message);

// --- Adaptee #1: Legacy payment system with incompatible interface ---

// INTERVIEW ANSWER: The adaptee is the existing class with an incompatible
// interface. We can't modify it — maybe it's a third-party library, a legacy
// system, or code owned by another team. The adapter bridges the gap.
public class LegacyPaymentGateway
{
    public int MakePayment(string account, double amountInCents, string curr)
    {
        Console.WriteLine($"    [Legacy] Charging account {account}: {amountInCents} cents ({curr})");
        // Returns a numeric status code: 0 = success, non-zero = error
        return 0;
    }

    public int ReversePayment(int transactionCode, double amountInCents)
    {
        Console.WriteLine($"    [Legacy] Reversing transaction {transactionCode}: {amountInCents} cents");
        return 0;
    }

    public int GetLastTransactionCode() => Random.Shared.Next(10000, 99999);
}

// --- Adaptee #2: Modern third-party API with different interface ---

public class StripeApiClient
{
    public StripeResponse CreateCharge(StripeChargeRequest request)
    {
        Console.WriteLine($"    [Stripe] Creating charge: {request.Amount} {request.Currency} for {request.CustomerRef}");
        return new StripeResponse
        {
            Id = $"ch_{Guid.NewGuid().ToString()[..8]}",
            Status = "succeeded",
            ErrorMessage = null
        };
    }

    public StripeResponse CreateRefund(StripeRefundRequest request)
    {
        Console.WriteLine($"    [Stripe] Refunding {request.Amount} from charge {request.ChargeId}");
        return new StripeResponse
        {
            Id = $"re_{Guid.NewGuid().ToString()[..8]}",
            Status = "succeeded",
            ErrorMessage = null
        };
    }
}

public class StripeChargeRequest
{
    public long Amount { get; set; }         // In smallest currency unit (cents)
    public string Currency { get; set; } = "";
    public string CustomerRef { get; set; } = "";
}

public class StripeRefundRequest
{
    public string ChargeId { get; set; } = "";
    public long Amount { get; set; }
}

public class StripeResponse
{
    public string Id { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ErrorMessage { get; set; }
}

// --- Adapter for legacy system ---

// INTERVIEW ANSWER: The adapter implements the target interface and wraps the
// adaptee. It translates calls from the target interface into calls the adaptee
// understands. All the conversion logic (decimal to cents, result codes to
// booleans, etc.) lives in the adapter, keeping both sides clean.
public class LegacyPaymentAdapter : IPaymentProcessor
{
    private readonly LegacyPaymentGateway _legacy;

    public LegacyPaymentAdapter(LegacyPaymentGateway legacy)
    {
        _legacy = legacy;
    }

    public string ProcessorName => "Legacy Gateway (Adapted)";

    public PaymentResult Charge(string customerId, decimal amount, string currency)
    {
        // Convert decimal dollars to double cents (legacy API format)
        double cents = (double)(amount * 100);
        int statusCode = _legacy.MakePayment(customerId, cents, currency);
        int txnCode = _legacy.GetLastTransactionCode();

        return statusCode == 0
            ? new PaymentResult(true, txnCode.ToString(), "Payment successful")
            : new PaymentResult(false, "", $"Payment failed with code {statusCode}");
    }

    public PaymentResult Refund(string transactionId, decimal amount)
    {
        double cents = (double)(amount * 100);
        int statusCode = _legacy.ReversePayment(int.Parse(transactionId), cents);

        return statusCode == 0
            ? new PaymentResult(true, transactionId, "Refund successful")
            : new PaymentResult(false, "", $"Refund failed with code {statusCode}");
    }
}

// --- Adapter for Stripe ---

public class StripePaymentAdapter : IPaymentProcessor
{
    private readonly StripeApiClient _stripe;

    public StripePaymentAdapter(StripeApiClient stripe)
    {
        _stripe = stripe;
    }

    public string ProcessorName => "Stripe (Adapted)";

    public PaymentResult Charge(string customerId, decimal amount, string currency)
    {
        var response = _stripe.CreateCharge(new StripeChargeRequest
        {
            Amount = (long)(amount * 100),
            Currency = currency.ToLowerInvariant(),
            CustomerRef = customerId
        });

        return response.Status == "succeeded"
            ? new PaymentResult(true, response.Id, "Charge created")
            : new PaymentResult(false, "", response.ErrorMessage ?? "Unknown error");
    }

    public PaymentResult Refund(string transactionId, decimal amount)
    {
        var response = _stripe.CreateRefund(new StripeRefundRequest
        {
            ChargeId = transactionId,
            Amount = (long)(amount * 100)
        });

        return response.Status == "succeeded"
            ? new PaymentResult(true, response.Id, "Refund created")
            : new PaymentResult(false, "", response.ErrorMessage ?? "Unknown error");
    }
}

// --- Client code that uses the target interface ---

public class CheckoutService
{
    private readonly IPaymentProcessor _processor;

    public CheckoutService(IPaymentProcessor processor)
    {
        _processor = processor;
    }

    public void ProcessOrder(string customerId, decimal total)
    {
        Console.WriteLine($"  Processing order via {_processor.ProcessorName}...");
        var result = _processor.Charge(customerId, total, "USD");
        Console.WriteLine($"  Result: {(result.Success ? "OK" : "FAILED")} " +
                          $"[{result.TransactionId}] {result.Message}\n");
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== ADAPTER PATTERN DEMO ===\n");

// --- Using adapted legacy system ---
Console.WriteLine("--- Legacy Payment System (Adapted) ---");
var legacyAdapter = new LegacyPaymentAdapter(new LegacyPaymentGateway());
var checkout1 = new CheckoutService(legacyAdapter);
checkout1.ProcessOrder("CUST-001", 149.99m);

// --- Using adapted Stripe API ---
Console.WriteLine("--- Stripe API (Adapted) ---");
var stripeAdapter = new StripePaymentAdapter(new StripeApiClient());
var checkout2 = new CheckoutService(stripeAdapter);
checkout2.ProcessOrder("CUST-002", 299.50m);

// --- Polymorphic usage: same client code, different adapters ---
Console.WriteLine("--- Polymorphic Usage ---");
IPaymentProcessor[] processors =
[
    new LegacyPaymentAdapter(new LegacyPaymentGateway()),
    new StripePaymentAdapter(new StripeApiClient())
];

foreach (var processor in processors)
{
    var service = new CheckoutService(processor);
    service.ProcessOrder("CUST-003", 75.00m);
}
