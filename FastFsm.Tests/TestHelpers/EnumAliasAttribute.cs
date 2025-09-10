using System;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Attribute to specify alternative names for enum values when mapping between Fluent and Legacy APIs
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class EnumAliasAttribute : Attribute
    {
        public string Alias { get; }
        public string? TargetApi { get; }

        /// <summary>
        /// Creates an enum alias for mapping
        /// </summary>
        /// <param name="alias">Alternative name for this enum value</param>
        /// <param name="targetApi">Optional: "Fluent" or "Legacy" to specify which API this alias is for</param>
        public EnumAliasAttribute(string alias, string? targetApi = null)
        {
            Alias = alias ?? throw new ArgumentNullException(nameof(alias));
            TargetApi = targetApi;
        }
    }
}