[StateMachine(typeof(S), typeof(T))]
[OnException(nameof(HandleAsync))]
public partial class AsyncOnExitContinueMachine
{
    public enum S { Idle, Running }
    public enum T { Start }

    [State(S.Idle, OnExit = nameof(OnExitIdleAsync))]
    [Transition(S.Idle, T.Start, S.Running)]
    private void Configure() { }

    public static bool ThrowOnExit { get; set; } = true;

    private async System.Threading.Tasks.Task OnExitIdleAsync()
    {
        await System.Threading.Tasks.Task.Yield();
        if (ThrowOnExit) throw new System.InvalidOperationException("boom");
    }

    private async System.Threading.Tasks.ValueTask<FastFsm.Exceptions.ExceptionDirective> HandleAsync(
        FastFsm.Exceptions.ExceptionContext<S, T> ctx,
        System.Threading.CancellationToken ct)
    {
        await System.Threading.Tasks.Task.Yield();
        return FastFsm.Exceptions.ExceptionDirective.Continue;
    }
}
