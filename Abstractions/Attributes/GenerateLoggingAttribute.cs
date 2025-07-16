//using System;

//namespace Abstractions.Attributes;

///// <summary>
///// Controls whether logging code should be generated for this state machine.
///// By default, logging is enabled only when FSM.NET.Logging package is installed.
///// </summary>
//[AttributeUsage(AttributeTargets.Class)]
//public sealed class GenerateLoggingAttribute(bool enabled) : Attribute
//{
//    /// <summary>
//    /// Whether to generate logging code for this state machine.
//    /// </summary>
//    public bool Enabled { get; } = enabled;
//}