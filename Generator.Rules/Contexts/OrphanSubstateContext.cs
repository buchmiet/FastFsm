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
        public string SubstateName { get; }

        /// <summary>
        /// Gets the name of the non-existent parent state.
        /// </summary>
        public string ParentStateName { get; }

        /// <summary>
        /// Gets a value indicating whether the parent exists.
        /// </summary>
        public bool ParentExists { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrphanSubstateContext"/> class.
        /// </summary>
        /// <param name="substateName">The name of the orphan substate.</param>
        /// <param name="parentStateName">The name of the non-existent parent state.</param>
        /// <param name="parentExists">Indicates whether the parent exists.</param>
        public OrphanSubstateContext(string substateName, string parentStateName, bool parentExists)
        {
            SubstateName = substateName;
            ParentStateName = parentStateName;
            ParentExists = parentExists;
        }
    }
}
