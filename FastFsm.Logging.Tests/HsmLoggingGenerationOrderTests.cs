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

        [Fact]
        public void GeneratedCode_LogsCompositeEntry_UsingCompositeIndex_BeforeStateIsResolved()
        {
            // Weryfikujemy porządek oraz źródła parametrów w wersji z logowaniem.
            var path = FindGeneratedFilePath("HsmMachine");
            var text = File.ReadAllText(path);

            // Oczekiwany porządek (logging path):
            // 1) int __compositeIndex = bestDestIndex;           // (lub równoważne, byle COMPOSITE)
            //    _currentState = (HState)bestDestIndex;          // akceptowalne, jeśli jest po wyliczeniu compositeIndex
            // 2) int __resolvedIndex = GetCompositeEntryTarget(__compositeIndex);
            // 3) HsmMachineLog.CompositeStateEntry(... ((HState)__compositeIndex).ToString(), ((HState)__resolvedIndex).ToString(), ...)
            // 4) _currentState = (HState)__resolvedIndex;

            // Lokalizacje kluczowych fragmentów:
            // Dopuszczamy dwie strategie: albo compositeIndex = bestDestIndex, albo compositeIndex = (int)_currentState tuż po przypisaniu destLeaf.
            int idxDestAssign1 = text.IndexOf("_currentState = (HState)destLeaf", StringComparison.Ordinal);
            int idxDestAssign2 = text.IndexOf("_currentState = (HState)bestDestIndex", StringComparison.Ordinal);
            int idxCompFromBest = text.IndexOf("int __compositeIndex = bestDestIndex", StringComparison.Ordinal);
            int idxCompFromCur = text.IndexOf("int __compositeIndex = (int)_currentState", StringComparison.Ordinal);
            int idxResolved = text.IndexOf("int __resolvedIndex = GetCompositeEntryTarget(__compositeIndex)", StringComparison.Ordinal);
            int idxLogComposite = text.IndexOf("CompositeStateEntry(", StringComparison.Ordinal);
            int idxSetResolved = text.IndexOf("_currentState = (HState)__resolvedIndex", StringComparison.Ordinal);

            Assert.True(idxResolved > 0 && idxLogComposite > 0 && idxSetResolved > 0, "Missing key HSM logging segments.");

            // Jeśli używamy compositeIndex z bestDestIndex:
            if (idxCompFromBest >= 0)
            {
                Assert.True(idxCompFromBest >= 0, "Expected __compositeIndex from bestDestIndex.");
                Assert.True(idxResolved > idxCompFromBest, "resolvedIndex must be computed AFTER compositeIndex.");
                Assert.True(idxLogComposite > idxResolved, "CompositeStateEntry must occur AFTER resolvedIndex.");
                Assert.True(idxSetResolved > idxLogComposite, "Setting _currentState to resolved must be LAST.");
            }
            else
            {
                // fallback: compositeIndex z (int)_currentState – musi być PO przypisaniu destLeaf, a PRZED resolved
                int idxDest = Math.Max(idxDestAssign1, idxDestAssign2);
                Assert.True(idxDest >= 0, "Expected assignment of _currentState to destination leaf first.");
                Assert.True(idxCompFromCur > idxDest, "__compositeIndex must be captured AFTER destLeaf assignment.");
                Assert.True(idxResolved > idxCompFromCur, "resolvedIndex must be computed AFTER compositeIndex.");
                Assert.True(idxLogComposite > idxResolved, "CompositeStateEntry must occur AFTER resolvedIndex.");
                Assert.True(idxSetResolved > idxLogComposite, "Setting _currentState to resolved must be LAST.");
            }

            // Dodatkowo, upewnij się, że wywołanie loga korzysta z __compositeIndex, nie z __resolvedIndex, na pierwszym parametrze stanu złożonego.
            var callStart = text.IndexOf("CompositeStateEntry(", StringComparison.Ordinal);
            var callEnd = callStart >= 0 ? text.IndexOf(");", callStart, StringComparison.Ordinal) : -1;
            Assert.True(callStart >= 0 && callEnd > callStart, "Could not locate CompositeStateEntry(...) call.");
            var callArgs = text.Substring(callStart, callEnd - callStart);

            Assert.Contains("((HState)__compositeIndex).ToString()", callArgs);
            Assert.Contains("((HState)__resolvedIndex).ToString()", callArgs);
        }
    }
}
