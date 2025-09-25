namespace Machines.Tests.Features.Core;

public enum CallbackState { A, B, C }
public enum CallbackTrigger { Next }

public enum InitialState { Start, Next }
public enum InitialTrigger { Go }

public enum InternalState { Active, Inactive }
public enum InternalTrigger { Update, Deactivate }

public enum GuardedState { A, B }
public enum GuardedTrigger { Go }

public enum SelfState { Active }
public enum SelfTrigger { Refresh }

public enum ExceptionState { A, B }
public enum ExceptionTrigger { Go }

public enum ComplexCallbackState { Idle, Ready, Processing, Done }
public enum ComplexCallbackTrigger { Start, Process, Complete }

public enum MultiState { A, B }
public enum MultiTrigger { Go }
