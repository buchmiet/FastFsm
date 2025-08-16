using Generator.Model;
using Generator.Model.Dtos;
using System.Collections.Generic;
using System.Linq;
using Generator.Infrastructure;
using static Generator.Strings;

namespace Generator.Helpers;

public class FactoryGenerationModelBuilder
{
    public static FactoryGenerationModel Create(StateMachineModel model)
    {
        var typeHelper = new TypeSystemHelper();

        // Przetwarzanie typu State
        var stateTypeInfo = new TypeGenerationInfo
        {
            UsageName = typeHelper.FormatTypeForUsage(model.StateType),
            TypeOfName = typeHelper.FormatForTypeof(model.StateType),
            SimpleName = typeHelper.GetSimpleTypeName(model.StateType),
            RequiredNamespaces = typeHelper.GetRequiredNamespaces(model.StateType).ToList()
        };

        // Przetwarzanie typu Trigger
        var triggerTypeInfo = new TypeGenerationInfo
        {
            UsageName = typeHelper.FormatTypeForUsage(model.TriggerType),
            TypeOfName = typeHelper.FormatForTypeof(model.TriggerType),
            SimpleName = typeHelper.GetSimpleTypeName(model.TriggerType),
            RequiredNamespaces = typeHelper.GetRequiredNamespaces(model.TriggerType).ToList()
        };

        // Przetwarzanie typu Payload (jeśli istnieje)
        TypeGenerationInfo? payloadTypeInfo = null;
        var isSinglePayload = (model.GenerationConfig.HasPayload || model.DefaultPayloadType != null || model.TriggerPayloadTypes.Any())
                              && !model.TriggerPayloadTypes.Any()
                              && model.DefaultPayloadType != null;

        if (isSinglePayload)
        {
            payloadTypeInfo = new TypeGenerationInfo
            {
                UsageName = typeHelper.FormatTypeForUsage(model.DefaultPayloadType!),
                TypeOfName = typeHelper.FormatForTypeof(model.DefaultPayloadType!),
                SimpleName = typeHelper.GetSimpleTypeName(model.DefaultPayloadType!),
                RequiredNamespaces = typeHelper.GetRequiredNamespaces(model.DefaultPayloadType!).ToList()
            };
        }

        // Zbieranie wszystkich unikalnych przestrzeni nazw
        var allNamespaces = new HashSet<string>();
        allNamespaces.UnionWith(stateTypeInfo.RequiredNamespaces);
        allNamespaces.UnionWith(triggerTypeInfo.RequiredNamespaces);
        if (payloadTypeInfo != null)
        {
            allNamespaces.UnionWith(payloadTypeInfo.RequiredNamespaces);
        }

        // Dodaj stałe usingi
        allNamespaces.Add(NamespaceSystem);
        allNamespaces.Add(NamespaceMicrosoftDependencyInjection);
        if (model.GenerateLogging)
        {
            allNamespaces.Add(NamespaceMicrosoftExtensionsLogging);
        }
        allNamespaces.Add(NamespaceStateMachineContracts);
        allNamespaces.Add("StateMachine.DependencyInjection");
        if (!string.IsNullOrEmpty(model.Namespace))
        {
            allNamespaces.Add(model.Namespace);
        }

        // Tworzenie finalnego modelu
        return new FactoryGenerationModel
        {
            StateType = stateTypeInfo,
            TriggerType = triggerTypeInfo,
            PayloadType = payloadTypeInfo,
            ClassName = model.ClassName,
            UserNamespace = model.Namespace,
            ShouldGenerateLogging = model.GenerateLogging,
            HasExtensions = model.GenerationConfig.HasExtensions,
            IsSinglePayload = isSinglePayload,
            AllRequiredNamespaces = allNamespaces.OrderBy(ns => ns).ToList()
        };
    }
}
