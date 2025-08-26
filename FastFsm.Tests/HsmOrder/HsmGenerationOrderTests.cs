using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace FastFsm.Tests.HsmOrder
{
    public partial class HsmGenerationOrderTests
    {
        private static string FindProjectRootByMarker(string startDir)
        {
            // Idź w górę dopóki nie znajdziesz FastFsm.Tests.csproj
            var dir = new DirectoryInfo(startDir);
            while (dir != null)
            {
                if (Directory.GetFiles(dir.FullName, "FastFsm.Tests.csproj", SearchOption.TopDirectoryOnly).Any())
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Could not locate FastFsm.Tests project root.");
        }

        private static string FindGeneratedFilePath(string className)
        {
            // Start z katalogu assembly testów (bin/…/FastFsm.Tests.dll)
            var asmDir = Path.GetDirectoryName(typeof(HsmGenerationOrderTests).Assembly.Location)!;
            // Wejdź w górę do katalogu projektu FastFsm.Tests
            var projectRoot = FindProjectRootByMarker(asmDir);

            // Tu MSBuild emituje pliki generatora
            var genRoot = Path.Combine(projectRoot, "obj", "GeneratedFiles");
            if (!Directory.Exists(genRoot))
                throw new InvalidOperationException(
                    $"GeneratedFiles not found under: {genRoot}. Upewnij się, że EmitCompilerGeneratedFiles=true oraz, " +
                    $"że build/test szedł na tym projekcie.");

            // Szukamy global__*.Generated.cs zawierających nazwę klasy
            var candidates = Directory.GetFiles(genRoot, "global__*.Generated.cs", SearchOption.AllDirectories)
                                      .Where(p => Path.GetFileName(p).IndexOf(className, StringComparison.OrdinalIgnoreCase) >= 0)
                                      .ToArray();

            if (candidates.Length == 0)
                throw new FileNotFoundException($"Generated file for {className} not found under {genRoot}");

            // Jeśli jest kilka — bierz najnowszy po mtime
            return candidates.OrderByDescending(File.GetLastWriteTimeUtc).First();
        }

        [Fact]
        public void GeneratedCode_AssignsCompositeIndexBeforeResolvingHistory()
        {
            var path = FindGeneratedFilePath("HsmOrderMachine");
            var text = File.ReadAllText(path);

            // Spodziewana kolejność:
            // 1) _currentState = (HState)destLeaf;
            // 2) int __compositeIndex = (int)_currentState;
            // 3) int __resolvedIndex = GetCompositeEntryTarget(__compositeIndex);
            // 4) _currentState = (HState)__resolvedIndex;

            int i1 = text.IndexOf("_currentState = (HState)destLeaf", StringComparison.Ordinal);
            int i2 = text.IndexOf("int __compositeIndex = (int)_currentState", StringComparison.Ordinal);
            int i3 = text.IndexOf("int __resolvedIndex = GetCompositeEntryTarget(__compositeIndex)", StringComparison.Ordinal);
            int i4 = text.IndexOf("_currentState = (HState)__resolvedIndex", StringComparison.Ordinal);

            Assert.True(i1 >= 0, "Missing assignment to destLeaf");
            Assert.True(i2 > i1, $"__compositeIndex must be captured after destLeaf assignment (i2:{i2} <= i1:{i1})");
            Assert.True(i3 > i2, $"__resolvedIndex must be computed from __compositeIndex (i3:{i3} <= i2:{i2})");
            Assert.True(i4 > i3, $"_currentState must be set to __resolvedIndex last (i4:{i4} <= i3:{i3})");

            // Opcjonalnie: upewnij się, że między i2 a i3 NIE ma innego przypisania do _currentState
            var between = text.Substring(i2, i3 - i2);
            Assert.DoesNotContain("_currentState =", between);
        }
    }
}
