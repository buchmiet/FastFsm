namespace Machines.Tests.Machines.Legacy;

[Abstractions.Attributes.StateMachine(typeof(InitialChildMachine_S), typeof(InitialChildMachine_T), EnableHierarchy = true)]
        public partial class InitialChildMachine
        {
            [State(InitialChildMachine_S.Parent)] private void Parent() { }
            [State(InitialChildMachine_S.Parent_A, Parent = InitialChildMachine_S.Parent, IsInitial = true)] private void ChildA() { }
            [State(InitialChildMachine_S.Parent_B, Parent = InitialChildMachine_S.Parent)] private void ChildB() { }

            [Transition(InitialChildMachine_S.Outside, InitialChildMachine_T.EnterParent, InitialChildMachine_S.Parent)]
            [Transition(InitialChildMachine_S.Parent_A, InitialChildMachine_T.Switch, InitialChildMachine_S.Parent_B)]
            [Transition(InitialChildMachine_S.Parent, InitialChildMachine_T.LeaveParent, InitialChildMachine_S.Outside)]
            private void Configure() { }
        }
