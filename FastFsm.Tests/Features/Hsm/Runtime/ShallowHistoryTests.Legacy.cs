using Xunit;
using Abstractions.Attributes;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    public partial class ShallowHistoryTestsLegacy
    {
        [Fact]
        public void Reentering_Parent_With_ShallowHistory_Restores_LastChildLegacy()
        {
            var m = new ShallowHistoryMachineLegacy(ShallowHistoryTestsFluent.S.Outside);
            m.Start();

            // Enter parent → initial child
            m.Fire(ShallowHistoryTestsFluent.T.Enter);
            Assert.Equal(ShallowHistoryTestsFluent.S.Menu_Main, m.CurrentState);

            // Move to another child
            m.Fire(ShallowHistoryTestsFluent.T.Next);
            Assert.Equal(ShallowHistoryTestsFluent.S.Menu_Settings, m.CurrentState);

            // Exit composite
            m.Fire(ShallowHistoryTestsFluent.T.Exit);
            Assert.Equal(ShallowHistoryTestsFluent.S.Outside, m.CurrentState);

            // Re-enter → shallow history brings us back to Settings
            m.Fire(ShallowHistoryTestsFluent.T.Enter);
            Assert.Equal(ShallowHistoryTestsFluent.S.Menu_Settings, m.CurrentState);
        }

        [StateMachine(typeof(ShallowHistoryTestsFluent.S), typeof(ShallowHistoryTestsFluent.T), EnableHierarchy = true)]
        public partial class ShallowHistoryMachineLegacy
        {
            // Define parent state with shallow history
            [State(ShallowHistoryTestsFluent.S.Menu, History = HistoryMode.Shallow)]
            private void ConfigureMenu() { }

            // Define child states
            [State(ShallowHistoryTestsFluent.S.Menu_Main, Parent = ShallowHistoryTestsFluent.S.Menu, IsInitial = true)]
            private void ConfigureMenuMain() { }

            [State(ShallowHistoryTestsFluent.S.Menu_Settings, Parent = ShallowHistoryTestsFluent.S.Menu)]
            private void ConfigureMenuSettings() { }

            // Define outside state
            [State(ShallowHistoryTestsFluent.S.Outside)]
            private void ConfigureOutside() { }

            // Transitions
            [Transition(ShallowHistoryTestsFluent.S.Outside, ShallowHistoryTestsFluent.T.Enter, ShallowHistoryTestsFluent.S.Menu)]
            private void ConfigureOutsideToMenu() { }

            [Transition(ShallowHistoryTestsFluent.S.Menu_Main, ShallowHistoryTestsFluent.T.Next, ShallowHistoryTestsFluent.S.Menu_Settings)]
            private void ConfigureMenuMainToSettings() { }

            [Transition(ShallowHistoryTestsFluent.S.Menu_Settings, ShallowHistoryTestsFluent.T.Back, ShallowHistoryTestsFluent.S.Menu_Main)]
            private void ConfigureMenuSettingsToMain() { }

            [Transition(ShallowHistoryTestsFluent.S.Menu, ShallowHistoryTestsFluent.T.Exit, ShallowHistoryTestsFluent.S.Outside)]
            private void ConfigureMenuToOutside() { }
        }
    }
}