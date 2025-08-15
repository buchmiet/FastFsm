using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public readonly record struct OrderStatus(string Value)
{
    public static readonly OrderStatus New     = "new";
    public static readonly OrderStatus Paid    = "paid";
    public static readonly OrderStatus Shipped = "shipped";

    public static implicit operator OrderStatus(string s) => new(s);
    public static implicit operator string(OrderStatus s) => s.Value;
}

public enum OrderTrigger { Pay, Ship }

/// <summary>
/// Minimalny FSM proof-of-concept dla string-enum:
/// Publicznie: OrderStatus (record struct z nazwą)
/// Wewnętrznie: indeks int (_stateId) + parser bez alokacji
/// </summary>
public sealed class OrderFsm_PoC
{
    private static readonly string[] s_names = new[] { "new", "paid", "shipped" };
    private int _stateId;
    private bool _started;

    public OrderFsm_PoC(OrderStatus initial)
    {
        if (!TryParseName(initial.Value, out _stateId))
            throw new ArgumentOutOfRangeException(nameof(initial), $"Unknown state '{initial.Value}'");
    }

    public OrderStatus CurrentState => s_names[_stateId];

    public void Start() => _started = true;

    public bool CanFire(OrderTrigger t)
    {
        EnsureStarted();
        return GetTransition(_stateId, t) >= 0;
    }

    public void Fire(OrderTrigger t)
    {
        EnsureStarted();
        var to = GetTransition(_stateId, t);
        if (to < 0) throw new InvalidOperationException("Transition not permitted");
        _stateId = to;
    }

    // Parser bez alokacji: rozgałęzienie po długości, SequenceEqual nad Span
    public static bool TryParseName(ReadOnlySpan<char> s, out int id)
    {
        switch (s.Length)
        {
            case 3:
                if (s.SequenceEqual("new".AsSpan())) { id = 0; return true; }
                break;
            case 4:
                if (s.SequenceEqual("paid".AsSpan())) { id = 1; return true; }
                break;
            case 7:
                if (s.SequenceEqual("shipped".AsSpan())) { id = 2; return true; }
                break;
        }
        id = -1;
        return false;
    }

    private static int GetTransition(int fromId, OrderTrigger trigger) =>
        (fromId, trigger) switch
        {
            (0, OrderTrigger.Pay)  => 1, // new -> paid
            (1, OrderTrigger.Ship) => 2, // paid -> shipped
            _ => -1
        };

    private void EnsureStarted()
    {
        if (!_started) throw new InvalidOperationException("Call Start() first");
    }
}

/// <summary>
/// JsonConverter dla OrderStatus:
/// - Serializuje jako sam string (np. "paid").
/// - Deserializuje z stringa; waliduje do znanego zbioru (opcjonalnie).
/// W realnym generatorze można to powiązać z mapami compile-time.
/// </summary>
public sealed class OrderStatusJsonConverter : JsonConverter<OrderStatus>
{
    private readonly bool _validate;

    public OrderStatusJsonConverter(bool validateKnownValues = true)
    {
        _validate = validateKnownValues;
    }

    public override OrderStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString() ?? throw new JsonException("Expected string for OrderStatus.");
        if (_validate && !OrderFsm_PoC.TryParseName(s.AsSpan(), out _))
            throw new JsonException($"Unknown OrderStatus '{s}'.");
        return new OrderStatus(s);
    }

    public override void Write(Utf8JsonWriter writer, OrderStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public sealed class Snapshot
{
    public OrderStatus State { get; set; }
}

public static class Program
{
    public static void Main()
    {
        var fsm = new OrderFsm_PoC(OrderStatus.New);
        fsm.Start();

        Console.WriteLine($"Start: {fsm.CurrentState.Value}");           // new
        Console.WriteLine($"Can Pay? {fsm.CanFire(OrderTrigger.Pay)}");  // True
        fsm.Fire(OrderTrigger.Pay);
        Console.WriteLine($"After Pay: {fsm.CurrentState.Value}");       // paid

        Console.WriteLine($"Can Ship? {fsm.CanFire(OrderTrigger.Ship)}");// True
        fsm.Fire(OrderTrigger.Ship);
        Console.WriteLine($"After Ship: {fsm.CurrentState.Value}");      // shipped

        // JSON demo
        var opts = new JsonSerializerOptions { WriteIndented = false };
        opts.Converters.Add(new OrderStatusJsonConverter(validateKnownValues: true));

        var snap = new Snapshot { State = fsm.CurrentState };
        var json = JsonSerializer.Serialize(snap, opts);
        Console.WriteLine($"JSON: {json}"); // {"State":"shipped"}

        var roundtrip = JsonSerializer.Deserialize<Snapshot>(json, opts)
                        ?? throw new Exception("Deserialize failed");
        Console.WriteLine($"Roundtrip State: {roundtrip.State.Value}"); // shipped

        // Próba z nieznaną wartością (spodziewany błąd JSON)
        try
        {
            var bad = "{\"State\":\"unknown\"}";
            _ = JsonSerializer.Deserialize<Snapshot>(bad, opts);
            Console.WriteLine("Unexpected: deserialized unknown value.");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Unknown check OK: {ex.Message}");
        }
    }
}