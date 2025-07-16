using System;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Generator.Helpers;

internal static class BuildProperties
{
    public static bool Get(AnalyzerConfigOptions options,string propertyName)
    {
           
        bool buildProperty = false;
        if (options.TryGetValue($"build_property.{propertyName}", out var msValue))
        {
            buildProperty = msValue.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        return buildProperty;
    }
    public static bool GetGenerateLogging(AnalyzerConfigOptions options) =>
        Get(options, "FsmGenerateLogging");

    public static bool GetGenerateDI(AnalyzerConfigOptions options) =>
        Get(options, "FsmGenerateDI");

}