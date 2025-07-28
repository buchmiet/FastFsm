
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Generator.Rules.Definitions;
using Microsoft.CodeAnalysis;

namespace Generator.Helpers; 

public static class DiagnosticFactory
{
   
    private static readonly ConcurrentDictionary<string, DiagnosticDescriptor> DescriptorCache = new();

    private static DiagnosticSeverity ToRoslynSeverity(RuleSeverity ruleSeverity)
    {
        return ruleSeverity switch
        {
            RuleSeverity.Error => DiagnosticSeverity.Error,
            RuleSeverity.Warning => DiagnosticSeverity.Warning,
            RuleSeverity.Info => DiagnosticSeverity.Info,
            _ => DiagnosticSeverity.Warning // Domyślna wartość
        };
    }

    /// <summary>
    /// Pobiera lub tworzy DiagnosticDescriptor na podstawie RuleId.
    /// Deskryptor jest cache'owany.
    /// </summary>
    /// <param name="ruleId">Identyfikator reguły (np. "FSM001").</param>
    /// <returns>Odpowiedni DiagnosticDescriptor.</returns>
    /// <exception cref="KeyNotFoundException">Jeśli reguła o danym ID nie jest zdefiniowana w DefinedRules.</exception>
    public static DiagnosticDescriptor Get(string ruleId)
    {
        return DescriptorCache.GetOrAdd(ruleId, id =>
        {
            // Znajdź RuleDefinition w StateMachine.Rules.Definitions.DefinedRules
            RuleDefinition? ruleDefinition = DefinedRules.All.FirstOrDefault(rd => rd.Id == id);

            if (ruleDefinition == null)
            {
                throw new KeyNotFoundException($"RuleDefinition for ID '{id}' not found in DefinedRules.All. Ensure it is defined in StateMachine.Rules.");
            }

            // Stwórz DiagnosticDescriptor na podstawie RuleDefinition
            return new DiagnosticDescriptor(
                id: ruleDefinition.Id,
                title: ruleDefinition.Title, // Używamy stringa bezpośrednio
                messageFormat: ruleDefinition.MessageFormat, // Używamy stringa bezpośrednio
                category: ruleDefinition.Category,
                defaultSeverity: ToRoslynSeverity(ruleDefinition.DefaultSeverity),
                isEnabledByDefault: ruleDefinition.IsEnabledByDefault,
                description: ruleDefinition.Description, // Używamy stringa bezpośrednio
                helpLinkUri: null // Można dodać, jeśli macie dokumentację online dla reguł
                                  // customTags: Można dodać jeśli potrzebne
            );
        });
    }

    public static bool TryCreateDiagnostic(ValidationResult validationResult, Location location,out Diagnostic result)
    {
      
        if (validationResult.IsValid || string.IsNullOrEmpty(validationResult.RuleId) || string.IsNullOrEmpty(validationResult.Message))
        {
            result = null;
            return false;
        }

        DiagnosticDescriptor baseDescriptor = Get(validationResult.RuleId!);

        var descriptorForFinalMessage = new DiagnosticDescriptor(
            id: baseDescriptor.Id,
            title: baseDescriptor.Title,
            messageFormat: "{0}",
            category: baseDescriptor.Category,
            defaultSeverity: ToRoslynSeverity(validationResult.Severity),
            isEnabledByDefault: baseDescriptor.IsEnabledByDefault,
            description: baseDescriptor.Description,
            helpLinkUri: baseDescriptor.HelpLinkUri,
            customTags: baseDescriptor.CustomTags.ToArray()
        );

        result= Diagnostic.Create(descriptorForFinalMessage, location, validationResult.Message);
        return true;
    }
}