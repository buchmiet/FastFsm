using Abstractions.Attributes;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;


var vaultUri = new Uri("https://denmain.vault.azure.net/");

var client = new SecretClient(vaultUri, new DefaultAzureCredential());

Console.WriteLine("Pobieram sekret z Azure Key Vault...");
try
{
    KeyVaultSecret secret = await client.GetSecretAsync("test-secret");
    Console.WriteLine($"Sekret ‘test-secret’: {secret.Value}");
}
catch (Exception ex)
{
    Console.WriteLine($"Błąd: {ex.Message}");
}

var machine = new OrderMachine(OrderState.New);
machine.Fire(OrderTrigger.Submit);
Console.WriteLine($"State: {machine.CurrentState}");

[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
public partial class OrderMachine
{
    [Transition(OrderState.New, OrderTrigger.Submit, OrderState.Submitted)]
    private void Configure() { }
}

public enum OrderState { New, Submitted, Shipped }
public enum OrderTrigger { Submit, Ship }


//// See https://aka.ms/new-console-template for more information
//using Experiments;

//FileLogger.Log("Application started");
//Console.WriteLine("Hello, World!");

//try
//{
//    var typeTracker = new PayloadTypeTracker();
//    FileLogger.Log("Creating PayloadTypeTracker");

//    // Upewnij się, że maszyna ma konstruktor przyjmujący rozszerzenia
//    FileLogger.Log("Creating FullMultiPayloadMachine");
//    var machine = new FullMultiPayloadMachine(OrderState.New, new[] { typeTracker });
//    FileLogger.Log("Machine created");

//    //// === Krok 1: Tylko pierwsze przejście ===
//    Console.WriteLine("--- Krok 1: Przejście Process -> Processing ---");
//    FileLogger.Log("About to call TryFire");

//    var processResult = machine.TryFire(OrderTrigger.Process, new FullVariantExtendedTests.OrderPayload { OrderId = 1 });

//    FileLogger.Log($"TryFire result: {processResult}");
//}
//catch (Exception ex)
//{
//    FileLogger.Log($"EXCEPTION: {ex.GetType().Name} - {ex.Message}");
//    FileLogger.Log($"Stack trace: {ex.StackTrace}");
//    throw;
//}

//public class FullVariantExtendedTests()
//{
//    public class OrderPayload
//    {
//        public int OrderId { get; set; }
//        public decimal Amount { get; set; }
//        public string? TrackingNumber { get; set; }
//    }
//    public class PaymentPayload : OrderPayload
//    {
//        public string PaymentMethod { get; set; } = "";
//        public DateTime PaymentDate { get; set; }
//    }

//    public class ShippingPayload : OrderPayload
//    {
//        public string Carrier { get; set; } = "";
//        public DateTime EstimatedDelivery { get; set; }
//    }
//}