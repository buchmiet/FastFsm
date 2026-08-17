using System.Collections.Generic;

namespace Generator.Rules.Contexts
{
    /// <summary>
    /// Represents the context for a circular hierarchy validation rule.
    /// </summary>
    public class CircularHierarchyContext
    {
        /// <summary>
        /// Gets the name of the state that is part of the circular dependency.
        /// </summary>
        public string StateName { get; }

        /// <summary>
        /// Gets the list of state names forming the circular path.
        /// </summary>
        public List<string> CyclePath { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircularHierarchyContext"/> class.
        /// </summary>
        /// <param name="stateName">The name of the state that is part of the circular dependency.</param>
        /// <param name="cyclePath">The list of state names forming the circular path.</param>
        public CircularHierarchyContext(string stateName, List<string> cyclePath)
        {
            StateName = stateName;
            CyclePath = cyclePath ?? new List<string>();
        }
    }
}
