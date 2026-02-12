// ============================================================================
// TOPIC: Factory Pattern
// ============================================================================
// INTERVIEW ANSWER:
// The Factory pattern decouples object creation from usage. Instead of calling
// `new ConcreteClass()` directly, you ask a factory to create the object. This
// lets you change which implementation gets created without changing the code
// that uses it. There are two main variants: Factory Method (a single method
// that creates objects) and Abstract Factory (a family of related factories
// that produce compatible objects).
// ============================================================================

// --- Products ---

public interface IPaymentGateway
{
    string Name { get; }
    Task<PaymentResponse> ChargeAsync(decimal amount, string currency);
    Task<PaymentResponse> RefundAsync(string transactionId, decimal amount);
}

public record PaymentResponse(bool Success, string TransactionId, string Message);

public class StripeGateway : IPaymentGateway
{
    public string Name => "Stripe";

    public async Task<PaymentResponse> ChargeAsync(decimal amount, string currency)
    {
        await Task.Delay(50);
        var txId = $"stripe_ch_{Guid.NewGuid().ToString()[..8]}";
        Console.WriteLine($"  [Stripe] Charged {amount:C} {currency} → {txId}");
        return new(true, txId, "Charge successful");
    }

    public async Task<PaymentResponse> RefundAsync(string transactionId, decimal amount)
    {
        await Task.Delay(50);
        Console.WriteLine($"  [Stripe] Refunded {amount:C} for {transactionId}");
        return new(true, $"stripe_re_{Guid.NewGuid().ToString()[..8]}", "Refund processed");
    }
}

public class PayPalGateway : IPaymentGateway
{
    public string Name => "PayPal";

    public async Task<PaymentResponse> ChargeAsync(decimal amount, string currency)
    {
        await Task.Delay(50);
        var txId = $"pp_{Guid.NewGuid().ToString()[..8]}";
        Console.WriteLine($"  [PayPal] Charged {amount:C} {currency} → {txId}");
        return new(true, txId, "PayPal payment complete");
    }

    public async Task<PaymentResponse> RefundAsync(string transactionId, decimal amount)
    {
        await Task.Delay(50);
        Console.WriteLine($"  [PayPal] Refunded {amount:C} for {transactionId}");
        return new(true, $"pp_ref_{Guid.NewGuid().ToString()[..8]}", "PayPal refund issued");
    }
}

public class SquareGateway : IPaymentGateway
{
    public string Name => "Square";

    public async Task<PaymentResponse> ChargeAsync(decimal amount, string currency)
    {
        await Task.Delay(50);
        var txId = $"sq_{Guid.NewGuid().ToString()[..8]}";
        Console.WriteLine($"  [Square] Charged {amount:C} {currency} → {txId}");
        return new(true, txId, "Square payment processed");
    }

    public async Task<PaymentResponse> RefundAsync(string transactionId, decimal amount)
    {
        await Task.Delay(50);
        Console.WriteLine($"  [Square] Refunded {amount:C} for {transactionId}");
        return new(true, $"sq_ref_{Guid.NewGuid().ToString()[..8]}", "Square refund complete");
    }
}

// --- Factory Method Pattern ---

// INTERVIEW ANSWER: The Factory Method pattern uses a method to create objects.
// The method encapsulates the decision logic about which concrete type to
// instantiate. This centralizes creation logic so if you add a new payment
// provider, you only update the factory — not every place that creates gateways.
public static class PaymentGatewayFactory
{
    public static IPaymentGateway Create(string provider) => provider.ToLowerInvariant() switch
    {
        "stripe" => new StripeGateway(),
        "paypal" => new PayPalGateway(),
        "square" => new SquareGateway(),
        _ => throw new ArgumentException($"Unknown payment provider: {provider}")
    };

    // Overload that selects based on runtime conditions
    public static IPaymentGateway CreateForRegion(string region) => region.ToUpperInvariant() switch
    {
        "US" or "CA" => new StripeGateway(),
        "EU" or "UK" => new PayPalGateway(),
        "APAC" => new SquareGateway(),
        _ => new StripeGateway() // Default
    };
}

// --- Abstract Factory Pattern ---

// INTERVIEW ANSWER: Abstract Factory goes a step further — it's a factory of
// factories. It creates families of RELATED objects that are designed to work
// together. You get a factory interface, and each concrete factory produces a
// consistent set of products. This is useful when you have multiple related
// objects that must be compatible (like UI themes or cloud provider SDKs).
public interface ICloudServiceFactory
{
    string CloudProvider { get; }
    IStorageService CreateStorage();
    IQueueService CreateQueue();
    INotificationService CreateNotification();
}

public interface IStorageService
{
    Task UploadAsync(string path, string content);
}

public interface IQueueService
{
    Task EnqueueAsync(string message);
}

public interface INotificationService
{
    Task SendAsync(string recipient, string message);
}

// --- AWS Family ---

public class AwsServiceFactory : ICloudServiceFactory
{
    public string CloudProvider => "AWS";
    public IStorageService CreateStorage() => new S3Storage();
    public IQueueService CreateQueue() => new SqsQueue();
    public INotificationService CreateNotification() => new SnsNotification();
}

public class S3Storage : IStorageService
{
    public Task UploadAsync(string path, string content)
    {
        Console.WriteLine($"  [AWS S3] Uploaded to s3://bucket/{path} ({content.Length} bytes)");
        return Task.CompletedTask;
    }
}

public class SqsQueue : IQueueService
{
    public Task EnqueueAsync(string message)
    {
        Console.WriteLine($"  [AWS SQS] Enqueued: {message}");
        return Task.CompletedTask;
    }
}

public class SnsNotification : INotificationService
{
    public Task SendAsync(string recipient, string message)
    {
        Console.WriteLine($"  [AWS SNS] Sent to {recipient}: {message}");
        return Task.CompletedTask;
    }
}

// --- Azure Family ---

public class AzureServiceFactory : ICloudServiceFactory
{
    public string CloudProvider => "Azure";
    public IStorageService CreateStorage() => new BlobStorage();
    public IQueueService CreateQueue() => new ServiceBusQueue();
    public INotificationService CreateNotification() => new AzureNotification();
}

public class BlobStorage : IStorageService
{
    public Task UploadAsync(string path, string content)
    {
        Console.WriteLine($"  [Azure Blob] Uploaded to container/{path} ({content.Length} bytes)");
        return Task.CompletedTask;
    }
}

public class ServiceBusQueue : IQueueService
{
    public Task EnqueueAsync(string message)
    {
        Console.WriteLine($"  [Azure ServiceBus] Enqueued: {message}");
        return Task.CompletedTask;
    }
}

public class AzureNotification : INotificationService
{
    public Task SendAsync(string recipient, string message)
    {
        Console.WriteLine($"  [Azure NotificationHub] Sent to {recipient}: {message}");
        return Task.CompletedTask;
    }
}

// --- Client code that uses the abstract factory ---

public class OrderFulfillmentService(ICloudServiceFactory cloudFactory)
{
    public async Task FulfillOrderAsync(string orderId, string customerEmail)
    {
        Console.WriteLine($"  Fulfilling order {orderId} using {cloudFactory.CloudProvider}:");

        var storage = cloudFactory.CreateStorage();
        var queue = cloudFactory.CreateQueue();
        var notification = cloudFactory.CreateNotification();

        await storage.UploadAsync($"orders/{orderId}.json", $"{{\"id\":\"{orderId}\"}}");
        await queue.EnqueueAsync($"process:{orderId}");
        await notification.SendAsync(customerEmail, $"Order {orderId} confirmed!");
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== FACTORY PATTERN DEMO ===\n");

// --- Factory Method ---
Console.WriteLine("--- Factory Method: PaymentGatewayFactory ---\n");

string[] providers = ["Stripe", "PayPal", "Square"];
foreach (var provider in providers)
{
    var gateway = PaymentGatewayFactory.Create(provider);
    var chargeResult = await gateway.ChargeAsync(99.99m, "USD");
    Console.WriteLine($"  Result: {chargeResult}\n");
}

// Runtime selection based on region
Console.WriteLine("--- Factory Method: Region-based Selection ---\n");
string[] regions = ["US", "EU", "APAC"];
foreach (var region in regions)
{
    var gateway = PaymentGatewayFactory.CreateForRegion(region);
    Console.WriteLine($"  Region {region} → {gateway.Name}");
    await gateway.ChargeAsync(50m, "USD");
    Console.WriteLine();
}

// --- Abstract Factory ---
Console.WriteLine("--- Abstract Factory: Cloud Services ---\n");

ICloudServiceFactory[] factories = [new AwsServiceFactory(), new AzureServiceFactory()];

foreach (var factory in factories)
{
    Console.WriteLine($"Using {factory.CloudProvider}:");
    var fulfillment = new OrderFulfillmentService(factory);
    await fulfillment.FulfillOrderAsync("ORD-12345", "customer@example.com");
    Console.WriteLine();
}

// INTERVIEW ANSWER: The beauty of Abstract Factory is that OrderFulfillmentService
// doesn't import or reference any AWS or Azure classes directly. It works entirely
// through the ICloudServiceFactory interface. Switching cloud providers is just
// swapping which factory you pass in — zero changes to business logic.
