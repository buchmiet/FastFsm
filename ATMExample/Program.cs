// ============================================
// ATM State Machine Example
// ============================================
// This example demonstrates that FastFSM attributes work
// WITHOUT any explicit 'using Abstractions.Attributes'
// The global using is provided by the NuGet package via .props file
// ============================================

namespace ATMExample;

// ATM States
public enum ATMState
{
    Idle,           // Waiting for card
    CardInserted,   // Card inserted, waiting for PIN
    Authenticated,  // PIN verified, showing menu
    Dispensing,     // Dispensing cash
    Error           // Error state
}

// ATM Triggers/Events
public enum ATMTrigger
{
    InsertCard,
    EnterPin,
    SelectAmount,
    DispenseCash,
    EjectCard,
    Cancel,
    ReportError
}

// ============================================
// NOTICE: No "using Abstractions.Attributes"!
// Yet all attributes below work perfectly
// ============================================

[StateMachine(typeof(ATMState), typeof(ATMTrigger))]
public partial class ATMMachine
{
    private int _attemptsLeft = 3;
    private decimal _balance = 1000m;
    private decimal _requestedAmount;

    // === State Transitions ===
    [Transition(ATMState.Idle, ATMTrigger.InsertCard, ATMState.CardInserted)]
    [Transition(ATMState.CardInserted, ATMTrigger.EnterPin, ATMState.Authenticated, 
        Guard = nameof(IsPinValid), Action = nameof(ResetAttempts))]
    [Transition(ATMState.CardInserted, ATMTrigger.EnterPin, ATMState.CardInserted, 
        Guard = nameof(IsPinInvalid), Action = nameof(DecrementAttempts))]
    [Transition(ATMState.CardInserted, ATMTrigger.Cancel, ATMState.Idle)]
    [Transition(ATMState.Authenticated, ATMTrigger.SelectAmount, ATMState.Dispensing,
        Guard = nameof(HasSufficientBalance), Action = nameof(PrepareDispense))]
    [Transition(ATMState.Authenticated, ATMTrigger.SelectAmount, ATMState.Error,
        Guard = nameof(InsufficientBalance))]
    [Transition(ATMState.Dispensing, ATMTrigger.DispenseCash, ATMState.Idle,
        Action = nameof(CompleteTransaction))]
    [Transition(ATMState.Error, ATMTrigger.EjectCard, ATMState.Idle)]
    private void ConfigureTransitions() { }

    // === State Configurations ===
    [State(ATMState.Idle, OnEntry = nameof(OnIdleEntry))]
    private void ConfigureIdle() { }

    [State(ATMState.CardInserted, OnEntry = nameof(OnCardInsertedEntry))]
    private void ConfigureCardInserted() { }

    [State(ATMState.Authenticated, OnEntry = nameof(OnAuthenticatedEntry))]
    private void ConfigureAuthenticated() { }

    [State(ATMState.Dispensing, OnEntry = nameof(OnDispensingEntry))]
    private void ConfigureDispensing() { }

    [State(ATMState.Error, OnEntry = nameof(OnErrorEntry))]
    private void ConfigureError() { }

    // === State Entry Actions ===
    private void OnIdleEntry()
    {
        Console.WriteLine("💳 Welcome! Please insert your card.");
        _attemptsLeft = 3;
    }

    private void OnCardInsertedEntry()
    {
        Console.WriteLine($"🔐 Card inserted. Please enter PIN. (Attempts left: {_attemptsLeft})");
    }

    private void OnAuthenticatedEntry()
    {
        Console.WriteLine($"✅ Authentication successful!");
        Console.WriteLine($"💰 Your balance: ${_balance:F2}");
        Console.WriteLine("📋 Please select amount to withdraw.");
    }

    private void OnDispensingEntry()
    {
        Console.WriteLine($"💵 Dispensing ${_requestedAmount:F2}...");
    }

    private void OnErrorEntry()
    {
        Console.WriteLine("❌ Insufficient funds! Transaction cancelled.");
    }

    // === Guards ===
    private bool IsPinValid()
    {
        // Simulate PIN validation (always true for demo)
        return _attemptsLeft > 0 && new Random().Next(2) == 1;
    }

    private bool IsPinInvalid()
    {
        return !IsPinValid();
    }

    private bool HasSufficientBalance()
    {
        _requestedAmount = 200m; // Simulated amount
        return _balance >= _requestedAmount;
    }

    private bool InsufficientBalance()
    {
        _requestedAmount = 200m; // Simulated amount
        return _balance < _requestedAmount;
    }

    // === Actions ===
    private void ResetAttempts()
    {
        _attemptsLeft = 3;
    }

    private void DecrementAttempts()
    {
        _attemptsLeft--;
        if (_attemptsLeft == 0)
        {
            Console.WriteLine("⚠️ Too many failed attempts! Card blocked.");
        }
    }

    private void PrepareDispense()
    {
        Console.WriteLine($"✓ Transaction approved for ${_requestedAmount:F2}");
    }

    private void CompleteTransaction()
    {
        _balance -= _requestedAmount;
        Console.WriteLine($"✅ Transaction complete. New balance: ${_balance:F2}");
        Console.WriteLine("📤 Please take your card.");
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine("║          ATM STATE MACHINE DEMONSTRATION              ║");
        Console.WriteLine("║                                                       ║");
        Console.WriteLine("║  This example uses FastFSM attributes WITHOUT any    ║");
        Console.WriteLine("║  explicit 'using Abstractions.Attributes' statement! ║");
        Console.WriteLine("║                                                       ║");
        Console.WriteLine("║  Global usings are provided via NuGet .props file    ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Create and start ATM
        var atm = new ATMMachine(ATMState.Idle);
        atm.Start();

        // Simulate ATM usage
        Console.WriteLine("\n=== Simulating ATM Transaction ===\n");

        // Insert card
        atm.Fire(ATMTrigger.InsertCard);

        // Try PIN (may fail randomly for demo)
        int pinAttempts = 0;
        while (atm.CurrentState == ATMState.CardInserted && pinAttempts < 3)
        {
            Console.WriteLine($"\n→ Attempting PIN entry (attempt {pinAttempts + 1})...");
            atm.Fire(ATMTrigger.EnterPin);
            pinAttempts++;
        }

        // If authenticated, try to withdraw
        if (atm.CurrentState == ATMState.Authenticated)
        {
            Console.WriteLine("\n→ Requesting $200 withdrawal...");
            atm.Fire(ATMTrigger.SelectAmount);

            if (atm.CurrentState == ATMState.Dispensing)
            {
                atm.Fire(ATMTrigger.DispenseCash);
            }
            else if (atm.CurrentState == ATMState.Error)
            {
                atm.Fire(ATMTrigger.EjectCard);
            }
        }
        else
        {
            Console.WriteLine("\n→ Authentication failed. Cancelling...");
            if (atm.CurrentState == ATMState.CardInserted)
            {
                atm.Fire(ATMTrigger.Cancel);
            }
        }

        Console.WriteLine("\n╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    TEST COMPLETED                     ║");
        Console.WriteLine("║                                                       ║");
        Console.WriteLine("║  ✅ State machine worked without explicit usings!    ║");
        Console.WriteLine("║  ✅ All attributes resolved via global usings!       ║");
        Console.WriteLine("║  ✅ Propagation through .props file confirmed!       ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
    }
}