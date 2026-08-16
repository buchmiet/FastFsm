using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ParserComparison.Tests
{
    public static class ModelExtractor
    {
        public static void ExtractModels()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };

            // This would require access to the generator internals
            // For now, we'll document the issue based on the generated code
            
            Console.WriteLine("=== MODEL EXTRACTION ===");
            Console.WriteLine("Legacy generates 1 transition for S.A + T.Go");
            Console.WriteLine("Fluent generates 2 transitions for S.A + T.Go");
            Console.WriteLine("This causes duplicate case statements in switch");
        }
    }
}