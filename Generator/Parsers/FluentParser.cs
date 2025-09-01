using System;
using System.Collections.Generic;
using System.Linq;
using Generator.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.Parsers
{
    internal class FluentParser : IStateMachineParser
    {
        private readonly Compilation _compilation;
        private readonly SourceProductionContext _context;
        private SemanticModel? _semanticModel;

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
            
            // Check if this class uses Fluent API (has Configure method)
            var configureMethod = FindConfigureMethod(classDeclaration);
            if (configureMethod == null)
            {
                report?.Invoke($"[FluentParser] No Configure() method found in {classDeclaration.Identifier.Text}");
                return false;
            }

            report?.Invoke($"[FluentParser] Found Configure() method in {classDeclaration.Identifier.Text}");

            // Get semantic model
            _semanticModel = _compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            
            // Initialize model
            model = new StateMachineModel
            {
                ClassName = classDeclaration.Identifier.Text,
                Namespace = GetNamespace(classDeclaration),
                States = new Dictionary<string, StateModel>(),
                Transitions = new List<TransitionModel>(),
                GenerationConfig = new GenerationConfig()
            };

            // Extract state and trigger types from [StateMachine] attribute
            if (!ExtractTypesFromAttribute(classDeclaration, model, report))
            {
                return false;
            }

            // Parse the Configure method body
            if (!ParseConfigureMethod(configureMethod, model, report))
            {
                return false;
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
            var stateMachineAttr = classDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(a => a.Name.ToString().Contains("StateMachine"));

            if (stateMachineAttr?.ArgumentList?.Arguments.Count >= 2)
            {
                // Extract State type
                var stateTypeArg = stateMachineAttr.ArgumentList.Arguments[0];
                if (stateTypeArg.Expression is TypeOfExpressionSyntax stateTypeOf)
                {
                    var stateType = stateTypeOf.Type.ToString();
                    model.StateType = $"{model.Namespace}.{stateType}";
                    report?.Invoke($"[FluentParser] State type: {model.StateType}");
                }

                // Extract Trigger type
                var triggerTypeArg = stateMachineAttr.ArgumentList.Arguments[1];
                if (triggerTypeArg.Expression is TypeOfExpressionSyntax triggerTypeOf)
                {
                    var triggerType = triggerTypeOf.Type.ToString();
                    model.TriggerType = $"{model.Namespace}.{triggerType}";
                    report?.Invoke($"[FluentParser] Trigger type: {model.TriggerType}");
                }

                return true;
            }

            report?.Invoke("[FluentParser] Failed to extract State/Trigger types from [StateMachine] attribute");
            return false;
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
                    }
                }
                // Check if it's a string literal
                else if (arg.Expression is LiteralExpressionSyntax literal && 
                         literal.Token.Value is string actionName)
                {
                    lastTransition.ActionMethod = actionName;
                    report?.Invoke($"[FluentParser] Set action method: {actionName}");
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
                    }
                }
                // Check if it's a string literal
                else if (arg.Expression is LiteralExpressionSyntax literal && 
                         literal.Token.Value is string guardName)
                {
                    lastTransition.GuardMethod = guardName;
                    report?.Invoke($"[FluentParser] Set guard method: {guardName}");
                }
            }
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