using System;
using System.Collections.Generic;
using System.Linq;
using Generator.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Generator.Infrastructure;
using Generator.Helpers;

namespace Generator.Parsers
{
    internal class FluentParser : IStateMachineParser
    {
        private readonly Compilation _compilation;
        private readonly SourceProductionContext _context;
        private SemanticModel? _semanticModel;
        private INamedTypeSymbol? _classSymbol;
        private ClassDeclarationSyntax? _classDecl;

        private readonly TypeSystemHelper _typeHelper = new();
        private CallbackSignatureAnalyzer? _callbackAnalyzer;
        private INamedTypeSymbol? _stateEnumSymbol;

        public FluentParser(Compilation compilation, SourceProductionContext context)
        {
            _compilation = compilation;
            _context = context;
        }

        public bool TryParse(
            ClassDeclarationSyntax classDeclaration,
            out StateMachineModel? model,
            Action<string>? report = null)
        {
            model = null;
            _classDecl = classDeclaration;
            
            // Check if this class uses Fluent API (has Configure method)
            var configureMethod = FindConfigureMethod(classDeclaration);
            if (configureMethod == null)
            {
                report?.Invoke($"[FluentParser] No Configure() method found in {classDeclaration.Identifier.Text}");
                return false;
            }

            report?.Invoke($"[FluentParser] Found Configure() method in {classDeclaration.Identifier.Text}");

            // Get semantic model and class symbol
            _semanticModel = _compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            _classSymbol = _semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

            // Initialize model
            model = new StateMachineModel
            {
                ClassName = classDeclaration.Identifier.Text,
                Namespace = GetNamespace(classDeclaration),
                States = new Dictionary<string, StateModel>(),
                Transitions = new List<TransitionModel>(),
                GenerationConfig = new GenerationConfig()
            };

            // Extract configuration and types from [StateMachine] attribute
            if (!ExtractTypesFromAttribute(classDeclaration, model, report))
            {
                return false;
            }

            // Parse the Configure method body
            if (!ParseConfigureMethod(configureMethod, model, report))
            {
                return false;
            }

            // Populate ExpectedPayloadType for transitions (Default or per-trigger)
            foreach (var t in model.Transitions)
            {
                if (!string.IsNullOrEmpty(t.Trigger) && model.TriggerPayloadTypes.TryGetValue(t.Trigger, out var trigPayload))
                {
                    t.ExpectedPayloadType = trigPayload;
                }
                else if (!string.IsNullOrEmpty(model.DefaultPayloadType))
                {
                    t.ExpectedPayloadType = model.DefaultPayloadType;
                }
            }

            // If class signals fluent usage (Configure exists) but no DSL recognized,
            // fall back to enum-only states model for parity with legacy parser.
            if (model.States.Count == 0 && model.Transitions.Count == 0)
            {
                ApplyEnumOnlyFallback(model, report);
            }

            report?.Invoke($"[FluentParser] Successfully parsed {model.States.Count} states and {model.Transitions.Count} transitions");
            return true;
        }

        private MethodDeclarationSyntax? FindConfigureMethod(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "Configure" && 
                                    m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));
        }

        private bool ExtractTypesFromAttribute(ClassDeclarationSyntax classDeclaration, StateMachineModel model, Action<string>? report)
        {
            if (_classSymbol == null)
            {
                report?.Invoke("[FluentParser] Semantic class symbol not available");
                return false;
            }

            var smAttr = _classSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == Strings.StateMachineAttributeFullName);
            if (smAttr == null)
            {
                report?.Invoke("[FluentParser] [StateMachine] attribute not found");
                return false;
            }

            // Constructor args: (typeof(State), typeof(Trigger))
            if (smAttr.ConstructorArguments.Length >= 2)
            {
                if (smAttr.ConstructorArguments[0].Value is INamedTypeSymbol stateSym)
                {
                    model.StateType = _typeHelper.BuildFullTypeName(stateSym);
                    _stateEnumSymbol = stateSym;
                    report?.Invoke($"[FluentParser] State type: {model.StateType}");
                }
                if (smAttr.ConstructorArguments[1].Value is INamedTypeSymbol triggerSym)
                {
                    model.TriggerType = _typeHelper.BuildFullTypeName(triggerSym);
                    report?.Invoke($"[FluentParser] Trigger type: {model.TriggerType}");
                }
            }
            else
            {
                // Lenient fallback: extract names from attribute syntax (typeof(...))
                var attrSyntax = classDeclaration.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .FirstOrDefault(attr =>
                    {
                        var n = attr.Name.ToString();
                        return n == "StateMachine" || n == "StateMachineAttribute" ||
                               n.EndsWith(".StateMachine") || n.EndsWith(".StateMachineAttribute");
                    });

                if (attrSyntax?.ArgumentList?.Arguments.Count >= 2)
                {
                    static string? ExtractTypeName(AttributeArgumentSyntax a)
                        => (a.Expression as TypeOfExpressionSyntax)?.Type?.ToString();

                    var stName = ExtractTypeName(attrSyntax.ArgumentList.Arguments[0]);
                    var trName = ExtractTypeName(attrSyntax.ArgumentList.Arguments[1]);

                    if (!string.IsNullOrEmpty(stName)) model.StateType = stName!;
                    if (!string.IsNullOrEmpty(trName)) model.TriggerType = trName!;
                    report?.Invoke($"[FluentParser] [Lenient] Types from syntax: state={model.StateType}, trigger={model.TriggerType}");

                    // Try resolving state symbol so that enum-only fallback can enumerate members
                    if (!string.IsNullOrEmpty(model.StateType))
                    {
                        // If type is unqualified, try with containing namespace prefix
                        INamedTypeSymbol? resolved = _compilation.GetTypeByMetadataName(model.StateType) as INamedTypeSymbol;
                        if (resolved == null && !string.IsNullOrEmpty(model.Namespace) && !model.StateType!.Contains('.'))
                        {
                            var fq = string.IsNullOrEmpty(model.Namespace) ? model.StateType : ($"{model.Namespace}.{model.StateType}");
                            resolved = _compilation.GetTypeByMetadataName(fq) as INamedTypeSymbol;
                        }
                        _stateEnumSymbol = resolved;
                    }
                }
                else
                {
                    report?.Invoke("[FluentParser] Invalid [StateMachine] constructor arguments and no syntax fallback available");
                    return false;
                }
            }

            // Named args: DefaultPayloadType, GenerateStructuralApi, ContinueOnCapturedContext, EnableHierarchy
            var defaultPayloadArg = smAttr.NamedArguments.FirstOrDefault(na => na.Key == nameof(Abstractions.Attributes.StateMachineAttribute.DefaultPayloadType));
            if (defaultPayloadArg.Key != null && defaultPayloadArg.Value.Value is INamedTypeSymbol payloadSym)
            {
                model.DefaultPayloadType = _typeHelper.BuildFullTypeName(payloadSym);
                model.GenerationConfig.HasPayload = true;
                report?.Invoke($"[FluentParser] DefaultPayloadType: {model.DefaultPayloadType}");
            }

            var structuralApiArg = smAttr.NamedArguments.FirstOrDefault(na => na.Key == "GenerateStructuralApi");
            if (structuralApiArg.Key != null && structuralApiArg.Value.Value is bool structural)
            {
                model.EmitStructuralHelpers = structural;
            }

            var continueCtxArg = smAttr.NamedArguments.FirstOrDefault(na => na.Key == "ContinueOnCapturedContext");
            if (continueCtxArg.Key != null && continueCtxArg.Value.Value is bool cont)
            {
                model.ContinueOnCapturedContext = cont;
            }

            var enableHierarchyArg = smAttr.NamedArguments.FirstOrDefault(na => na.Key == "EnableHierarchy");
            if (enableHierarchyArg.Key != null && enableHierarchyArg.Value.Value is bool enableHsm)
            {
                model.HierarchyEnabled = enableHsm;
            }

            // Also honor [PayloadType] attributes (class-level and method-level)
            ParsePayloadTypeAttributes(model, report);

            return true;
        }

        private bool ParseConfigureMethod(MethodDeclarationSyntax configureMethod, StateMachineModel model, Action<string>? report)
        {
            // Find the expression body or block body
            ExpressionSyntax? expression = null;
            
            if (configureMethod.ExpressionBody != null)
            {
                expression = configureMethod.ExpressionBody.Expression;
            }
            else if (configureMethod.Body != null)
            {
                // Look for return statement with FSM chain
                var returnStatement = configureMethod.Body.Statements
                    .OfType<ReturnStatementSyntax>()
                    .FirstOrDefault();
                expression = returnStatement?.Expression;
            }

            if (expression == null)
            {
                report?.Invoke("[FluentParser] No FSM configuration found in Configure() method");
                return false;
            }

            // Parse the fluent API chain
            ParseFluentChain(expression, model, report);

            // Add ordinal values to states (required for code generation)
            int ordinal = 0;
            foreach (var state in model.States.Values)
            {
                state.OrdinalValue = ordinal++;
            }

            return true;
        }

        private void ParseFluentChain(ExpressionSyntax expression, StateMachineModel model, Action<string>? report)
        {
            // Track current state being configured
            string? currentState = null;
            
            // Walk through the method call chain
            var invocations = new List<InvocationExpressionSyntax>();
            CollectInvocations(expression, invocations);

            foreach (var invocation in invocations)
            {
                var methodName = GetMethodName(invocation);
                report?.Invoke($"[FluentParser] Processing method: {methodName}");

                switch (methodName)
                {
                    case "State":
                        currentState = ParseStateCall(invocation, model, report);
                        break;
                
                    case "On":
                        if (currentState != null)
                        {
                            ParseTransitionStart(invocation, currentState, model, report);
                        }
                        break;
                
                    case "OnInternal":
                        if (currentState != null)
                        {
                            ParseInternalTransition(invocation, currentState, model, report);
                        }
                        break;

                    case "GoTo":
                        CompleteTransition(invocation, model, report);
                        break;
                
                    case "Action":
                        ParseAction(invocation, model, report);
                        break;
                    
                    case "Guard":
                        ParseGuard(invocation, model, report);
                        break;
                    
                    case "OnEntry":
                        if (currentState != null)
                        {
                            ParseOnEntry(invocation, currentState, model, report);
                        }
                        break;
                    
                    case "OnExit":
                        if (currentState != null)
                        {
                            ParseOnExit(invocation, currentState, model, report);
                        }
                        break;
                }
            }
        }

        private void ParseInternalTransition(InvocationExpressionSyntax invocation, string fromState, StateMachineModel model, Action<string>? report)
        {
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                if (arg.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var triggerName = memberAccess.Name.Identifier.Text;

                    var transition = new TransitionModel
                    {
                        FromState = fromState,
                        ToState = fromState,
                        Trigger = triggerName,
                        IsInternal = true
                    };
                    model.Transitions.Add(transition);
                    report?.Invoke($"[FluentParser] Added internal transition in {fromState} on {triggerName}");
                }
            }
        }

        private void ApplyEnumOnlyFallback(StateMachineModel model, Action<string>? report)
        {
            INamedTypeSymbol? stateEnum = _stateEnumSymbol;
            if (stateEnum == null && !string.IsNullOrEmpty(model.StateType))
            {
                stateEnum = _compilation.GetTypeByMetadataName(model.StateType) as INamedTypeSymbol;
            }

            if (stateEnum == null)
            {
                // Try syntax-based enumeration as last resort (lenient):
                // find enum declaration matching the simple type name in this syntax tree
                string? typeName = model.StateType;
                string simpleName = typeName ?? string.Empty;
                if (!string.IsNullOrEmpty(typeName))
                {
                    int lastDot = typeName.LastIndexOf('.');
                    int lastPlus = typeName.LastIndexOf('+');
                    int cut = Math.Max(lastDot, lastPlus);
                    if (cut >= 0 && cut + 1 < typeName.Length)
                        simpleName = typeName.Substring(cut + 1);
                }

                var enumDecl = _classDecl?.SyntaxTree.GetRoot().DescendantNodes()
                    .OfType<EnumDeclarationSyntax>()
                    .FirstOrDefault(e => e.Identifier.Text == simpleName);

                if (enumDecl != null)
                {
                    model.States.Clear();
                    int ordinal = 0;
                    foreach (var member in enumDecl.Members)
                    {
                        var memberName = member.Identifier.Text;
                        int value = ordinal;
                        if (member.EqualsValue?.Value is LiteralExpressionSyntax lit && lit.Token.Value is int explicitValue)
                        {
                            value = explicitValue;
                            ordinal = explicitValue + 1;
                        }
                        else
                        {
                            ordinal++;
                        }
                        if (!model.States.ContainsKey(memberName))
                        {
                            model.States[memberName] = new StateModel { Name = memberName, OrdinalValue = value };
                        }
                    }
                    model.UsedEnumOnlyFallback = true;
                    report?.Invoke($"[FluentParser] Enum-only fallback (syntax): {model.States.Count} states from enum {simpleName}");
                    return;
                }

                // No symbol and no syntax; still mark fallback for diagnostics parity
                model.UsedEnumOnlyFallback = true;
                report?.Invoke("[FluentParser] Enum-only fallback: could not resolve state enum symbol");
                return;
            }

            model.States.Clear();
            foreach (var member in stateEnum.GetMembers().OfType<IFieldSymbol>())
            {
                if (member.IsConst && member.HasConstantValue)
                {
                    var name = member.Name;
                    int ordinal = member.ConstantValue is int iv ? iv : 0;
                    if (!model.States.ContainsKey(name))
                    {
                        model.States[name] = new StateModel
                        {
                            Name = name,
                            OrdinalValue = ordinal
                        };
                    }
                }
            }

            model.UsedEnumOnlyFallback = true;
            report?.Invoke($"[FluentParser] Enum-only fallback applied: {model.States.Count} states from enum");
        }

        private void CollectInvocations(ExpressionSyntax expression, List<InvocationExpressionSyntax> invocations)
        {
            if (expression is InvocationExpressionSyntax invocation)
            {
                // Recursively collect from the left side (previous call in chain)
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    CollectInvocations(memberAccess.Expression, invocations);
                }
                invocations.Add(invocation);
            }
        }

        private string? GetMethodName(InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Name.Identifier.Text;
            }
            return null;
        }

        private string? ParseStateCall(InvocationExpressionSyntax invocation, StateMachineModel model, Action<string>? report)
        {
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                if (arg.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var stateName = memberAccess.Name.Identifier.Text;
                    
                    if (!model.States.ContainsKey(stateName))
                    {
                        model.States[stateName] = new StateModel
                        {
                            Name = stateName,
                            OrdinalValue = 0 // Will be set later
                        };
                        report?.Invoke($"[FluentParser] Added state: {stateName}");
                    }
                    
                    return stateName;
                }
            }
            return null;
        }

        private void ParseTransitionStart(InvocationExpressionSyntax invocation, string fromState, StateMachineModel model, Action<string>? report)
        {
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                if (arg.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var triggerName = memberAccess.Name.Identifier.Text;
                    
                    // Create partial transition (will be completed by GoTo)
                    var transition = new TransitionModel
                    {
                        FromState = fromState,
                        Trigger = triggerName,
                        IsInternal = false
                    };
                    
                    model.Transitions.Add(transition);
                    report?.Invoke($"[FluentParser] Started transition from {fromState} on {triggerName}");
                }
            }
        }

        private void CompleteTransition(InvocationExpressionSyntax invocation, StateMachineModel model, Action<string>? report)
        {
            // Find the last incomplete transition
            var lastTransition = model.Transitions.LastOrDefault(t => string.IsNullOrEmpty(t.ToState));
            if (lastTransition != null && invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                if (arg.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var toStateName = memberAccess.Name.Identifier.Text;
                    lastTransition.ToState = toStateName;
                    
                    // Ensure target state exists
                    if (!model.States.ContainsKey(toStateName))
                    {
                        model.States[toStateName] = new StateModel
                        {
                            Name = toStateName,
                            OrdinalValue = 0 // Will be set later
                        };
                    }
                    
                    report?.Invoke($"[FluentParser] Completed transition to {toStateName}");
                }
            }
        }
        
        private void ParseAction(InvocationExpressionSyntax invocation, StateMachineModel model, Action<string>? report)
        {
            if (model.Transitions.Count == 0) return;

            var lastTransition = model.Transitions[model.Transitions.Count - 1];

            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                
                // Check if it's a nameof expression
                if (arg.Expression is InvocationExpressionSyntax nameofInvocation &&
                    nameofInvocation.Expression is IdentifierNameSyntax identifier &&
                    identifier.Identifier.Text == "nameof")
                {
                    if (nameofInvocation.ArgumentList.Arguments.Count > 0 &&
                        nameofInvocation.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax methodName)
                    {
                        lastTransition.ActionMethod = methodName.Identifier.Text;
                        report?.Invoke($"[FluentParser] Set action method: {lastTransition.ActionMethod}");
                        AnalyzeActionSignature(lastTransition);
                    }
                }
                // Check if it's a string literal
                else if (arg.Expression is LiteralExpressionSyntax literal && 
                         literal.Token.Value is string actionName)
                {
                    lastTransition.ActionMethod = actionName;
                    report?.Invoke($"[FluentParser] Set action method: {actionName}");
                    AnalyzeActionSignature(lastTransition);
                }

                // If no GoTo was specified, this is an internal transition
                if (string.IsNullOrEmpty(lastTransition.ToState))
                {
                    lastTransition.ToState = lastTransition.FromState;
                    lastTransition.IsInternal = true;
                    report?.Invoke($"[FluentParser] Marked as internal transition");
                }
            }
        }

        private void ParseGuard(InvocationExpressionSyntax invocation, StateMachineModel model, Action<string>? report)
        {
            if (model.Transitions.Count == 0) return;

            var lastTransition = model.Transitions[model.Transitions.Count - 1];

            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                
                // Check if it's a nameof expression
                if (arg.Expression is InvocationExpressionSyntax nameofInvocation &&
                    nameofInvocation.Expression is IdentifierNameSyntax identifier &&
                    identifier.Identifier.Text == "nameof")
                {
                    if (nameofInvocation.ArgumentList.Arguments.Count > 0 &&
                        nameofInvocation.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax methodName)
                    {
                        lastTransition.GuardMethod = methodName.Identifier.Text;
                        report?.Invoke($"[FluentParser] Set guard method: {lastTransition.GuardMethod}");
                        AnalyzeGuardSignature(lastTransition);
                    }
                }
                // Check if it's a string literal
                else if (arg.Expression is LiteralExpressionSyntax literal && 
                         literal.Token.Value is string guardName)
                {
                    lastTransition.GuardMethod = guardName;
                    report?.Invoke($"[FluentParser] Set guard method: {guardName}");
                    AnalyzeGuardSignature(lastTransition);
                }
            }
        }

        private void EnsureAnalyzers()
        {
            if (_callbackAnalyzer == null)
            {
                var asyncAnalyzer = new AsyncSignatureAnalyzer(_typeHelper);
                _callbackAnalyzer = new CallbackSignatureAnalyzer(_typeHelper, asyncAnalyzer);
            }
        }

        private void AnalyzeActionSignature(TransitionModel t)
        {
            if (_classSymbol == null || string.IsNullOrEmpty(t.ActionMethod)) return;
            EnsureAnalyzers();
            var sig = _callbackAnalyzer!.AnalyzeCallback(_classSymbol, t.ActionMethod!, "Action", _compilation);
            t.ActionSignature = sig;
            t.ActionIsAsync = sig.IsAsync;
            t.ActionHasParameterlessOverload = sig.HasParameterless;
            t.ActionExpectsPayload = sig.HasPayloadOnly || sig.HasPayloadAndToken;
        }

        private void AnalyzeGuardSignature(TransitionModel t)
        {
            if (_classSymbol == null || string.IsNullOrEmpty(t.GuardMethod)) return;
            EnsureAnalyzers();
            var sig = _callbackAnalyzer!.AnalyzeCallback(_classSymbol, t.GuardMethod!, "Guard", _compilation);
            t.GuardSignature = sig;
            t.GuardIsAsync = sig.IsAsync;
            t.GuardHasParameterlessOverload = sig.HasParameterless;
            t.GuardExpectsPayload = sig.HasPayloadOnly || sig.HasPayloadAndToken;
        }

        private void ParsePayloadTypeAttributes(StateMachineModel model, Action<string>? report)
        {
            if (_classSymbol == null) return;

            // Class-level [PayloadType(typeof(Default))]
            var classPayloadAttrs = _classSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == Strings.PayloadTypeAttributeFullName);

            foreach (var attr in classPayloadAttrs)
            {
                if (attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Value is INamedTypeSymbol payloadType)
                {
                    model.DefaultPayloadType = _typeHelper.BuildFullTypeName(payloadType);
                    model.GenerationConfig.HasPayload = true;
                    report?.Invoke($"[FluentParser] [PayloadType] default: {model.DefaultPayloadType}");
                }
                else if (attr.ConstructorArguments.Length == 2)
                {
                    var triggerArg = attr.ConstructorArguments[0];
                    var payloadTypeArg = attr.ConstructorArguments[1];
                    var triggerEnum = _compilation.GetTypeByMetadataName(model.TriggerType) as INamedTypeSymbol;
                    if (triggerEnum != null && payloadTypeArg.Value is INamedTypeSymbol named)
                    {
                        var triggerName = ResolveEnumMemberName(triggerArg, triggerEnum);
                        if (triggerName != null)
                        {
                            model.TriggerPayloadTypes[triggerName] = _typeHelper.BuildFullTypeName(named);
                            model.GenerationConfig.HasPayload = true;
                            report?.Invoke($"[FluentParser] [PayloadType] for {triggerName}: {model.TriggerPayloadTypes[triggerName]}");
                        }
                    }
                }
            }

            // Method-level [PayloadType(Trigger.X, typeof(T))] (overrides)
            foreach (var m in _classSymbol.GetMembers().OfType<IMethodSymbol>())
            {
                foreach (var attr in m.GetAttributes().Where(a => a.AttributeClass?.ToDisplayString() == Strings.PayloadTypeAttributeFullName))
                {
                    if (attr.ConstructorArguments.Length == 2)
                    {
                        var triggerArg = attr.ConstructorArguments[0];
                        var payloadTypeArg = attr.ConstructorArguments[1];
                        var triggerEnum = _compilation.GetTypeByMetadataName(model.TriggerType) as INamedTypeSymbol;
                        if (triggerEnum != null && payloadTypeArg.Value is INamedTypeSymbol named)
                        {
                            var triggerName = ResolveEnumMemberName(triggerArg, triggerEnum);
                            if (triggerName != null)
                            {
                                model.TriggerPayloadTypes[triggerName] = _typeHelper.BuildFullTypeName(named);
                                model.GenerationConfig.HasPayload = true;
                                report?.Invoke($"[FluentParser] [PayloadType method] for {triggerName}: {model.TriggerPayloadTypes[triggerName]}");
                            }
                        }
                    }
                }
            }
        }

        private static string? ResolveEnumMemberName(TypedConstant enumValueConstant, INamedTypeSymbol enumTypeSymbol)
        {
            if (enumValueConstant.Kind == TypedConstantKind.Error || enumValueConstant.Value == null)
                return null;
            foreach (var member in enumTypeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                if (member.IsConst && member.HasConstantValue && member.ConstantValue != null && Equals(member.ConstantValue, enumValueConstant.Value))
                {
                    return member.Name;
                }
            }
            return null;
        }
        
        private void ParseOnEntry(InvocationExpressionSyntax invocation, string currentState, StateMachineModel model, Action<string>? report)
        {
            if (!model.States.TryGetValue(currentState, out var state)) return;
            
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                
                // Check if it's a nameof expression
                if (arg.Expression is InvocationExpressionSyntax nameofInvocation &&
                    nameofInvocation.Expression is IdentifierNameSyntax identifier &&
                    identifier.Identifier.Text == "nameof")
                {
                    if (nameofInvocation.ArgumentList.Arguments.Count > 0 &&
                        nameofInvocation.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax methodName)
                    {
                        state.OnEntryMethod = methodName.Identifier.Text;
                        report?.Invoke($"[FluentParser] Set OnEntry for {currentState}: {state.OnEntryMethod}");
                    }
                }
                // Check if it's a string literal
                else if (arg.Expression is LiteralExpressionSyntax literal && 
                         literal.Token.Value is string entryName)
                {
                    state.OnEntryMethod = entryName;
                    report?.Invoke($"[FluentParser] Set OnEntry for {currentState}: {entryName}");
                }
            }
        }
        
        private void ParseOnExit(InvocationExpressionSyntax invocation, string currentState, StateMachineModel model, Action<string>? report)
        {
            if (!model.States.TryGetValue(currentState, out var state)) return;
            
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                
                // Check if it's a nameof expression
                if (arg.Expression is InvocationExpressionSyntax nameofInvocation &&
                    nameofInvocation.Expression is IdentifierNameSyntax identifier &&
                    identifier.Identifier.Text == "nameof")
                {
                    if (nameofInvocation.ArgumentList.Arguments.Count > 0 &&
                        nameofInvocation.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax methodName)
                    {
                        state.OnExitMethod = methodName.Identifier.Text;
                        report?.Invoke($"[FluentParser] Set OnExit for {currentState}: {state.OnExitMethod}");
                    }
                }
                // Check if it's a string literal
                else if (arg.Expression is LiteralExpressionSyntax literal && 
                         literal.Token.Value is string exitName)
                {
                    state.OnExitMethod = exitName;
                    report?.Invoke($"[FluentParser] Set OnExit for {currentState}: {exitName}");
                }
            }
        }

        private string? GetNamespace(ClassDeclarationSyntax classDeclaration)
        {
            var namespaceDeclaration = classDeclaration.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();
            return namespaceDeclaration?.Name.ToString();
        }
    }
}
