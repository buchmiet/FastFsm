namespace FastFsm.Observability;

public interface IObservabilityEventSink
{
    void OnEvent(in ObservabilityEvent evt);
}
