namespace Generator.Rules.Contexts
{
    /// <summary>
    /// Represents the context for a rule detecting invalid history configuration on a state.
    /// </summary>
    public class InvalidHistoryConfigurationContext
    {
        /// <summary>
        /// Gets the name of the state with the invalid history configuration.
        /// </summary>
        public string StateName { get; }

        /// <summary>
        /// Gets the history mode value that is causing the issue.
        /// </summary>
        public int History { get; }

        /// <summary>
        /// Gets a value indicating whether the state is a composite state.
        /// </summary>
        public bool IsComposite { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidHistoryConfigurationContext"/> class.
        /// </summary>
        /// <param name="stateName">The name of the state with the invalid history configuration.</param>
        /// <param name="history">The history mode value.</param>
        /// <param name="isComposite">A flag indicating if the state is composite.</param>
        public InvalidHistoryConfigurationContext(string stateName, int history, bool isComposite)
        {
            StateName = stateName;
            History = history;
            IsComposite = isComposite;
        }
    }
}
