// ============================================================================
// TOPIC: State Pattern
// ============================================================================
// INTERVIEW ANSWER:
// The State pattern lets an object alter its behavior when its internal state
// changes, making it appear as if the object changed its class. Instead of using
// large if/else or switch blocks to handle different states, you extract each
// state into its own class that implements a common interface. The context object
// delegates behavior to the current state object. Transitions between states are
// explicit and self-documenting. This is closely related to finite state machines.
// ============================================================================

// --- State interface ---

// INTERVIEW ANSWER: The state interface declares methods for all behaviors
// that change with state. Each concrete state implements these differently.
// The context delegates to whichever state is current.
public interface IOrderState
{
    string StateName { get; }
    void Confirm(OrderContext order);
    void Ship(OrderContext order);
    void Deliver(OrderContext order);
    void Cancel(OrderContext order);
}

// --- Context ---

// INTERVIEW ANSWER: The context maintains a reference to the current state
// and delegates all state-dependent behavior to it. The context exposes a
// method for states to trigger transitions. Client code interacts with the
// context, not with states directly.
public class OrderContext
{
    private IOrderState _state;

    public string OrderId { get; }
    public string CustomerName { get; }
    public List<string> History { get; } = [];

    public OrderContext(string orderId, string customerName)
    {
        OrderId = orderId;
        CustomerName = customerName;
        _state = new DraftState();
        LogTransition("Order created");
    }

    public void TransitionTo(IOrderState newState)
    {
        var oldName = _state.StateName;
        _state = newState;
        LogTransition($"{oldName} → {newState.StateName}");
    }

    public void Confirm() => _state.Confirm(this);
    public void Ship() => _state.Ship(this);
    public void Deliver() => _state.Deliver(this);
    public void Cancel() => _state.Cancel(this);

    public string CurrentState => _state.StateName;

    private void LogTransition(string message)
    {
        var entry = $"[{DateTime.UtcNow:HH:mm:ss}] {message}";
        History.Add(entry);
        Console.WriteLine($"    {entry}");
    }

    public void PrintHistory()
    {
        Console.WriteLine($"  Order {OrderId} history:");
        foreach (var entry in History)
            Console.WriteLine($"    {entry}");
    }
}

// --- Concrete states ---

public class DraftState : IOrderState
{
    public string StateName => "Draft";

    public void Confirm(OrderContext order)
    {
        Console.WriteLine($"    Validating order {order.OrderId}...");
        order.TransitionTo(new ConfirmedState());
    }

    public void Ship(OrderContext order) =>
        Console.WriteLine("    Cannot ship a draft order — confirm it first");

    public void Deliver(OrderContext order) =>
        Console.WriteLine("    Cannot deliver a draft order");

    public void Cancel(OrderContext order)
    {
        order.TransitionTo(new CancelledState());
    }
}

// INTERVIEW ANSWER: Each state class encapsulates the behavior for that state.
// Invalid transitions are handled gracefully — the state knows what operations
// are valid. This eliminates scattered state-checking conditionals throughout
// the codebase.
public class ConfirmedState : IOrderState
{
    public string StateName => "Confirmed";

    public void Confirm(OrderContext order) =>
        Console.WriteLine("    Order is already confirmed");

    public void Ship(OrderContext order)
    {
        Console.WriteLine($"    Preparing shipment for order {order.OrderId}...");
        order.TransitionTo(new ShippedState());
    }

    public void Deliver(OrderContext order) =>
        Console.WriteLine("    Cannot deliver — order hasn't been shipped yet");

    public void Cancel(OrderContext order)
    {
        Console.WriteLine($"    Cancelling confirmed order {order.OrderId}, issuing refund...");
        order.TransitionTo(new CancelledState());
    }
}

public class ShippedState : IOrderState
{
    public string StateName => "Shipped";

    public void Confirm(OrderContext order) =>
        Console.WriteLine("    Order already confirmed and shipped");

    public void Ship(OrderContext order) =>
        Console.WriteLine("    Order is already in transit");

    public void Deliver(OrderContext order)
    {
        Console.WriteLine($"    Order {order.OrderId} delivered to {order.CustomerName}");
        order.TransitionTo(new DeliveredState());
    }

    public void Cancel(OrderContext order) =>
        Console.WriteLine("    Cannot cancel — order is already in transit");
}

public class DeliveredState : IOrderState
{
    public string StateName => "Delivered";

    public void Confirm(OrderContext order) =>
        Console.WriteLine("    Order already delivered");

    public void Ship(OrderContext order) =>
        Console.WriteLine("    Order already delivered");

    public void Deliver(OrderContext order) =>
        Console.WriteLine("    Order already delivered");

    public void Cancel(OrderContext order) =>
        Console.WriteLine("    Cannot cancel a delivered order — initiate a return instead");
}

public class CancelledState : IOrderState
{
    public string StateName => "Cancelled";

    public void Confirm(OrderContext order) =>
        Console.WriteLine("    Cannot confirm a cancelled order");

    public void Ship(OrderContext order) =>
        Console.WriteLine("    Cannot ship a cancelled order");

    public void Deliver(OrderContext order) =>
        Console.WriteLine("    Cannot deliver a cancelled order");

    public void Cancel(OrderContext order) =>
        Console.WriteLine("    Order is already cancelled");
}

// --- Second example: Traffic light (classic state machine) ---

public interface ITrafficLightState
{
    string Color { get; }
    int DurationSeconds { get; }
    ITrafficLightState Next();
    string GetInstruction();
}

public class RedLight : ITrafficLightState
{
    public string Color => "RED";
    public int DurationSeconds => 30;
    public ITrafficLightState Next() => new GreenLight();
    public string GetInstruction() => "STOP — Wait for green";
}

public class GreenLight : ITrafficLightState
{
    public string Color => "GREEN";
    public int DurationSeconds => 25;
    public ITrafficLightState Next() => new YellowLight();
    public string GetInstruction() => "GO — Proceed with caution";
}

public class YellowLight : ITrafficLightState
{
    public string Color => "YELLOW";
    public int DurationSeconds => 5;
    public ITrafficLightState Next() => new RedLight();
    public string GetInstruction() => "CAUTION — Prepare to stop";
}

public class TrafficLight
{
    private ITrafficLightState _state;

    public TrafficLight() => _state = new RedLight();

    public void Advance()
    {
        _state = _state.Next();
    }

    public void Display()
    {
        Console.WriteLine($"    [{_state.Color}] {_state.GetInstruction()} ({_state.DurationSeconds}s)");
    }
}

// ============================================================================
// DEMO
// ============================================================================

Console.WriteLine("=== STATE PATTERN DEMO ===\n");

// --- Happy path: Draft → Confirmed → Shipped → Delivered ---
Console.WriteLine("--- Order Lifecycle (Happy Path) ---");
var order1 = new OrderContext("ORD-001", "Alice");
Console.WriteLine($"  State: {order1.CurrentState}\n");

order1.Confirm();
Console.WriteLine($"  State: {order1.CurrentState}\n");

order1.Ship();
Console.WriteLine($"  State: {order1.CurrentState}\n");

order1.Deliver();
Console.WriteLine($"  State: {order1.CurrentState}\n");

// --- Invalid transitions ---
Console.WriteLine("--- Invalid Transitions ---");
Console.WriteLine("  Trying to ship a delivered order:");
order1.Ship();

Console.WriteLine("  Trying to cancel a delivered order:");
order1.Cancel();

// --- Cancellation path ---
Console.WriteLine("\n--- Order Cancellation ---");
var order2 = new OrderContext("ORD-002", "Bob");
order2.Confirm();
order2.Cancel();
Console.WriteLine($"  State: {order2.CurrentState}\n");

Console.WriteLine("  Trying to confirm cancelled order:");
order2.Confirm();

// --- Skip-ahead attempt ---
Console.WriteLine("\n--- Skip-ahead Attempt ---");
var order3 = new OrderContext("ORD-003", "Carol");
Console.WriteLine("  Trying to ship a draft order:");
order3.Ship();
Console.WriteLine("  Trying to deliver a draft order:");
order3.Deliver();

// --- Order history ---
Console.WriteLine("\n--- Order History ---");
order1.PrintHistory();

// --- Traffic light state machine ---
Console.WriteLine("\n--- Traffic Light State Machine ---");
var light = new TrafficLight();
for (int i = 0; i < 7; i++)
{
    light.Display();
    light.Advance();
}
