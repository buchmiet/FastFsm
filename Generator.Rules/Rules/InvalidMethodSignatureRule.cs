using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

/// <summary>
/// Validates method signature for FSM callbacks.
/// </summary>
public class InvalidMethodSignatureRule : IValidationRule<MethodSignatureValidationContext>
{
    private const string ParameterlessSignatureFormat = "{0} {1}()";
    private const string ParameterSignatureFormat = "{0} {1}({2} payload)";
    private const string BothSignaturesFormat = "{0} {1}() or {1}({2} payload)";
    private const string UnknownType = "unknown";
    private const string NotFoundSuffix = " (method not found)";
    private const string MustBeParameterlessSuffix = " (must be parameterless)";
    private const string FoundParameterTypeSuffix = " (found parameter type: {0})";
    private const string FoundParameterCountSuffix = " (found {0} parameters)";

    public IEnumerable<ValidationResult> Validate(MethodSignatureValidationContext context)
    {
        // Method not found
        if (!context.MethodFound)
        {
            string expectedSignature = BuildExpectedSignature(context) + NotFoundSuffix;
            string message = string.Format(
                DefinedRules.InvalidMethodSignature.MessageFormat,
                context.MethodName,
                context.CallbackType,
                expectedSignature
            );
            yield return ValidationResult.Fail(
                RuleIdentifiers.InvalidMethodSignature,
                message,
                DefinedRules.InvalidMethodSignature.DefaultSeverity
            );
            yield break;
        }

        // Too many parameters for parameterless expected
        if (!context.ParametersAllowed && context.ActualParameterCount > 0)
        {
            string expectedSignature = string.Format(ParameterlessSignatureFormat, context.ExpectedReturnType.ToLowerInvariant(), context.MethodName) + MustBeParameterlessSuffix;
            string message = string.Format(
                DefinedRules.InvalidMethodSignature.MessageFormat,
                context.MethodName,
                context.CallbackType,
                expectedSignature
            );
            yield return ValidationResult.Fail(
                RuleIdentifiers.InvalidMethodSignature,
                message,
                DefinedRules.InvalidMethodSignature.DefaultSeverity
            );
            yield break;
        }

        // Validate parameter type for payload callbacks
        if (context.ParametersAllowed && context.ExpectedParameterType != null)
        {
            if (context.ActualParameterCount == 0)
            {
                // Parameterless method is OK for backward compatibility
            }
            else if (context.ActualParameterCount == 1)
            {
                if (context.ActualParameterType != context.ExpectedParameterType)
                {
                    string expectedSignature = string.Format(ParameterSignatureFormat,
                        context.ExpectedReturnType.ToLowerInvariant(),
                        context.MethodName,
                        GetSimpleTypeName(context.ExpectedParameterType))
                        + string.Format(FoundParameterTypeSuffix, GetSimpleTypeName(context.ActualParameterType));
                    string message = string.Format(
                        DefinedRules.InvalidMethodSignature.MessageFormat,
                        context.MethodName,
                        context.CallbackType,
                        expectedSignature
                    );
                    yield return ValidationResult.Fail(
                        RuleIdentifiers.InvalidMethodSignature,
                        message,
                        DefinedRules.InvalidMethodSignature.DefaultSeverity
                    );
                    yield break;
                }
            }
            else
            {
                string expectedSignature = string.Format(BothSignaturesFormat,
                        context.ExpectedReturnType.ToLowerInvariant(),
                        context.MethodName,
                        GetSimpleTypeName(context.ExpectedParameterType))
                    + string.Format(FoundParameterCountSuffix, context.ActualParameterCount);
                string message = string.Format(
                    DefinedRules.InvalidMethodSignature.MessageFormat,
                    context.MethodName,
                    context.CallbackType,
                    expectedSignature
                );
                yield return ValidationResult.Fail(
                    RuleIdentifiers.InvalidMethodSignature,
                    message,
                    DefinedRules.InvalidMethodSignature.DefaultSeverity
                );
                yield break;
            }
        }

        // Check return type
        if (context.ActualReturnType?.ToLowerInvariant() != context.ExpectedReturnType.ToLowerInvariant())
        {
            string expectedSignature = BuildExpectedSignature(context);
            string message = string.Format(
                DefinedRules.InvalidMethodSignature.MessageFormat,
                context.MethodName,
                context.CallbackType,
                expectedSignature
            );
            yield return ValidationResult.Fail(
                RuleIdentifiers.InvalidMethodSignature,
                message,
                DefinedRules.InvalidMethodSignature.DefaultSeverity
            );
            yield break;
        }

        yield return ValidationResult.Success();
    }

    private string BuildExpectedSignature(MethodSignatureValidationContext context)
    {
        var returnType = context.ExpectedReturnType.ToLowerInvariant();
        var methodName = context.MethodName;

        if (!context.ParametersAllowed)
        {
            return string.Format(ParameterlessSignatureFormat, returnType, methodName);
        }
        else if (context.ExpectedParameterType != null)
        {
            var paramType = GetSimpleTypeName(context.ExpectedParameterType);
            return string.Format(BothSignaturesFormat, returnType, methodName, paramType);
        }
        else
        {
            return $"{returnType} {methodName}(...)";
        }
    }

    private string GetSimpleTypeName(string? fullyQualifiedName)
    {
        if (string.IsNullOrEmpty(fullyQualifiedName))
            return UnknownType;

        int lastDot = fullyQualifiedName.LastIndexOf('.');
        return lastDot >= 0 ? fullyQualifiedName.Substring(lastDot + 1) : fullyQualifiedName;
    }
}
