using System;
using System.Collections.Generic;
using System.Linq;
using Generator.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Generator.Infrastructure;
using Generator.Helpers;
using Generator.Rules.Definitions;

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
                Namespace = GetNamespace(),
                States = new Dictionary<string, StateModel>(),
                Transitions = new List<TransitionModel>(),
                GenerationConfig = new GenerationConfig()
            };

            // Capture nested containing types (outer classes) to mirror nested partials
            if (_classSymbol != null)
            {
                var containers = new List<string>();
                var containerSymbol = _classSymbol.ContainingType;
                while (containerSymbol != null)
                {
                    containers.Insert(0, containerSymbol.Name);
                    containerSymbol = containerSymbol.ContainingType;
                }
                model.ContainerClasses = containers;
                report?.Invoke($"[FluentParser] ContainerClasses: [{string.Join(", ", containers)}]");
            }

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

            // Finalize all callback signatures after the model is fully parsed
            FinalizeSignatures(model, report);
            
            // Build HSM hierarchy if enabled (must be done AFTER parsing all states)
            BuildHSMHierarchy(model, report);
            
            // Validate HSM configuration if hierarchy is enabled
            if (model.HierarchyEnabled)
            {
                ValidateHsmModel(model, report);
            }

            // Deduplicate transitions: Keep only the first transition for each (FromState, Trigger, Priority) tuple
            // This matches Legacy parser behavior for source-order resolution
            var originalCount = model.Transitions.Count;
            var deduplicatedTransitions = new List<TransitionModel>();
            var seenTransitions = new HashSet<(string FromState, string Trigger, int Priority)>();
            
            foreach (var transition in model.Transitions)
            {
                var key = (transition.FromState, transition.Trigger, transition.Priority);
                if (!seenTransitions.Contains(key))
                {
                    seenTransitions.Add(key);
                    deduplicatedTransitions.Add(transition);
                }
                else
                {
                    report?.Invoke($"[FluentParser] Duplicate transition ignored: {transition.FromState} + {transition.Trigger} (Priority={transition.Priority}) -> {transition.ToState}");
                }
            }
            
            if (deduplicatedTransitions.Count < originalCount)
            {
                model.Transitions = deduplicatedTransitions;
                report?.Invoke($"[FluentParser] Deduplicated transitions: {originalCount} -> {deduplicatedTransitions.Count}");
            }

            // Determine async mode: if any guard/action/entry/exit is async, mark machine as async.
            // This mirrors legacy parser behavior where async callbacks flip machine into async mode
            // so generator emits awaitable code paths instead of sync wrappers.
            // NOTE: This must be done AFTER FinalizeSignatures because that's where IsAsync flags are set
            bool hasAsyncTransitions = model.Transitions.Any(tr => tr.GuardIsAsync || tr.ActionIsAsync);
            bool hasAsyncStates = model.States.Values.Any(st => st.OnEntryIsAsync || st.OnExitIsAsync);
            
            if (hasAsyncTransitions || hasAsyncStates)
            {
                model.GenerationConfig.IsAsync = true;
                report?.Invoke($"[FluentParser] Async mode enabled due to async callbacks (transitions: {hasAsyncTransitions}, states: {hasAsyncStates})");
            }

            report?.Invoke($"[FluentParser] Successfully parsed {model.States.Count} states and {model.Transitions.Count} transitions");
            return true;
        }

        private MethodDeclarationSyntax? FindConfigureMethod(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => (m.Identifier.Text == "Configure" || m.Identifier.Text == "SetupStates") && 
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
                .FirstOrDefault(a => a.AttributeClass != null && _typeHelper.BuildFullTypeName(a.AttributeClass) == Strings.StateMachineAttributeFullName);
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
            report?.Invoke($"[FluentParser] ParseConfigureMethod called for method: {configureMethod.Identifier.Text}");
            
            // Find the expression body or block body
            ExpressionSyntax? expression = null;
            
            if (configureMethod.ExpressionBody != null)
            {
                expression = configureMethod.ExpressionBody.Expression;
                // Parse the fluent API chain
                ParseFluentChain(expression, model, report);
            }
            else if (configureMethod.Body != null)
            {
                // First try to find a return statement with FSM chain
                var returnStatement = configureMethod.Body.Statements
                    .OfType<ReturnStatementSyntax>()
                    .FirstOrDefault();
                    
                if (returnStatement?.Expression != null)
                {
                    ParseFluentChain(returnStatement.Expression, model, report);
                }
                else
                {
                    // If no return statement, look for all expression statements with FSM calls
                    var expressionStatements = configureMethod.Body.Statements
                        .OfType<ExpressionStatementSyntax>();
                        
                    report?.Invoke($"[FluentParser] Found {expressionStatements.Count()} expression statements in Configure()");
                    
                    foreach (var statement in expressionStatements)
                    {
                        if (statement.Expression != null)
                        {
                            report?.Invoke($"[FluentParser] Processing statement: {statement.Expression.GetType().Name}");
                            ParseFluentChain(statement.Expression, model, report);
                        }
                    }
                }
            }
            else
            {
                report?.Invoke("[FluentParser] No FSM configuration found in Configure() method");
                return false;
            }

            // Add ordinal values to states (required for code generation)
            // Must use actual enum values, not sequential numbers!
            if (_stateEnumSymbol != null && _stateEnumSymbol.TypeKind == TypeKind.Enum)
            {
                var enumMembers = _stateEnumSymbol.GetMembers().OfType<IFieldSymbol>()
                    .Where(f => f.IsConst && f.HasConstantValue);
                    
                foreach (var state in model.States.Values)
                {
                    var enumField = enumMembers.FirstOrDefault(f => f.Name == state.Name);
                    if (enumField?.ConstantValue != null)
                    {
                        state.OrdinalValue = Convert.ToInt32(enumField.ConstantValue);
                        report?.Invoke($"[FluentParser] State {state.Name} assigned OrdinalValue={state.OrdinalValue} from enum");
                    }
                    else
                    {
                        // Fallback if not found (shouldn't happen)
                        report?.Invoke($"[FluentParser] WARNING: State {state.Name} not found in enum, using fallback ordinal");
                        state.OrdinalValue = 0;
                    }
                }
            }
            else
            {
                // Fallback to sequential numbering if enum not available
                report?.Invoke("[FluentParser] WARNING: State enum symbol not available, using sequential ordinals");
                int ordinal = 0;
                foreach (var state in model.States.Values)
                {
                    state.OrdinalValue = ordinal++;
                }
            }

            return true;
        }

        private void ParseFluentChain(ExpressionSyntax expression, StateMachineModel model, Action<string>? report)
        {
            // Track current state being configured
            string? currentState = null;
            TransitionModel? currentTransition = null;
            
            // Walk through the method call chain
            var invocations = new List<InvocationExpressionSyntax>();
            CollectInvocations(expression, invocations);
            report?.Invoke($"[FluentParser] Found {invocations.Count} invocations in chain");
            
            // Debug: log all method names in chain
            var methodNames = invocations.Select(inv => GetMethodName(inv) ?? "unknown").ToList();
            report?.Invoke($"[FluentParser] Chain methods: {string.Join(" -> ", methodNames)}");

            // PASS 1 — process global directives first (position-independent)
            foreach (var inv in invocations)
            {
                var name = GetMethodName(inv);
                switch (name)
                {
                    case "Extensible":
                        report?.Invoke("[FluentParser] [PASS1] Enabling extensions via .Extensible()");
                        EnableExtensions(model, report);
                        break;
                    case "OnException":
                        report?.Invoke("[FluentParser] [PASS1] Processing global OnException");
                        ParseOnException(inv, model, report);
                        break;
                }
            }

            foreach (var invocation in invocations)
            {
                var methodName = GetMethodName(invocation);
                report?.Invoke($"[FluentParser] Processing method: {methodName} from {invocation.Expression}");

                switch (methodName)
                {
                    case "Extensible":
                        // Already processed in PASS 1
                        continue;
                        
                    case "State":
                    case "At": // Alias for State
                        // Auto-finalize open transition as internal
                        if (currentTransition != null && string.IsNullOrEmpty(currentTransition.ToState))
                        {
                            currentTransition.ToState = currentTransition.FromState;
                            currentTransition.IsInternal = true;
                            report?.Invoke($"[FluentParser] Auto-finalized transition as internal");
                            
                            // Report warning for auto-finalization
                            var descriptor = DiagnosticFactory.Get(RuleIdentifiers.AutoFinalizedTransition);
                            var diagnostic = Diagnostic.Create(
                                descriptor,
                                invocation.GetLocation(),
                                currentTransition.FromState,
                                currentTransition.Trigger);
                            _context.ReportDiagnostic(diagnostic);
                        }
                        currentState = ParseStateCall(invocation, model, report);
                        currentTransition = null; // Clear any previous transition context
                        break;
                
                    case "On":
                        // Auto-finalize previous open transition as internal
                        if (currentTransition != null && string.IsNullOrEmpty(currentTransition.ToState))
                        {
                            currentTransition.ToState = currentTransition.FromState;
                            currentTransition.IsInternal = true;
                            report?.Invoke($"[FluentParser] Auto-finalized previous transition as internal");
                            
                            // Report warning for auto-finalization
                            var descriptor = DiagnosticFactory.Get(RuleIdentifiers.AutoFinalizedTransition);
                            var diagnostic = Diagnostic.Create(
                                descriptor,
                                invocation.GetLocation(),
                                currentTransition.FromState,
                                currentTransition.Trigger);
                            _context.ReportDiagnostic(diagnostic);
                        }
                        if (currentState != null)
                        {
                            currentTransition = ParseTransitionStart(invocation, currentState, model, report, isInternal: false);
                        }
                        break;
                
                    case "OnInternal":
                        // Auto-finalize previous open transition as internal
                        if (currentTransition != null && string.IsNullOrEmpty(currentTransition.ToState))
                        {
                            currentTransition.ToState = currentTransition.FromState;
                            currentTransition.IsInternal = true;
                            report?.Invoke($"[FluentParser] Auto-finalized previous transition as internal");
                            
                            // Report warning for auto-finalization
                            var descriptor = DiagnosticFactory.Get(RuleIdentifiers.AutoFinalizedTransition);
                            var diagnostic = Diagnostic.Create(
                                descriptor,
                                invocation.GetLocation(),
                                currentTransition.FromState,
                                currentTransition.Trigger);
                            _context.ReportDiagnostic(diagnostic);
                        }
                        if (currentState != null)
                        {
                            currentTransition = ParseTransitionStart(invocation, currentState, model, report, isInternal: true);
                        }
                        break;

                    case "GoTo":
                        if (currentTransition != null)
                        {
                            CompleteTransition(invocation, currentTransition, model, report);
                            // Don't set currentTransition to null yet - allow Guard/Action to be chained after GoTo
                        }
                        break;
                    
                    case "Internal":
                        if (currentTransition != null)
                        {
                            currentTransition.ToState = currentTransition.FromState;
                            currentTransition.IsInternal = true;
                            report?.Invoke($"[FluentParser] Finalized transition as internal");
                            currentTransition = null; // Transition is finalized
                        }
                        break;

                    case "Payload":
                        if (currentTransition != null)
                        {
                            ParsePayload(invocation, currentTransition, model, report);
                        }
                        break;
                
                    case "Action":
                        if (currentTransition != null)
                        {
                            ParseAction(invocation, currentTransition, model, report, isAsync: false);
                        }
                        break;
                    
                    case "ActionAsync":
                        if (currentTransition != null)
                        {
                            ParseAction(invocation, currentTransition, model, report, isAsync: true);
                        }
                        break;
                    
                    case "Guard":
                        if (currentTransition != null)
                        {
                            ParseGuard(invocation, currentTransition, model, report, isAsync: false);
                        }
                        break;
                    
                    case "GuardAsync":
                        if (currentTransition != null)
                        {
                            ParseGuard(invocation, currentTransition, model, report, isAsync: true);
                        }
                        break;
                    
                    case "OnEntry":
                        if (currentState != null)
                        {
                            ParseOnEntry(invocation, currentState, model, report, isAsync: false);
                        }
                        break;
                    
                    case "OnEntryAsync":
                        if (currentState != null)
                        {
                            ParseOnEntry(invocation, currentState, model, report, isAsync: true);
                        }
                        break;
                    
                    case "OnExit":
                        if (currentState != null)
                        {
                            ParseOnExit(invocation, currentState, model, report, isAsync: false);
                        }
                        break;
                    
                    case "OnExitAsync":
                        if (currentState != null)
                        {
                            ParseOnExit(invocation, currentState, model, report, isAsync: true);
                        }
                        break;
                    
                    case "OnException":
                        // Already processed in PASS 1
                        continue;
                    
                    case "ChildOf":
                        if (currentState != null)
                        {
                            report?.Invoke($"[FluentParser] Processing ChildOf for state {currentState}");
                            ParseChildOf(invocation, currentState, model, report);
                        }
                        break;
                    
                    case "Initial":
                        if (currentState != null)
                        {
                            report?.Invoke($"[FluentParser] Processing Initial for state {currentState}");
                            ParseInitial(invocation, currentState, model, report);
                        }
                        break;
                    
                    case "HistoryShallow":
                        report?.Invoke($"[FluentParser] Processing HistoryShallow for state: {currentState ?? "null"}");
                        if (currentState != null)
                        {
                            ParseHistory(currentState, model, report, isShallow: true);
                        }
                        else
                        {
                            report?.Invoke("[FluentParser] WARNING: HistoryShallow called without current state context");
                        }
                        break;
                    
                    case "HistoryDeep":
                        if (currentState != null)
                        {
                            ParseHistory(currentState, model, report, isShallow: false);
                        }
                        break;
                    
                    case "Priority":
                        if (currentTransition != null)
                        {
                            ParsePriority(invocation, currentTransition, model, report);
                        }
                        else
                        {
                            ReportPriorityWithoutTransition(invocation);
                        }
                        break;
                }
            }
            
            // Auto-finalize any remaining open transition
            if (currentTransition != null && string.IsNullOrEmpty(currentTransition.ToState))
            {
                // Report error for open transition at end of chain
                var descriptor = DiagnosticFactory.Get(RuleIdentifiers.OpenTransition);
                var diagnostic = Diagnostic.Create(
                    descriptor,
                    invocations.LastOrDefault()?.GetLocation() ?? Location.None,
                    currentTransition.FromState,
                    currentTransition.Trigger);
                _context.ReportDiagnostic(diagnostic);
                
                currentTransition.ToState = currentTransition.FromState;
                currentTransition.IsInternal = true;
                report?.Invoke($"[FluentParser] ERROR: Open transition at end of chain - auto-finalized as internal");
            }
        }

        private void ParseInternalTransition(InvocationExpressionSyntax invocation, string fromState, StateMachineModel model, Action<string>? report)
        {
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                if (TryExtractName(arg.Expression, out var triggerName, report))
                {
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

        private void EnableExtensions(StateMachineModel model, Action<string>? report)
        {
            // Check if GenerateExtensibleVersion exists in GenerationConfig
            // This flag is shared with legacy attribute API
            if (model.GenerationConfig.HasExtensions)
            {
                report?.Invoke("[FluentParser] Warning: Duplicate .Extensible() ignored (already enabled)");
                return;
            }

            model.GenerationConfig.HasExtensions = true;
            report?.Invoke("[FluentParser] Extensions enabled via .Extensible()");
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
                // Handle both regular and generic method names
                return memberAccess.Name switch
                {
                    GenericNameSyntax genericName => genericName.Identifier.Text,
                    SimpleNameSyntax simpleName => simpleName.Identifier.Text,
                    _ => null
                };
            }
            return null;
        }

        private string? ParseStateCall(InvocationExpressionSyntax invocation, StateMachineModel model, Action<string>? report)
        {
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                if (TryExtractName(arg.Expression, out var stateName, report))
                {
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

        private TransitionModel? ParseTransitionStart(InvocationExpressionSyntax invocation, string fromState, StateMachineModel model, Action<string>? report, bool isInternal)
        {
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                if (TryExtractName(arg.Expression, out var triggerName, report))
                {
                    // Create partial transition (will be completed by GoTo or Internal)
                    var transition = new TransitionModel
                    {
                        FromState = fromState,
                        Trigger = triggerName,
                        IsInternal = isInternal,
                        ToState = isInternal ? fromState : null // Internal transitions know their target immediately
                    };
                    
                    model.Transitions.Add(transition);
                    report?.Invoke($"[FluentParser] Started {(isInternal ? "internal " : "")}transition from {fromState} on {triggerName}");
                    return transition;
                }
            }
            return null;
        }

        private void CompleteTransition(InvocationExpressionSyntax invocation, TransitionModel transition, StateMachineModel model, Action<string>? report)
        {
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                if (TryExtractName(arg.Expression, out var toStateName, report))
                {
                    transition.ToState = toStateName;
                    
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
        
        private void ParsePayload(InvocationExpressionSyntax invocation, TransitionModel transition, StateMachineModel model, Action<string>? report)
        {
            // Handle .Payload(typeof(T)) or .Payload<T>()
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                
                // Check for typeof(T) expression
                if (arg.Expression is TypeOfExpressionSyntax typeofExpr)
                {
                    var typeSyntax = typeofExpr.Type;
                    if (_semanticModel != null)
                    {
                        var typeInfo = _semanticModel.GetTypeInfo(typeSyntax);
                        if (typeInfo.Type is INamedTypeSymbol namedType)
                        {
                            var payloadType = _typeHelper.BuildFullTypeName(namedType);
                            
                            // Check if payload was already set
                            if (!string.IsNullOrEmpty(transition.ExpectedPayloadType) && transition.ExpectedPayloadType != payloadType)
                            {
                                // Report warning for multiple payloads
                                var descriptor = DiagnosticFactory.Get(RuleIdentifiers.MultiplePayloadsOnTransition);
                                var diagnostic = Diagnostic.Create(
                                    descriptor,
                                    invocation.GetLocation(),
                                    transition.FromState,
                                    transition.Trigger,
                                    payloadType);
                                _context.ReportDiagnostic(diagnostic);
                            }
                            
                            transition.ExpectedPayloadType = payloadType;
                            
                            // Also update trigger payload map
                            if (!string.IsNullOrEmpty(transition.Trigger))
                            {
                                model.TriggerPayloadTypes[transition.Trigger] = payloadType;
                            }
                            
                            model.GenerationConfig.HasPayload = true;
                            report?.Invoke($"[FluentParser] Set payload type: {payloadType}");
                        }
                    }
                }
            }
            // For .Payload<T>() - check generic type arguments
            else if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                     memberAccess.Name is GenericNameSyntax genericName &&
                     genericName.TypeArgumentList.Arguments.Count > 0)
            {
                var typeSyntax = genericName.TypeArgumentList.Arguments[0];
                if (_semanticModel != null)
                {
                    var typeInfo = _semanticModel.GetTypeInfo(typeSyntax);
                    if (typeInfo.Type is INamedTypeSymbol namedType)
                    {
                        var payloadType = _typeHelper.BuildFullTypeName(namedType);
                        
                        // Check if payload was already set
                        if (!string.IsNullOrEmpty(transition.ExpectedPayloadType) && transition.ExpectedPayloadType != payloadType)
                        {
                            // Report warning for multiple payloads
                            var descriptor = DiagnosticFactory.Get(RuleIdentifiers.MultiplePayloadsOnTransition);
                            var diagnostic = Diagnostic.Create(
                                descriptor,
                                invocation.GetLocation(),
                                transition.FromState,
                                transition.Trigger,
                                payloadType);
                            _context.ReportDiagnostic(diagnostic);
                        }
                        
                        transition.ExpectedPayloadType = payloadType;
                        
                        // Also update trigger payload map
                        if (!string.IsNullOrEmpty(transition.Trigger))
                        {
                            model.TriggerPayloadTypes[transition.Trigger] = payloadType;
                        }
                        
                        model.GenerationConfig.HasPayload = true;
                        report?.Invoke($"[FluentParser] Set payload type (generic): {payloadType}");
                    }
                }
            }
        }
        
        private void ParseAction(InvocationExpressionSyntax invocation, TransitionModel transition, StateMachineModel model, Action<string>? report, bool isAsync)
        {

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
                        transition.ActionMethod = methodName.Identifier.Text;
                        report?.Invoke($"[FluentParser] Set action{(isAsync ? " async" : "")} method: {transition.ActionMethod}");
                        // NOTE: Delay signature analysis until after all parsing is done
                        // AnalyzeActionSignature(transition);
                        if (isAsync) transition.ActionIsAsync = true;
                    }
                }
                // Check if it's a string literal
                else if (arg.Expression is LiteralExpressionSyntax literal && 
                         literal.Token.Value is string actionName)
                {
                    transition.ActionMethod = actionName;
                    report?.Invoke($"[FluentParser] Set action{(isAsync ? " async" : "")} method: {actionName}");
                    // NOTE: Delay signature analysis until after all parsing is done
                    // AnalyzeActionSignature(transition);
                    if (isAsync) transition.ActionIsAsync = true;
                }

            }
        }

        private void ParseGuard(InvocationExpressionSyntax invocation, TransitionModel transition, StateMachineModel model, Action<string>? report, bool isAsync)
        {

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
                        transition.GuardMethod = methodName.Identifier.Text;
                        report?.Invoke($"[FluentParser] Set guard{(isAsync ? " async" : "")} method: {transition.GuardMethod}");
                        // NOTE: Delay signature analysis until after all parsing is done
                        // AnalyzeGuardSignature(transition);
                        if (isAsync) transition.GuardIsAsync = true;
                    }
                }
                // Check if it's a string literal
                else if (arg.Expression is LiteralExpressionSyntax literal && 
                         literal.Token.Value is string guardName)
                {
                    transition.GuardMethod = guardName;
                    report?.Invoke($"[FluentParser] Set guard{(isAsync ? " async" : "")} method: {guardName}");
                    AnalyzeGuardSignature(transition);
                    if (isAsync) transition.GuardIsAsync = true;
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
            t.ActionIsAsync = t.ActionIsAsync || sig.IsAsync; // Preserve explicit async flag from ActionAsync()
            t.ActionHasParameterlessOverload = sig.HasParameterless;
            t.ActionExpectsPayload = sig.HasPayloadOnly || sig.HasPayloadAndToken;
        }

        private void AnalyzeGuardSignature(TransitionModel t)
        {
            if (_classSymbol == null || string.IsNullOrEmpty(t.GuardMethod)) return;
            EnsureAnalyzers();
            var sig = _callbackAnalyzer!.AnalyzeCallback(_classSymbol, t.GuardMethod!, "Guard", _compilation);
            t.GuardSignature = sig;
            t.GuardIsAsync = t.GuardIsAsync || sig.IsAsync; // Preserve explicit async flag from GuardAsync()
            t.GuardHasParameterlessOverload = sig.HasParameterless;
            t.GuardExpectsPayload = sig.HasPayloadOnly || sig.HasPayloadAndToken;
        }

        private void ParsePayloadTypeAttributes(StateMachineModel model, Action<string>? report)
        {
            if (_classSymbol == null) return;

            // Class-level [PayloadType(typeof(Default))]
            var classPayloadAttrs = _classSymbol.GetAttributes()
                .Where(a => a.AttributeClass != null && _typeHelper.BuildFullTypeName(a.AttributeClass) == Strings.PayloadTypeAttributeFullName);

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
                foreach (var attr in m.GetAttributes().Where(a => a.AttributeClass != null && _typeHelper.BuildFullTypeName(a.AttributeClass) == Strings.PayloadTypeAttributeFullName))
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
        
        private void ParseOnEntry(InvocationExpressionSyntax invocation, string currentState, StateMachineModel model, Action<string>? report, bool isAsync)
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
                        // NOTE: Delay signature analysis until after all parsing is done
                        // AnalyzeOnEntrySignature(state);
                        if (isAsync) state.OnEntryIsAsync = true;
                        model.GenerationConfig.HasOnEntryExit = true;
                        report?.Invoke($"[FluentParser] Set OnEntry{(isAsync ? "Async" : "")} for {currentState}: {state.OnEntryMethod}");
                    }
                }
                // Check if it's a string literal
                else if (arg.Expression is LiteralExpressionSyntax literal && 
                         literal.Token.Value is string entryName)
                {
                    state.OnEntryMethod = entryName;
                    // NOTE: Delay signature analysis until after all parsing is done
                    // AnalyzeOnEntrySignature(state);
                    if (isAsync) state.OnEntryIsAsync = true;
                    model.GenerationConfig.HasOnEntryExit = true;
                    report?.Invoke($"[FluentParser] Set OnEntry{(isAsync ? "Async" : "")} for {currentState}: {entryName}");
                }
            }
        }
        
        private void AnalyzeOnEntrySignature(StateModel state)
        {
            if (_classSymbol == null || string.IsNullOrEmpty(state.OnEntryMethod)) return;
            EnsureAnalyzers();
            var sig = _callbackAnalyzer!.AnalyzeCallback(_classSymbol, state.OnEntryMethod!, "OnEntry", _compilation);
            state.OnEntrySignature = sig;
            state.OnEntryIsAsync = state.OnEntryIsAsync || sig.IsAsync; // Preserve explicit async flag
            state.OnEntryHasParameterlessOverload = sig.HasParameterless;
            state.OnEntryExpectsPayload = sig.HasPayloadOnly || sig.HasPayloadAndToken;
        }
        
        private void ParseOnExit(InvocationExpressionSyntax invocation, string currentState, StateMachineModel model, Action<string>? report, bool isAsync)
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
                        // NOTE: Delay signature analysis until after all parsing is done
                        // AnalyzeOnExitSignature(state);
                        if (isAsync) state.OnExitIsAsync = true;
                        model.GenerationConfig.HasOnEntryExit = true;
                        report?.Invoke($"[FluentParser] Set OnExit{(isAsync ? "Async" : "")} for {currentState}: {state.OnExitMethod}");
                    }
                }
                // Check if it's a string literal
                else if (arg.Expression is LiteralExpressionSyntax literal && 
                         literal.Token.Value is string exitName)
                {
                    state.OnExitMethod = exitName;
                    // NOTE: Delay signature analysis until after all parsing is done
                    // AnalyzeOnExitSignature(state);
                    if (isAsync) state.OnExitIsAsync = true;
                    model.GenerationConfig.HasOnEntryExit = true;
                    report?.Invoke($"[FluentParser] Set OnExit{(isAsync ? "Async" : "")} for {currentState}: {exitName}");
                }
            }
        }
        
        private void AnalyzeOnExitSignature(StateModel state)
        {
            if (_classSymbol == null || string.IsNullOrEmpty(state.OnExitMethod)) return;
            EnsureAnalyzers();
            var sig = _callbackAnalyzer!.AnalyzeCallback(_classSymbol, state.OnExitMethod!, "OnExit", _compilation);
            state.OnExitSignature = sig;
            state.OnExitIsAsync = state.OnExitIsAsync || sig.IsAsync; // Preserve explicit async flag
            state.OnExitHasParameterlessOverload = sig.HasParameterless;
            state.OnExitExpectsPayload = sig.HasPayloadOnly || sig.HasPayloadAndToken;
        }
        
        private void ParseOnException(InvocationExpressionSyntax invocation, StateMachineModel model, Action<string>? report)
        {
            report?.Invoke($"[FluentParser] ParseOnException called");

            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];

                string? methodName = null;
                // Prefer nameof(Method)
                if (arg.Expression is InvocationExpressionSyntax nameofInvocation &&
                    nameofInvocation.Expression is IdentifierNameSyntax identifier &&
                    identifier.Identifier.Text == "nameof")
                {
                    if (nameofInvocation.ArgumentList.Arguments.Count > 0 &&
                        nameofInvocation.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax methodIdent)
                    {
                        methodName = methodIdent.Identifier.Text;
                    }
                }
                else if (arg.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    // Back-compat: allow string literal
                    methodName = literal.Token.ValueText;
                }

                if (string.IsNullOrEmpty(methodName))
                {
                    var descriptor = DiagnosticFactory.Get(RuleIdentifiers.InvalidOnExceptionSignature);
                    _context.ReportDiagnostic(Diagnostic.Create(descriptor, invocation.GetLocation(), "<unknown>", "OnException", "ExceptionDirective or ValueTask<ExceptionDirective> with (ExceptionContext<TState,TTrigger>[, CancellationToken])"));
                    return;
                }

                report?.Invoke($"[FluentParser] OnException method name: {methodName}");

                // Enforce a single global handler
                if (model.ExceptionHandler != null)
                {
                    var dupDescriptor = DiagnosticFactory.Get(RuleIdentifiers.DuplicateOnExceptionHandler);
                    _context.ReportDiagnostic(Diagnostic.Create(dupDescriptor, invocation.GetLocation()));
                    return;
                }

                if (_classSymbol == null)
                {
                    // Minimal model (no validation)
                    model.ExceptionHandler = new ExceptionHandlerModel
                    {
                        MethodName = methodName!,
                        IsAsync = false,
                        AcceptsCancellationToken = false,
                        ExceptionContextClosedType = $"global::FastFsm.Exceptions.ExceptionContext<{model.StateType}, {model.TriggerType}>"
                    };
                    return;
                }

                // Find method overloads
                var overloads = _classSymbol.GetMembers(methodName!)
                    .OfType<IMethodSymbol>()
                    .Where(m => !m.IsStatic && m.DeclaredAccessibility != Accessibility.Public)
                    .ToList();

                if (!overloads.Any())
                {
                    var descriptor = DiagnosticFactory.Get(RuleIdentifiers.InvalidOnExceptionSignature);
                    _context.ReportDiagnostic(Diagnostic.Create(descriptor, invocation.GetLocation(), methodName, "OnException", "ExceptionDirective or ValueTask<ExceptionDirective> with (ExceptionContext<TState,TTrigger>[, CancellationToken])"));
                    return;
                }

                // Get state and trigger symbols for building ExceptionContext type
                var stateTypeSymbol = _compilation.GetTypeByMetadataName(model.StateType) as INamedTypeSymbol;
                var triggerTypeSymbol = _compilation.GetTypeByMetadataName(model.TriggerType) as INamedTypeSymbol;

                if (stateTypeSymbol == null || triggerTypeSymbol == null)
                {
                    // Fallback: create handler with string-based type
                    model.ExceptionHandler = new ExceptionHandlerModel
                    {
                        MethodName = methodName!,
                        IsAsync = false,
                        AcceptsCancellationToken = false,
                        ExceptionContextClosedType = $"global::FastFsm.Exceptions.ExceptionContext<{model.StateType}, {model.TriggerType}>"
                    };
                    return;
                }

                // Construct closed ExceptionContext type
                var exceptionContextOpen = _compilation.GetTypeByMetadataName("FastFsm.Exceptions.ExceptionContext`2");
                if (exceptionContextOpen == null)
                {
                    var descriptor = DiagnosticFactory.Get(RuleIdentifiers.InvalidOnExceptionSignature);
                    _context.ReportDiagnostic(Diagnostic.Create(descriptor, invocation.GetLocation(), methodName, "OnException", "ExceptionDirective or ValueTask<ExceptionDirective> with (ExceptionContext<TState,TTrigger>[, CancellationToken])"));
                    return;
                }

                var exceptionContextClosed = exceptionContextOpen.Construct(stateTypeSymbol, triggerTypeSymbol);
                var exceptionDirectiveType = _compilation.GetTypeByMetadataName("FastFsm.Exceptions.ExceptionDirective");
                var cancellationTokenType = _compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

                if (exceptionDirectiveType == null || cancellationTokenType == null)
                {
                    var descriptor = DiagnosticFactory.Get(RuleIdentifiers.InvalidOnExceptionSignature);
                    _context.ReportDiagnostic(Diagnostic.Create(descriptor, invocation.GetLocation(), methodName, "OnException", "ExceptionDirective or ValueTask<ExceptionDirective> with (ExceptionContext<TState,TTrigger>[, CancellationToken])"));
                    return;
                }

                // Find best overload
                IMethodSymbol? selectedMethod = null;

                // Priority 1: (ExceptionContext<TState,TTrigger>, CancellationToken)
                selectedMethod = overloads.FirstOrDefault(m =>
                    m.Parameters.Length == 2 &&
                    SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, exceptionContextClosed) &&
                    SymbolEqualityComparer.Default.Equals(m.Parameters[1].Type, cancellationTokenType));

                // Priority 2: (ExceptionContext<TState,TTrigger>)
                if (selectedMethod == null)
                {
                    selectedMethod = overloads.FirstOrDefault(m =>
                        m.Parameters.Length == 1 &&
                        SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, exceptionContextClosed));
                }

                if (selectedMethod == null)
                {
                    var descriptor = DiagnosticFactory.Get(RuleIdentifiers.InvalidOnExceptionSignature);
                    _context.ReportDiagnostic(Diagnostic.Create(descriptor, invocation.GetLocation(), methodName, "OnException", "ExceptionDirective or ValueTask<ExceptionDirective> with (ExceptionContext<TState,TTrigger>[, CancellationToken])"));
                    return;
                }

                // Validate return type
                bool isAsync = false;
                bool validReturnType = false;

                if (SymbolEqualityComparer.Default.Equals(selectedMethod.ReturnType, exceptionDirectiveType))
                {
                    validReturnType = true;
                    isAsync = false;
                }
                else if (selectedMethod.ReturnType is INamedTypeSymbol namedReturn &&
                         namedReturn.IsGenericType &&
                         namedReturn.ConstructedFrom.ToDisplayString() == "System.Threading.Tasks.ValueTask<TResult>" &&
                         namedReturn.TypeArguments.Length == 1 &&
                         SymbolEqualityComparer.Default.Equals(namedReturn.TypeArguments[0], exceptionDirectiveType))
                {
                    validReturnType = true;
                    isAsync = true;
                }

                if (!validReturnType)
                {
                    var descriptor = DiagnosticFactory.Get(RuleIdentifiers.InvalidOnExceptionSignature);
                    _context.ReportDiagnostic(Diagnostic.Create(descriptor, invocation.GetLocation(), methodName, "OnException", "ExceptionDirective or ValueTask<ExceptionDirective> with (ExceptionContext<TState,TTrigger>[, CancellationToken])"));
                    return;
                }

                // Success - create model
                model.ExceptionHandler = new ExceptionHandlerModel
                {
                    MethodName = methodName!,
                    IsAsync = isAsync,
                    AcceptsCancellationToken = selectedMethod.Parameters.Length == 2,
                    ExceptionContextClosedType = _typeHelper.BuildFullTypeName(exceptionContextClosed)
                };

                report?.Invoke($"[FluentParser] Successfully parsed OnException handler: {methodName}, IsAsync={isAsync}, AcceptsCancellationToken={selectedMethod.Parameters.Length == 2}");
            }
        }
        
        private void ParseChildOf(InvocationExpressionSyntax invocation, string currentState, StateMachineModel model, Action<string>? report)
        {
            if (!model.States.TryGetValue(currentState, out var state)) 
            {
                report?.Invoke($"[FluentParser] WARNING: State {currentState} not found in model when processing ChildOf");
                return;
            }
            
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                report?.Invoke($"[FluentParser] ChildOf argument type: {arg.Expression?.GetType().Name}");
                
                if (TryExtractName(arg.Expression, out var parentStateName, report))
                {
                    state.ParentState = parentStateName;
                    
                    report?.Invoke($"[FluentParser] Successfully parsed ChildOf: {currentState} is child of {parentStateName}");
                    
                    // Ensure parent state exists
                    if (!model.States.ContainsKey(parentStateName))
                    {
                        model.States[parentStateName] = new StateModel
                        {
                            Name = parentStateName,
                            OrdinalValue = 0 // Will be set later
                        };
                        report?.Invoke($"[FluentParser] Created parent state {parentStateName}");
                    }
                    
                    report?.Invoke($"[FluentParser] Set {currentState} as child of {parentStateName}");
                }
                else
                {
                    report?.Invoke($"[FluentParser] WARNING: Could not parse ChildOf argument for {currentState}. Expression: {arg.Expression}");
                }
            }
            else
            {
                report?.Invoke($"[FluentParser] WARNING: ChildOf for {currentState} has no arguments");
            }
        }
        
        private void ParseInitial(InvocationExpressionSyntax invocation, string currentState, StateMachineModel model, Action<string>? report)
        {
            if (!model.States.TryGetValue(currentState, out var state)) 
            {
                report?.Invoke($"[FluentParser] WARNING: State {currentState} not found in model when processing Initial");
                return;
            }
            
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                report?.Invoke($"[FluentParser] Initial argument type: {arg.Expression?.GetType().Name}");
                
                if (TryExtractName(arg.Expression, out var initialStateName, report))
                {
                    state.InitialChildState = initialStateName;
                    
                    report?.Invoke($"[FluentParser] Successfully parsed Initial: {initialStateName} is initial child of {currentState}");
                    
                    // Ensure initial state exists and mark it
                    if (!model.States.ContainsKey(initialStateName))
                    {
                        model.States[initialStateName] = new StateModel
                        {
                            Name = initialStateName,
                            OrdinalValue = 0 // Will be set later
                        };
                        report?.Invoke($"[FluentParser] Created initial state {initialStateName}");
                    }
                    
                    // Mark the child state as initial
                    if (model.States.TryGetValue(initialStateName, out var childState))
                    {
                        childState.IsInitial = true;
                        childState.ParentState = currentState; // Also set parent relationship
                        report?.Invoke($"[FluentParser] Marked {initialStateName} as initial and set parent to {currentState}");
                    }
                    
                    report?.Invoke($"[FluentParser] Set {initialStateName} as initial child of {currentState}");
                }
                else
                {
                    report?.Invoke($"[FluentParser] WARNING: Could not parse Initial argument for {currentState}. Expression: {arg.Expression}");
                }
            }
            else
            {
                report?.Invoke($"[FluentParser] WARNING: Initial for {currentState} has no arguments");
            }
        }
        
        private void ParseHistory(string currentState, StateMachineModel model, Action<string>? report, bool isShallow)
        {
            if (!model.States.TryGetValue(currentState, out var state)) return;
            
            state.HistoryModeString = isShallow ? "Shallow" : "Deep";
            // Also set the enum property for compatibility
            state.History = isShallow ? Generator.Model.HistoryMode.Shallow : Generator.Model.HistoryMode.Deep;
            report?.Invoke($"[FluentParser] Set {currentState} history mode to {state.HistoryModeString}");
        }
        
        private void ParsePriority(InvocationExpressionSyntax invocation, TransitionModel transition, StateMachineModel model, Action<string>? report)
        {
            if (invocation.ArgumentList.Arguments.Count == 1 &&
                invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal &&
                literal.Token.Value is int priority)
            {
                transition.Priority = priority;
                report?.Invoke($"[FluentParser] Set transition priority: {priority} for {transition.FromState} -> {transition.ToState ?? "(pending)"}");
                return;
            }
            
            // Report diagnostic for invalid priority argument
            // Using FSM202 as a placeholder - would need proper RuleIdentifier
            var descriptor = DiagnosticFactory.Get("FSM202");
            _context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                invocation.GetLocation(),
                "Priority must be an integer literal"));
        }
        
        private void ReportPriorityWithoutTransition(InvocationExpressionSyntax invocation)
        {
            // Report diagnostic for Priority() called without active transition
            // Using FSM203 as a placeholder - would need proper RuleIdentifier  
            var descriptor = DiagnosticFactory.Get("FSM203");
            _context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                invocation.GetLocation(),
                "Priority() can only be called on a transition"));
        }

        private string? GetNamespace()
        {
            // Use symbol-based namespace extraction for consistency with StateMachineParser
            if (_classSymbol != null)
            {
                return _classSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : _classSymbol.ContainingNamespace.ToDisplayString();
            }
            return string.Empty;
        }

        /// <summary>
        /// Finalizes all callback signatures after the model has been fully parsed.
        /// This ensures that the class symbol has complete information about all methods.
        /// </summary>
        private void FinalizeSignatures(StateMachineModel model, Action<string>? report)
        {
            if (_classSymbol == null)
            {
                report?.Invoke("[FluentParser] Cannot finalize signatures: class symbol is null");
                return;
            }

            EnsureAnalyzers();
            report?.Invoke($"[FluentParser] Finalizing signatures for {model.Transitions.Count} transitions and {model.States.Count} states");

            // Analyze transition actions and guards
            foreach (var transition in model.Transitions)
            {
                if (!string.IsNullOrEmpty(transition.ActionMethod))
                {
                    var sig = _callbackAnalyzer!.AnalyzeCallback(_classSymbol, transition.ActionMethod, "Action", _compilation);
                    transition.ActionSignature = sig;
                    transition.ActionIsAsync = transition.ActionIsAsync || sig.IsAsync;
                    transition.ActionHasParameterlessOverload = sig.HasParameterless;
                    transition.ActionExpectsPayload = sig.HasPayloadOnly || sig.HasPayloadAndToken;
                }

                if (!string.IsNullOrEmpty(transition.GuardMethod))
                {
                    report?.Invoke($"[FluentParser] Analyzing guard signature for: {transition.GuardMethod}");
                    var sig = _callbackAnalyzer!.AnalyzeCallback(_classSymbol, transition.GuardMethod, "Guard", _compilation);
                    transition.GuardSignature = sig;
                    transition.GuardIsAsync = transition.GuardIsAsync || sig.IsAsync;
                    transition.GuardHasParameterlessOverload = sig.HasParameterless;
                    transition.GuardExpectsPayload = sig.HasPayloadOnly || sig.HasPayloadAndToken;
                    report?.Invoke($"[FluentParser]   - HasParameterless: {sig.HasParameterless}, IsAsync: {sig.IsAsync}, HasPayload: {sig.HasPayloadOnly || sig.HasPayloadAndToken}");
                }
            }

            // Analyze state entry/exit methods
            foreach (var state in model.States.Values)
            {
                if (!string.IsNullOrEmpty(state.OnEntryMethod))
                {
                    report?.Invoke($"[FluentParser] Analyzing OnEntry signature for state {state.Name}: {state.OnEntryMethod}");
                    var sig = _callbackAnalyzer!.AnalyzeCallback(_classSymbol, state.OnEntryMethod, "OnEntry", _compilation);
                    state.OnEntrySignature = sig;
                    state.OnEntryIsAsync = state.OnEntryIsAsync || sig.IsAsync;
                    state.OnEntryHasParameterlessOverload = sig.HasParameterless;
                    state.OnEntryExpectsPayload = sig.HasPayloadOnly || sig.HasPayloadAndToken;
                    report?.Invoke($"[FluentParser]   - HasParameterless: {sig.HasParameterless}, IsAsync: {sig.IsAsync}, HasPayload: {sig.HasPayloadOnly || sig.HasPayloadAndToken}");
                }

                if (!string.IsNullOrEmpty(state.OnExitMethod))
                {
                    report?.Invoke($"[FluentParser] Analyzing OnExit signature for state {state.Name}: {state.OnExitMethod}");
                    var sig = _callbackAnalyzer!.AnalyzeCallback(_classSymbol, state.OnExitMethod, "OnExit", _compilation);
                    state.OnExitSignature = sig;
                    state.OnExitIsAsync = state.OnExitIsAsync || sig.IsAsync;
                    state.OnExitHasParameterlessOverload = sig.HasParameterless;
                    state.OnExitExpectsPayload = sig.HasPayloadOnly || sig.HasPayloadAndToken;
                    report?.Invoke($"[FluentParser]   - HasParameterless: {sig.HasParameterless}, IsAsync: {sig.IsAsync}, HasPayload: {sig.HasPayloadOnly || sig.HasPayloadAndToken}");
                }
            }

            report?.Invoke("[FluentParser] Signature finalization complete");
        }
        
        private void BuildHSMHierarchy(StateMachineModel model, Action<string>? report)
        {
            // Check if any HSM features are used
            bool hasHsmFeatures = model.States.Values.Any(s => 
                s.ParentState != null || 
                s.History != Generator.Model.HistoryMode.None || 
                s.IsInitial ||
                s.InitialChildState != null);
                
            if (hasHsmFeatures)
            {
                model.HierarchyEnabled = true;
                report?.Invoke("[FluentParser] HSM features detected, enabling hierarchy");
            }
            
            // If hierarchy is enabled but no explicit parent relationships defined,
            // try to infer from naming convention (State_SubState pattern)
            if (model.HierarchyEnabled && !hasHsmFeatures)
            {
                report?.Invoke("[FluentParser] HierarchyEnabled=true but no explicit HSM features found. Attempting to infer hierarchy from naming convention...");
                InferHierarchyFromNamingConvention(model, report);
                
                // Check again if we found any hierarchy
                hasHsmFeatures = model.States.Values.Any(s => s.ParentState != null);
            }
            
            if (!model.HierarchyEnabled)
            {
                report?.Invoke("[FluentParser] Hierarchy not enabled, skipping hierarchy building");
                return;
            }
            
            // Build parent-child relationships from StateModel data
            foreach (var state in model.States.Values)
            {
                model.ParentOf[state.Name] = state.ParentState;
                
                if (!model.ChildrenOf.ContainsKey(state.Name))
                {
                    model.ChildrenOf[state.Name] = new List<string>();
                }
                
                if (state.ParentState != null)
                {
                    // Ensure parent exists
                    if (!model.States.ContainsKey(state.ParentState))
                    {
                        // Create parent if it doesn't exist
                        model.States[state.ParentState] = new StateModel
                        {
                            Name = state.ParentState,
                            OrdinalValue = 0 // Will be set later
                        };
                        model.ParentOf[state.ParentState] = null;
                        model.ChildrenOf[state.ParentState] = new List<string>();
                    }
                    
                    // Add to parent's children list
                    if (!model.ChildrenOf.ContainsKey(state.ParentState))
                    {
                        model.ChildrenOf[state.ParentState] = new List<string>();
                    }
                    if (!model.ChildrenOf[state.ParentState].Contains(state.Name))
                    {
                        model.ChildrenOf[state.ParentState].Add(state.Name);
                    }
                }
            }
            
            // Calculate depth for each state
            foreach (var state in model.States.Keys)
            {
                model.Depth[state] = CalculateDepth(state, model.ParentOf);
            }
            
            // Populate StateModel.ChildStates from model.ChildrenOf
            foreach (var state in model.States.Values)
            {
                if (model.ChildrenOf.TryGetValue(state.Name, out var children))
                {
                    state.ChildStates = children.ToList();
                }
            }
            
            // Process initial substates from both IsInitial flags and InitialChildState properties
            foreach (var state in model.States.Values)
            {
                // Process history mode
                if (state.History != Generator.Model.HistoryMode.None)
                {
                    model.HistoryOf[state.Name] = state.History;
                }
                
                // Process initial substates - from IsInitial flag
                if (state.IsInitial && state.ParentState != null)
                {
                    if (!model.InitialChildOf.ContainsKey(state.ParentState))
                    {
                        model.InitialChildOf[state.ParentState] = state.Name;
                    }
                    // Note: multiple initial substates should be reported as error but we're not adding diagnostics here
                }
                
                // Process initial substates - from InitialChildState property on parent
                if (!string.IsNullOrEmpty(state.InitialChildState))
                {
                    model.InitialChildOf[state.Name] = state.InitialChildState;
                    
                    // Also mark the child as initial for consistency
                    if (model.States.TryGetValue(state.InitialChildState, out var childState))
                    {
                        childState.IsInitial = true;
                    }
                }
            }
            
            report?.Invoke($"[FluentParser] Hierarchy built: {model.ParentOf.Count} parent relationships, {model.ChildrenOf.Count} composite states");
        }
        
        /// <summary>
        /// Infers parent-child relationships from state naming convention (Parent_Child pattern)
        /// </summary>
        private void ValidateHsmModel(StateMachineModel model, Action<string>? report)
        {
            report?.Invoke("[FluentParser] Starting HSM validation");
            
            // 1. Check for duplicate parent declarations (state has multiple parents)
            foreach (var state in model.States.Values)
            {
                if (!string.IsNullOrEmpty(state.ParentState))
                {
                    // Check if this child is also marked as a parent of something else that creates a cycle
                    var visited = new HashSet<string>();
                    var current = state.ParentState;
                    visited.Add(state.Name);
                    
                    while (current != null)
                    {
                        if (visited.Contains(current))
                        {
                            var descriptor = DiagnosticFactory.Get("FSM206"); // CircularParent
                            _context.ReportDiagnostic(Diagnostic.Create(
                                descriptor,
                                Location.None,
                                state.Name, current));
                            break;
                        }
                        visited.Add(current);
                        current = model.States.ContainsKey(current) ? model.States[current].ParentState : null;
                    }
                }
            }
            
            // 2. Check Initial() must be child of current state
            foreach (var state in model.States.Values)
            {
                if (!string.IsNullOrEmpty(state.InitialChildState))
                {
                    // Verify that InitialChildState is actually a child of this state
                    if (model.States.TryGetValue(state.InitialChildState, out var initialChild))
                    {
                        if (initialChild.ParentState != state.Name)
                        {
                            var descriptor = DiagnosticFactory.Get("FSM201"); // InitialNotChild
                            _context.ReportDiagnostic(Diagnostic.Create(
                                descriptor,
                                Location.None,
                                state.InitialChildState, state.Name));
                        }
                    }
                }
            }
            
            // 3. Check composite states must have Initial
            foreach (var state in model.States.Values)
            {
                if (state.ChildStates != null && state.ChildStates.Any())
                {
                    // This is a composite state
                    if (string.IsNullOrEmpty(state.InitialChildState))
                    {
                        // Check if any child is marked as initial
                        bool hasInitialChild = state.ChildStates.Any(childName => 
                            model.States.ContainsKey(childName) && model.States[childName].IsInitial);
                        
                        if (!hasInitialChild)
                        {
                            var descriptor = DiagnosticFactory.Get("FSM204"); // MissingInitialForComposite
                            _context.ReportDiagnostic(Diagnostic.Create(
                                descriptor,
                                Location.None,
                                state.Name));
                        }
                    }
                }
            }
            
            // 4. Check history on leaf states
            foreach (var state in model.States.Values)
            {
                if (state.History != Generator.Model.HistoryMode.None)
                {
                    // Check if this is a leaf state (no children)
                    if (state.ChildStates == null || !state.ChildStates.Any())
                    {
                        var descriptor = DiagnosticFactory.Get("FSM205"); // HistoryOnLeaf
                        _context.ReportDiagnostic(Diagnostic.Create(
                            descriptor,
                            Location.None,
                            state.Name));
                    }
                }
            }
            
            // 5. Check for multiple Initial() declarations for the same parent
            var initialsByParent = new Dictionary<string, List<string>>();
            foreach (var state in model.States.Values)
            {
                if (state.IsInitial && !string.IsNullOrEmpty(state.ParentState))
                {
                    if (!initialsByParent.ContainsKey(state.ParentState))
                    {
                        initialsByParent[state.ParentState] = new List<string>();
                    }
                    initialsByParent[state.ParentState].Add(state.Name);
                }
            }
            
            foreach (var kvp in initialsByParent)
            {
                if (kvp.Value.Count > 1)
                {
                    // Multiple initial states for the same parent
                    var descriptor = DiagnosticFactory.Get("FSM207"); // MultipleInitialsPerParent
                    var parentState = model.States.ContainsKey(kvp.Key) ? model.States[kvp.Key] : null;
                    _context.ReportDiagnostic(Diagnostic.Create(
                        descriptor,
                        Location.None,
                        kvp.Key, string.Join(", ", kvp.Value)));
                }
            }
            
            report?.Invoke("[FluentParser] HSM validation complete");
        }
        
        /// <summary>
        /// Extracts name from State.Name or Trigger.Name patterns.
        /// Handles both MemberAccessExpressionSyntax (State.Name) and simple identifiers.
        /// </summary>
        private bool TryExtractName(ExpressionSyntax? expression, out string name, Action<string>? report = null)
        {
            name = string.Empty;
            
            if (expression == null)
            {
                report?.Invoke("[FluentParser] TryExtractName: expression is null");
                return false;
            }
            
            // Handle State.Name or Trigger.Name pattern
            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                name = memberAccess.Name.Identifier.Text;
                report?.Invoke($"[FluentParser] TryExtractName: extracted '{name}' from MemberAccess");
                return true;
            }
            
            // Handle simple identifier
            if (expression is IdentifierNameSyntax identifier)
            {
                name = identifier.Identifier.Text;
                report?.Invoke($"[FluentParser] TryExtractName: extracted '{name}' from Identifier");
                return true;
            }
            
            // Handle nameof(Method) pattern
            if (expression is InvocationExpressionSyntax invocation &&
                invocation.Expression is IdentifierNameSyntax id &&
                id.Identifier.Text == "nameof" &&
                invocation.ArgumentList.Arguments.Count > 0)
            {
                return TryExtractName(invocation.ArgumentList.Arguments[0].Expression, out name, report);
            }
            
            report?.Invoke($"[FluentParser] TryExtractName: unsupported expression type {expression.GetType().Name}");
            return false;
        }
        
        private void InferHierarchyFromNamingConvention(StateMachineModel model, Action<string>? report)
        {
            var stateNames = model.States.Keys.OrderBy(s => s.Length).ToList();
            
            foreach (var stateName in stateNames)
            {
                // Check if state name contains underscore
                var underscoreIndex = stateName.IndexOf('_');
                if (underscoreIndex > 0)
                {
                    // Extract potential parent name
                    var potentialParentName = stateName.Substring(0, underscoreIndex);
                    
                    // Check if parent state exists
                    if (model.States.ContainsKey(potentialParentName))
                    {
                        var childState = model.States[stateName];
                        var parentState = model.States[potentialParentName];
                        
                        // Only set parent if not already set (explicit definitions take precedence)
                        if (childState.ParentState == null)
                        {
                            childState.ParentState = potentialParentName;
                            report?.Invoke($"[FluentParser] Inferred parent relationship: {stateName} -> {potentialParentName}");
                            
                            // If this is the first child and parent has no initial child, set as initial
                            if (parentState.InitialChildState == null)
                            {
                                // Check if the child name ends with common initial patterns
                                var childSuffix = stateName.Substring(underscoreIndex + 1);
                                if (childSuffix.Equals("Initializing", StringComparison.OrdinalIgnoreCase) ||
                                    childSuffix.Equals("Initial", StringComparison.OrdinalIgnoreCase) ||
                                    childSuffix.Equals("Start", StringComparison.OrdinalIgnoreCase) ||
                                    childSuffix.Equals("Begin", StringComparison.OrdinalIgnoreCase))
                                {
                                    parentState.InitialChildState = stateName;
                                    childState.IsInitial = true;
                                    report?.Invoke($"[FluentParser] Set {stateName} as initial child of {potentialParentName}");
                                }
                            }
                        }
                    }
                }
            }
            
            // After inferring all relationships, set initial children for parents that don't have one
            foreach (var stateName in model.States.Keys)
            {
                var state = model.States[stateName];
                var children = model.States.Values.Where(s => s.ParentState == stateName).ToList();
                
                if (children.Any() && state.InitialChildState == null)
                {
                    // Set the first child (in enum order) as initial if no explicit initial is set
                    var firstChild = children.OrderBy(c => c.OrdinalValue).First();
                    state.InitialChildState = firstChild.Name;
                    firstChild.IsInitial = true;
                    report?.Invoke($"[FluentParser] Auto-set {firstChild.Name} as initial child of {stateName} (first in enum order)");
                }
            }
        }
        
        private int CalculateDepth(string state, Dictionary<string, string?> parentOf)
        {
            int depth = 0;
            var current = state;
            while (parentOf.TryGetValue(current, out var parent) && parent != null)
            {
                depth++;
                current = parent;
                // Prevent infinite loops
                if (depth > 100) break;
            }
            return depth;
        }
    }
}
