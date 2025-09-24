namespace FastFsm.Tests.Features.EdgeCases;

public enum EmptyState { Only }
public enum EmptyTrigger { Trigger }

public enum SingleState { Only }
public enum SingleTrigger { Loop }

public enum UnreachableState { Start, Connected, Isolated }
public enum UnreachableTrigger { Connect, Disconnect, Isolate }

public enum InternalOnlyState { Static }
public enum InternalOnlyTrigger { Action }
