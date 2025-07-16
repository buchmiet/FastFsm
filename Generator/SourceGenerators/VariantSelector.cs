using System.Linq;
using Generator.Model;
using Microsoft.CodeAnalysis;
using static Generator.Strings;

namespace Generator.SourceGenerators;

public class VariantSelector
{
    public void DetermineVariant(StateMachineModel model, INamedTypeSymbol classSymbol)
    {
        var config = model.GenerationConfig;

        // 1. Analiza użycia - wykryj jakie funkcjonalności są używane
        DetectUsedFeatures(model, classSymbol, config);

        // 2. Sprawdź czy użytkownik wymusza konkretny wariant
        var forcedVariant = GetForcedVariant(classSymbol, config);

        if (forcedVariant.HasValue)
        {
            config.Variant = forcedVariant.Value;
            config.IsForced = true;

            // Dostosuj flagi do wymuszonego wariantu
            AdjustFlagsForVariant(config);
        }
        else
        {
            // 3. Automatyczny wybór wariantu na podstawie użytych funkcjonalności
            config.Variant = SelectVariantBasedOnFeatures(config);
            config.IsForced = false;
        }
    }

    private void DetectUsedFeatures(StateMachineModel model, INamedTypeSymbol classSymbol, GenerationConfig config)
    {
        // Wykryj OnEntry/Exit
        config.HasOnEntryExit = model.States.Values.Any(s =>
            !string.IsNullOrEmpty(s.OnEntryMethod) ||
            !string.IsNullOrEmpty(s.OnExitMethod));


        

        // HasPayload jest już ustawione przez parser

        // Wykryj Extensions z atrybutu StateMachine
        var stateMachineAttr = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == StateMachineAttributeFullName);

        if (stateMachineAttr != null)
        {
            var generateExtensibleArg = stateMachineAttr.NamedArguments
                .FirstOrDefault(na => na.Key == nameof(Abstractions.Attributes.StateMachineAttribute.GenerateExtensibleVersion));

            config.HasExtensions = generateExtensibleArg.Key != null && (bool)generateExtensibleArg.Value.Value!; 
        }
        else
        {
            config.HasExtensions = false;
        }
    }

    private GenerationVariant? GetForcedVariant(INamedTypeSymbol classSymbol, GenerationConfig config)
    {
        var genModeAttr = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == GenerationModeAttributeFullName);

        if (genModeAttr?.ConstructorArguments.Length > 0)
        {
            var modeValue = genModeAttr.ConstructorArguments[0].Value;
            var forceArg = genModeAttr.NamedArguments.FirstOrDefault(na => na.Key == "Force");

            if (forceArg.Key != null && forceArg.Value.Value is true && modeValue is int modeInt)
            {
                return (Abstractions.Attributes.GenerationMode)modeInt switch
                {
                    Abstractions.Attributes.GenerationMode.Pure => GenerationVariant.Pure,
                    Abstractions.Attributes.GenerationMode.Basic => GenerationVariant.Basic,
                    Abstractions.Attributes.GenerationMode.WithPayload => GenerationVariant.WithPayload,
                    Abstractions.Attributes.GenerationMode.WithExtensions => GenerationVariant.WithExtensions,
                    Abstractions.Attributes.GenerationMode.Full => GenerationVariant.Full,
                    Abstractions.Attributes.GenerationMode.Auto => null,
                    _ => GenerationVariant.Pure // Fallback dla nieobsługiwanych
                };
            }
        }

        return null;
    }

    private GenerationVariant SelectVariantBasedOnFeatures(GenerationConfig config)
    {
        // Tabela decyzyjna - czytelna i łatwa do modyfikacji
        return (config.HasPayload, config.HasExtensions, config.HasOnEntryExit) switch
        {
            (true, true, _) => GenerationVariant.Full,
            (true, false, _) => GenerationVariant.WithPayload,
            (false, true, _) => GenerationVariant.WithExtensions,
            (false, false, true) => GenerationVariant.Basic,
            (false, false, false) => GenerationVariant.Pure
        };
    }

    private void AdjustFlagsForVariant(GenerationConfig config)
    {
        // Upewnij się, że flagi są zgodne z wybranym wariantem
        switch (config.Variant)
        {
            case GenerationVariant.Pure:
                config.HasOnEntryExit = false;
                config.HasPayload = false;
                config.HasExtensions = false;
                break;

            case GenerationVariant.Basic:
                config.HasOnEntryExit = true;
                config.HasPayload = false;
                config.HasExtensions = false;
                break;

            case GenerationVariant.WithPayload:
                // HasOnEntryExit - zachowaj oryginalne wykryte
                config.HasPayload = true;
                config.HasExtensions = false;
                break;

            case GenerationVariant.WithExtensions:
                // HasOnEntryExit - zachowaj oryginalne wykryte
                config.HasPayload = false;
                config.HasExtensions = true;
                break;

            case GenerationVariant.Full:
                config.HasOnEntryExit = true;
                config.HasPayload = true;
                config.HasExtensions = true;
                break;
        }
    }
}