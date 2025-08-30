using System;
using System.Reflection;
using Microsoft.CodeAnalysis;

var genPath = "/mnt/c/Users/newon/source/repos/FastFsm/Generator/bin/Release/netstandard2.0/Generator.dll";
var asm = Assembly.LoadFrom(genPath);

var incType = typeof(IIncrementalGenerator);
var genType = asm.GetType("Generator.StateMachineGenerator");

Console.WriteLine($"Generator type: {genType}");
Console.WriteLine($"Implements IIncrementalGenerator: {incType.IsAssignableFrom(genType)}");

var interfaces = genType.GetInterfaces();
foreach (var i in interfaces)
{
    Console.WriteLine($"  Interface: {i.FullName} (Assembly: {i.Assembly.GetName().Name} v{i.Assembly.GetName().Version})");
}

Console.WriteLine($"\nExpected IIncrementalGenerator from: {incType.Assembly.GetName().Name} v{incType.Assembly.GetName().Version}");
