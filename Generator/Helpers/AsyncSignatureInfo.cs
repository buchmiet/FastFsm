namespace Generator.Helpers;

/// <summary>
/// Zawiera wynik analizy sygnatury metody pod kątem jej asynchroniczności i poprawności.
/// </summary>
internal struct AsyncSignatureInfo
{
    /// <summary>
    /// Czy metoda jest asynchroniczna (zwraca Task/ValueTask).
    /// </summary>
    public bool IsAsync { get; set; }

    /// <summary>
    /// Czy sygnatura jest równoważna `void` (void, Task, ValueTask).
    /// Używane dla Action, OnEntry, OnExit.
    /// </summary>
    public bool IsVoidEquivalent { get; set; }

    /// <summary>
    /// Czy sygnatura jest równoważna `bool` (bool, ValueTask&lt;bool&gt;).
    /// Używane dla Guard.
    /// </summary>
    public bool IsBoolEquivalent { get; set; }

    /// <summary>
    /// Czy wykryto niepoprawną sygnaturę `async void`.
    /// </summary>
    public bool IsInvalidAsyncVoid { get; set; }

    /// <summary>
    /// Czy wykryto niepoprawną sygnaturę `Task&lt;bool&gt;` dla guarda.
    /// </summary>
    public bool IsInvalidGuardTask { get; set; }
}
