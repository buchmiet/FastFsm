using Generator.Infrastructure;
using Generator.Model;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Generator.Helpers;

/// <summary>
/// Analyzes callback method signatures to determine all available overloads
/// and their characteristics (async, parameters, return types).
/// </summary>
public sealed class CallbackSignatureAnalyzer
{
    private readonly TypeSystemHelper _typeHelper;
    private readonly AsyncSignatureAnalyzer _asyncAnalyzer;
    private readonly ConcurrentDictionary<string, CallbackSignatureInfo> _cache = new();

    // Known type names
    private const string CancellationTokenFullName = "System.Threading.CancellationToken";

    public CallbackSignatureAnalyzer(TypeSystemHelper typeHelper, AsyncSignatureAnalyzer asyncAnalyzer)
    {
        _typeHelper = typeHelper;
        _asyncAnalyzer = asyncAnalyzer;
    }

    /// <summary>
    /// Analyzes all overloads of a callback method and returns comprehensive signature information.
    /// </summary>
    /// <param name="typeSymbol">The type containing the callback methods</param>
    /// <param name="callbackName">The name of the callback method</param>
    /// <param name="callbackType">The type of callback (Guard, Action, OnEntry, OnExit)</param>
    /// <param name="compilation">The compilation context</param>
    /// <returns>Complete signature information including all overloads</returns>
    public CallbackSignatureInfo AnalyzeCallback(
        ITypeSymbol typeSymbol,
        string callbackName,
        string callbackType,
        Compilation compilation)
    {
        var cacheKey = $"{typeSymbol.ToDisplayString()}::{callbackName}::{callbackType}";

        return _cache.GetOrAdd(cacheKey, _ =>
        {
            var methods = typeSymbol.GetMembers(callbackName)
                .OfType<IMethodSymbol>()
                .Where(m => !m.IsStatic && m.DeclaredAccessibility != Accessibility.Public)
                .ToList();

            if (!methods.Any())
                return CallbackSignatureInfo.Empty;

            var info = new CallbackSignatureInfo();
            string? payloadType = null;

            foreach (var method in methods)
            {
                var asyncInfo = _asyncAnalyzer.AnalyzeCallback(method, callbackType, compilation);

                // Update async/return type info from any overload
                info.IsAsync = info.IsAsync || asyncInfo.IsAsync;
                info.IsVoidEquivalent = info.IsVoidEquivalent || asyncInfo.IsVoidEquivalent;
                info.IsBoolEquivalent = info.IsBoolEquivalent || asyncInfo.IsBoolEquivalent;

                // Analyze parameters
                var parameters = method.Parameters;

                if (parameters.Length == 0)
                {
                    info.HasParameterless = true;
                }
                else if (parameters.Length == 1)
                {
                    var paramType = parameters[0].Type;
                    var paramTypeName = _typeHelper.BuildFullTypeName(paramType);

                    if (paramTypeName == CancellationTokenFullName)
                    {
                        info.HasTokenOnly = true;
                    }
                    else
                    {
                        info.HasPayloadOnly = true;
                        payloadType = payloadType ?? paramTypeName;
                    }
                }
                else if (parameters.Length == 2)
                {
                    var firstParamType = _typeHelper.BuildFullTypeName(parameters[0].Type);
                    var secondParamType = _typeHelper.BuildFullTypeName(parameters[1].Type);

                    if (secondParamType == CancellationTokenFullName)
                    {
                        info.HasPayloadAndToken = true;
                        payloadType = payloadType ?? firstParamType;
                    }
                }
            }

            // Set the payload type if any payload overloads were found
            if (payloadType != null)
            {
                info.PayloadTypeFullName = payloadType;
            }

            // (no validation here – analyzer is descriptive only)
            return info;
        });
    }

    /// <summary>
    /// Analyzes a specific method overload for a transition or state callback.
    /// Used when we already know which specific overload to analyze.
    /// Also scans other overloads of the same method name to provide complete overload information.
    /// </summary>
    public CallbackSignatureInfo AnalyzeSpecificMethod(
        IMethodSymbol method,
        string callbackType,
        Compilation compilation)
    {
        var asyncInfo = _asyncAnalyzer.AnalyzeCallback(method, callbackType, compilation);

        var info = new CallbackSignatureInfo
        {
            IsAsync = asyncInfo.IsAsync,
            IsVoidEquivalent = asyncInfo.IsVoidEquivalent,
            IsBoolEquivalent = asyncInfo.IsBoolEquivalent
        };

        // Analyze parameters of the specific method
        var parameters = method.Parameters;

        if (parameters.Length == 0)
        {
            info.HasParameterless = true;
        }
        else if (parameters.Length == 1)
        {
            var paramType = parameters[0].Type;
            var paramTypeName = _typeHelper.BuildFullTypeName(paramType);

            if (paramTypeName == CancellationTokenFullName)
            {
                info.HasTokenOnly = true;
            }
            else
            {
                info.HasPayloadOnly = true;
                info.PayloadTypeFullName = paramTypeName;
            }
        }
        else if (parameters.Length == 2)
        {
            var firstParamType = _typeHelper.BuildFullTypeName(parameters[0].Type);
            var secondParamType = _typeHelper.BuildFullTypeName(parameters[1].Type);

            if (secondParamType == CancellationTokenFullName)
            {
                info.HasPayloadAndToken = true;
                info.PayloadTypeFullName = firstParamType;
            }
        }

        // Scan all other overloads of the same method name to get complete overload information
        var allOverloads = method.ContainingType
            .GetMembers(method.Name)
            .OfType<IMethodSymbol>()
            .Where(m => !m.IsStatic && m.DeclaredAccessibility != Accessibility.Public) // Same filter as AnalyzeCallback
            .ToList();

        string? payloadType = info.PayloadTypeFullName;

        foreach (var overload in allOverloads)
        {
            if (overload.Equals(method, SymbolEqualityComparer.Default))
                continue; // Skip the method we already analyzed

            var overloadParams = overload.Parameters;

            if (overloadParams.Length == 0)
            {
                info.HasParameterless = true;
            }
            else if (overloadParams.Length == 1)
            {
                var paramTypeName = _typeHelper.BuildFullTypeName(overloadParams[0].Type);

                if (paramTypeName == CancellationTokenFullName)
                {
                    info.HasTokenOnly = true;
                }
                else
                {
                    info.HasPayloadOnly = true;
                    payloadType ??= paramTypeName;
                }
            }
            else if (overloadParams.Length == 2)
            {
                var firstParamType = _typeHelper.BuildFullTypeName(overloadParams[0].Type);
                var secondParamType = _typeHelper.BuildFullTypeName(overloadParams[1].Type);

                if (secondParamType == CancellationTokenFullName)
                {
                    info.HasPayloadAndToken = true;
                    payloadType ??= firstParamType;
                }
            }
        }

        // Update payload type if we found any payload overloads
        if (payloadType != null)
        {
            info.PayloadTypeFullName = payloadType;
        }

        return info;
    }

   
}