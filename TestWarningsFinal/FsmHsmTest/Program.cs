using System;
using Abstractions.Attributes;

namespace FsmHsmTestApp;

// ========================================
// TEST 1: PROSTA MASZYNA FSM (FLAT)
// ========================================

public enum DoorState { Open, Closed, Locked }
public enum DoorTrigger { OpenDoor, CloseDoor, LockDoor, UnlockDoor }

[StateMachine(typeof(DoorState), typeof(DoorTrigger))]
public partial class DoorController
{
    // Konfiguracja przejść
    [Transition(DoorState.Closed, DoorTrigger.OpenDoor, DoorState.Open)]
    [Transition(DoorState.Open, DoorTrigger.CloseDoor, DoorState.Closed)]
    [Transition(DoorState.Closed, DoorTrigger.LockDoor, DoorState.Locked)]
    [Transition(DoorState.Locked, DoorTrigger.UnlockDoor, DoorState.Closed)]
    private void ConfigureTransitions() { }
    
    // Konfiguracja stanów z akcjami wejścia/wyjścia
    [State(DoorState.Open, OnEntry = nameof(OnDoorOpened), OnExit = nameof(OnDoorClosing))]
    [State(DoorState.Locked, OnEntry = nameof(OnDoorLocked))]
    private void ConfigureStates() { }
    
    // Akcje
    private void OnDoorOpened() => Console.WriteLine("  [FSM] Drzwi otwarte!");
    private void OnDoorClosing() => Console.WriteLine("  [FSM] Zamykanie drzwi...");
    private void OnDoorLocked() => Console.WriteLine("  [FSM] Drzwi zablokowane!");
}

// ========================================
// TEST 2: HIERARCHICZNA MASZYNA HSM
// ========================================

public enum VehicleState 
{ 
    Parked,
    Running,              // Stan rodzic
    Running_Idle,         // Stan dziecko
    Running_Moving,       // Stan dziecko
    Running_Reversing     // Stan dziecko
}

public enum VehicleTrigger 
{ 
    StartEngine, 
    StopEngine, 
    Accelerate, 
    Brake, 
    ShiftReverse 
}

[StateMachine(typeof(VehicleState), typeof(VehicleTrigger), EnableHierarchy = true)]
public partial class VehicleController
{
    private int _speed = 0;
    
    // Konfiguracja hierarchii
    [State(VehicleState.Running, History = HistoryMode.Shallow, OnEntry = nameof(OnEngineStarted))]
    private void ConfigureRunning() { }
    
    [State(VehicleState.Running_Idle, Parent = VehicleState.Running, IsInitial = true)]
    private void ConfigureIdle() { }
    
    [State(VehicleState.Running_Moving, Parent = VehicleState.Running, 
        OnEntry = nameof(OnStartMoving), OnExit = nameof(OnStopMoving))]
    private void ConfigureMoving() { }
    
    [State(VehicleState.Running_Reversing, Parent = VehicleState.Running)]
    private void ConfigureReversing() { }
    
    // Przejścia
    [Transition(VehicleState.Parked, VehicleTrigger.StartEngine, VehicleState.Running)]
    [Transition(VehicleState.Running, VehicleTrigger.StopEngine, VehicleState.Parked)]
    [Transition(VehicleState.Running_Idle, VehicleTrigger.Accelerate, VehicleState.Running_Moving)]
    [Transition(VehicleState.Running_Moving, VehicleTrigger.Brake, VehicleState.Running_Idle)]
    [Transition(VehicleState.Running_Idle, VehicleTrigger.ShiftReverse, VehicleState.Running_Reversing)]
    [Transition(VehicleState.Running_Reversing, VehicleTrigger.Brake, VehicleState.Running_Idle)]
    private void ConfigureTransitions() { }
    
    // Przejście wewnętrzne (nie zmienia stanu)
    [InternalTransition(VehicleState.Running_Moving, VehicleTrigger.Accelerate, 
        Action = nameof(IncreaseSpeed))]
    private void ConfigureInternalTransitions() { }
    
    // Akcje
    private void OnEngineStarted() => Console.WriteLine("  [HSM] Silnik uruchomiony!");
    private void OnStartMoving() 
    { 
        _speed = 10;
        Console.WriteLine($"  [HSM] Rozpoczęto jazdę, prędkość: {_speed} km/h");
    }
    private void OnStopMoving() 
    {
        _speed = 0;
        Console.WriteLine("  [HSM] Zatrzymano pojazd");
    }
    private void IncreaseSpeed() 
    {
        _speed += 10;
        Console.WriteLine($"  [HSM] Przyspieszanie, prędkość: {_speed} km/h");
    }
}

// ========================================
// TEST 3: FSM Z GUARDS I PAYLOADS
// ========================================

public class TransferData
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public enum AccountState { Active, Frozen, Closed }
public enum AccountTrigger { Withdraw, Deposit, Freeze, Unfreeze, Close }

[StateMachine(typeof(AccountState), typeof(AccountTrigger), DefaultPayloadType = typeof(TransferData))]
public partial class BankAccount
{
    private decimal _balance = 1000m;
    private TransferData? _lastOperation;
    
    // Przejścia z guards
    [Transition(AccountState.Active, AccountTrigger.Withdraw, AccountState.Active,
        Guard = nameof(HasSufficientFunds), Action = nameof(ProcessWithdrawal))]
    [Transition(AccountState.Active, AccountTrigger.Deposit, AccountState.Active,
        Action = nameof(ProcessDeposit))]
    [Transition(AccountState.Active, AccountTrigger.Freeze, AccountState.Frozen)]
    [Transition(AccountState.Frozen, AccountTrigger.Unfreeze, AccountState.Active)]
    [Transition(AccountState.Active, AccountTrigger.Close, AccountState.Closed,
        Guard = nameof(IsBalanceZero))]
    private void ConfigureTransitions() { }
    
    // Guards
    private bool HasSufficientFunds(TransferData data) => 
        data != null && _balance >= data.Amount;
    
    private bool IsBalanceZero() => _balance == 0;
    
    // Akcje
    private void ProcessWithdrawal(TransferData data)
    {
        _balance -= data.Amount;
        _lastOperation = data;
        Console.WriteLine($"  [FSM+Guards] Wypłata {data.Amount:C}, saldo: {_balance:C}");
    }
    
    private void ProcessDeposit(TransferData data)
    {
        _balance += data.Amount;
        _lastOperation = data;
        Console.WriteLine($"  [FSM+Guards] Wpłata {data.Amount:C}, saldo: {_balance:C}");
    }
    
    public decimal GetBalance() => _balance;
}

// ========================================
// PROGRAM GŁÓWNY - TESTY
// ========================================

class Program
{
    static void Main()
    {
        Console.WriteLine("=== TEST KOMPILACJI FastFsm.Net 0.6.9.5 ===");
        Console.WriteLine("Testujemy FSM, HSM, Guards, Payloads, Internal Transitions");
        Console.WriteLine();
        
        // TEST 1: Prosta FSM
        Console.WriteLine("--- TEST 1: Prosta maszyna FSM (DoorController) ---");
        var door = new DoorController(DoorState.Closed);
        door.Start();
        
        Console.WriteLine($"Stan początkowy: {door.CurrentState}");
        door.Fire(DoorTrigger.OpenDoor);
        Console.WriteLine($"Po otwarciu: {door.CurrentState}");
        door.Fire(DoorTrigger.CloseDoor);
        door.Fire(DoorTrigger.LockDoor);
        Console.WriteLine($"Stan końcowy: {door.CurrentState}");
        Console.WriteLine();
        
        // TEST 2: Hierarchiczna HSM
        Console.WriteLine("--- TEST 2: Hierarchiczna maszyna HSM (VehicleController) ---");
        var vehicle = new VehicleController(VehicleState.Parked);
        vehicle.Start();
        
        Console.WriteLine($"Stan początkowy: {vehicle.CurrentState}");
        vehicle.Fire(VehicleTrigger.StartEngine);
        Console.WriteLine($"Po uruchomieniu: {vehicle.CurrentState}");
        
        vehicle.Fire(VehicleTrigger.Accelerate);
        Console.WriteLine($"Po przyspieszeniu: {vehicle.CurrentState}");
        
        // Test internal transition
        vehicle.Fire(VehicleTrigger.Accelerate); // Wewnętrzne przejście
        
        vehicle.Fire(VehicleTrigger.Brake);
        Console.WriteLine($"Po hamowaniu: {vehicle.CurrentState}");
        
        // Test historii
        vehicle.Fire(VehicleTrigger.StopEngine);
        Console.WriteLine($"Po wyłączeniu: {vehicle.CurrentState}");
        vehicle.Fire(VehicleTrigger.StartEngine);
        Console.WriteLine($"Po ponownym uruchomieniu (historia): {vehicle.CurrentState}");
        Console.WriteLine();
        
        // TEST 3: FSM z Guards i Payloads
        Console.WriteLine("--- TEST 3: FSM z Guards i Payloads (BankAccount) ---");
        var account = new BankAccount(AccountState.Active);
        account.Start();
        
        Console.WriteLine($"Stan początkowy: {account.CurrentState}, Saldo: {account.GetBalance():C}");
        
        // Próba wypłaty z wystarczającymi środkami
        var withdrawal = new TransferData { Amount = 300m, Description = "Wypłata z bankomatu" };
        if (account.CanFire(AccountTrigger.Withdraw))
        {
            account.Fire(AccountTrigger.Withdraw, withdrawal);
        }
        
        // Wpłata
        var deposit = new TransferData { Amount = 500m, Description = "Przelew przychodzący" };
        account.Fire(AccountTrigger.Deposit, deposit);
        
        // Próba wypłaty przekraczającej saldo (guard powinien zablokować)
        var largeWithdrawal = new TransferData { Amount = 2000m, Description = "Duża wypłata" };
        Console.WriteLine($"Czy można wypłacić 2000? {account.CanFire(AccountTrigger.Withdraw)}");
        
        Console.WriteLine($"Stan końcowy: {account.CurrentState}, Saldo: {account.GetBalance():C}");
        Console.WriteLine();
        
        // TEST 4: GetPermittedTriggers
        Console.WriteLine("--- TEST 4: Dozwolone przejścia ---");
        var permittedDoor = door.GetPermittedTriggers();
        Console.WriteLine($"Drzwi ({door.CurrentState}): [{string.Join(", ", permittedDoor)}]");
        
        var permittedVehicle = vehicle.GetPermittedTriggers();
        Console.WriteLine($"Pojazd ({vehicle.CurrentState}): [{string.Join(", ", permittedVehicle)}]");
        
        var permittedAccount = account.GetPermittedTriggers();
        Console.WriteLine($"Konto ({account.CurrentState}): [{string.Join(", ", permittedAccount)}]");
        
        Console.WriteLine();
        Console.WriteLine("=== TESTY ZAKOŃCZONE ===");
        Console.WriteLine("Jeśli nie ma warningów powyżej, pakiet działa poprawnie!");
    }
}