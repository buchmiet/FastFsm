//// FileLogger.cs

//using System.Runtime.CompilerServices;

//namespace Experiments
//{
//    public static class FileLogger
//    {
//        private static readonly object _lock = new object();
//        private static readonly string _logPath = "log.txt";
//        private static int _callDepth = 0;

//        static FileLogger()
//        {
//            // Wyczyść plik na start
//            File.WriteAllText(_logPath, $"=== Log started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===\n");
//        }

//        public static void Log(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
//        {
//            lock (_lock)
//            {
//                var indent = new string(' ', _callDepth * 2);
//                var logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] {indent}{caller}:{line} - {message}\n";
//                File.AppendAllText(_logPath, logEntry);
//            }
//        }

//        public static void EnterMethod([CallerMemberName] string methodName = "")
//        {
//            Log($"ENTER {methodName}");
//            lock (_lock) { _callDepth++; }
//        }

//        public static void ExitMethod([CallerMemberName] string methodName = "")
//        {
//            lock (_lock) { _callDepth = Math.Max(0, _callDepth - 1); }
//            Log($"EXIT {methodName}");
//        }
//    }
//}