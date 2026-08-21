using System;

namespace FastFsm.Contracts;

[Flags]
public enum ExtensionHooks
{
    None = 0,
    Transitions = 1 << 0,
    Guards = 1 << 1,
    States = 1 << 2,
    Callbacks = 1 << 3,
    Hierarchy = 1 << 4,
    Lifecycle = 1 << 5,
    All = Transitions | Guards | States | Callbacks | Hierarchy | Lifecycle
}