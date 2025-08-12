namespace Generator.Rules.Contexts
{
    /// <summary>
    /// Represents the context for a rule detecting an orphan substate that references a non-existent parent.
    /// </summary>
    public class OrphanSubstateContext
    {
        /// <summary>
        /// Gets the name of the orphan substate.
        /// </summary>
        public string StateName { get; }

        /// <summary>
        /// Gets the name of the non-existent parent state.
        /// </summary>
        public string ParentStateName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrphanSubstateContext"/> class.
        /// </summary>
        /// <param name="stateName">The name of the orphan substate.</param>
        /// <param name="parentStateName">The name of the non-existent parent state.</param>
        public OrphanSubstateContext(string stateName, string parentStateName)
        {
            StateName = stateName;
            ParentStateName = parentStateName;
        }
    }
}
