namespace Example.Tests.Unreachable;

public enum UnreachableState
{
    Start,
    Connected,
    Isolated // intentionally unreachable
}

public enum UnreachableTrigger
{
    Connect,
    Isolate
}
