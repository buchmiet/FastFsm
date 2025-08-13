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
        /// Gets a value indicating whether the state has history.
        /// </summary>
        public bool HasHistory { get; }

        /// <summary>
        /// Gets the history mode as a string.
        /// </summary>
        public string HistoryMode { get; }

        /// <summary>
        /// Gets a value indicating whether the state is a composite state.
        /// </summary>
        public bool IsComposite { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidHistoryConfigurationContext"/> class.
        /// </summary>
        /// <param name="stateName">The name of the state with the invalid history configuration.</param>
        /// <param name="hasHistory">Indicates whether the state has history.</param>
        /// <param name="historyMode">The history mode as a string.</param>
        /// <param name="isComposite">A flag indicating if the state is composite.</param>
        public InvalidHistoryConfigurationContext(string stateName, bool hasHistory, string historyMode, bool isComposite)
        {
            StateName = stateName;
            HasHistory = hasHistory;
            HistoryMode = historyMode;
            IsComposite = isComposite;
        }
    }
}
