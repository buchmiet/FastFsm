Console.WriteLine($"AppContext.BaseDirectory: {AppContext.BaseDirectory}");
var searchPath = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "Abstractions", "bin", "Release", "netstandard2.0", "Abstractions.dll");
Console.WriteLine($"Search path: {searchPath}");
Console.WriteLine($"Exists: {System.IO.File.Exists(searchPath)}");
