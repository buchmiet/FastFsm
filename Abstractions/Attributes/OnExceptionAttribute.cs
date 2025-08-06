using System;

namespace Abstractions.Attributes;

/// <summary>
/// Specifies a method to handle exceptions that occur during state transitions.
/// The method must accept ExceptionContext and return ExceptionDirective.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OnExceptionAttribute : Attribute
{
    /// <summary>
    /// The name of the method that handles exceptions.
    /// </summary>
    public string MethodName { get; }
    
    /// <summary>
    /// Initializes a new instance of the OnExceptionAttribute class.
    /// </summary>
    /// <param name="methodName">The name of the exception handling method.</param>
    public OnExceptionAttribute(string methodName)
    {
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
    }
}