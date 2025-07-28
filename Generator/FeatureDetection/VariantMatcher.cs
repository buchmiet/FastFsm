using System.Collections.Generic;
using Generator.Model;

namespace Generator.FeatureDetection
{
    /// <summary>
    /// Sprawdza zgodność między wykrytymi cechami a wybranym wariantem generacji.
    /// </summary>
    public class VariantMatcher
    {
        public ValidationResult ValidateVariant(FeatureSet features, GenerationVariant variant, bool isForced)
        {
            var expectedFeatures = GetExpectedFeatures(variant);
            var result = new ValidationResult { IsValid = true };

            // Jeśli wariant jest wymuszony, tylko ostrzegamy, nie blokujemy
            if (isForced)
            {
                result.IsWarningOnly = true;
            }

            // Sprawdź wymagane cechy
            if (expectedFeatures.RequiresPayload && !features.HasPayload)
            {
                result.AddIssue("Variant requires payload support but no payload detected", isForced);
            }

            if (expectedFeatures.RequiresExtensions && !features.HasExtensions)
            {
                result.AddIssue("Variant requires extensions but none detected", isForced);
            }

            if (expectedFeatures.RequiresOnEntryExit && !features.HasOnEntryExit)
            {
                result.AddIssue("Variant requires OnEntry/OnExit but none detected", isForced);
            }

            // Sprawdź zabronione cechy
            if (!expectedFeatures.AllowsPayload && features.HasPayload)
            {
                result.AddIssue("Variant does not support payload but payload detected", isForced);
            }

            if (!expectedFeatures.AllowsExtensions && features.HasExtensions)
            {
                result.AddIssue("Variant does not support extensions but extensions detected", isForced);
            }

            if (!expectedFeatures.AllowsOnEntryExit && features.HasOnEntryExit)
            {
                result.AddIssue("Variant does not support OnEntry/OnExit but they are detected", isForced);
            }

            return result;
        }

        private VariantExpectations GetExpectedFeatures(GenerationVariant variant)
        {
            switch (variant)
            {
                case GenerationVariant.Pure:
                    return new VariantExpectations
                    {
                        AllowsOnEntryExit = false,
                        AllowsPayload = false,
                        AllowsExtensions = false
                    };

                case GenerationVariant.Basic:
                    return new VariantExpectations
                    {
                        RequiresOnEntryExit = true,
                        AllowsPayload = false,
                        AllowsExtensions = false
                    };

                case GenerationVariant.WithPayload:
                    return new VariantExpectations
                    {
                        RequiresPayload = true,
                        AllowsOnEntryExit = true,
                        AllowsExtensions = false
                    };

                case GenerationVariant.WithExtensions:
                    return new VariantExpectations
                    {
                        RequiresExtensions = true,
                        AllowsOnEntryExit = true,
                        AllowsPayload = false
                    };

                case GenerationVariant.Full:
                    return new VariantExpectations
                    {
                        RequiresPayload = true,
                        RequiresExtensions = true,
                        RequiresOnEntryExit = true
                    };

                default:
                    return new VariantExpectations();
            }
        }

        private class VariantExpectations
        {
            public bool RequiresOnEntryExit { get; set; }
            public bool RequiresPayload { get; set; }
            public bool RequiresExtensions { get; set; }

            public bool AllowsOnEntryExit { get; set; } = true;
            public bool AllowsPayload { get; set; } = true;
            public bool AllowsExtensions { get; set; } = true;
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public bool IsWarningOnly { get; set; } = false;
        public List<string> Errors { get; } = [];
        public List<string> Warnings { get; } = [];

        public void AddError(string error)
        {
            IsValid = false;
            Errors.Add(error);
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        public void AddIssue(string message, bool asWarning)
        {
            if (asWarning)
            {
                AddWarning(message);
            }
            else
            {
                AddError(message);
            }
        }

        public string GetErrorMessage()
        {
            return string.Join("\n", Errors);
        }

        public string GetWarningMessage()
        {
            return string.Join("\n", Warnings);
        }

        public bool HasIssues()
        {
            return Errors.Count > 0 || Warnings.Count > 0;
        }
    }
}
