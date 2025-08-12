using System.Collections.Generic;

namespace Generator.Rules.Contexts
{
    /// <summary>
    /// Represents the context for a rule detecting multiple initial substates within a composite state.
    /// </summary>
    public class MultipleInitialSubstatesContext
    {
        /// <summary>
        /// Gets the name of the parent composite state.
        /// </summary>
        public string ParentStateName { get; }

        /// <summary>
        /// Gets the list of states incorrectly marked as initial substates.
        /// </summary>
        public List<string> InitialSubstates { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleInitialSubstatesContext"/> class.
        /// </summary>
        /// <param name="parentStateName">The name of the parent composite state.</param>
        /// <param name="initialSubstates">The list of states incorrectly marked as initial substates.</param>
        public MultipleInitialSubstatesContext(string parentStateName, List<string> initialSubstates)
        {
            ParentStateName = parentStateName;
            InitialSubstates = initialSubstates ?? new List<string>();
        }
    }
}
