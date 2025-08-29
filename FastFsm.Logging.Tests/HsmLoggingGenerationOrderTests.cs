using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FastFsm.Logging.Tests
{
    public class HsmLoggingGenerationOrderTests
    {
        private static string FindProjectRootByMarker(string startDir)
        {
            var dir = new DirectoryInfo(startDir);
            while (dir != null)
            {
                if (Directory.GetFiles(dir.FullName, "FastFsm.Logging.Tests.csproj", SearchOption.TopDirectoryOnly).Any())
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Could not locate FastFsm.Logging.Tests project root.");
        }

        private static string FindGeneratedFilePath(string className)
        {
            var asmDir = Path.GetDirectoryName(typeof(HsmLoggingGenerationOrderTests).Assembly.Location)!;
            var projectRoot = FindProjectRootByMarker(asmDir);
            var genRoot = Path.Combine(projectRoot, "obj", "GeneratedFiles");
            if (!Directory.Exists(genRoot))
                throw new InvalidOperationException($"GeneratedFiles not found under: {genRoot}. Upewnij się, że EmitCompilerGeneratedFiles=true.");

            var candidates = Directory.GetFiles(genRoot, "global__*.Generated.cs", SearchOption.AllDirectories)
                                      .Where(p => Path.GetFileName(p).IndexOf(className, StringComparison.OrdinalIgnoreCase) >= 0)
                                      .ToArray();
            if (candidates.Length == 0)
                throw new FileNotFoundException($"Generated file for {className} not found under {genRoot}");

            return candidates.OrderByDescending(File.GetLastWriteTimeUtc).First();
        }

    }
}
