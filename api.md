# FastFsm API Documentation

This document provides an overview of the FastFsm solution structure, projects, and their APIs.

## Solution Structure

```
/
├── Abstractions/
├── Benchmark/
├── Benchmark.cpp/
├── Benchmark.Java/
├── Benchmark.JavaScript/
├── Benchmark.Rust/
├── Benchmark.TypeScript/
├── Generator/
├── Generator.DependencyInjection/
├── Generator.Model/
├── Generator.Rules/
├── IndentedStringBuilder/
├── StateMachine/
├── StateMachine.Async.Tests/
├── StateMachine.DependencyInjection/
├── StateMachine.DependencyInjection.Tests/
├── StateMachine.Logging/
└── StateMachine.Tests/
```

## Project: `Abstractions`

This project contains the core attributes used to define state machines.

### `Attributes`

- **`GenerateLoggingAttribute.cs`**: Controls whether logging code should be generated for a state machine.
- **`GenerationMode.cs`**: Defines the generation mode for the state machine (e.g., `Pure`, `Basic`, `WithPayload`).
- **`HistoryMode.cs`**: Defines the history behavior for composite states in hierarchical state machines.
- **`InternalTransitionAttribute.cs`**: Defines an internal transition (no state change).
- **`OnExceptionAttribute.cs`**: Specifies a method to handle exceptions that occur during state transitions.
- **`PayloadTypeAttribute.cs`**: Specifies the payload type for a state machine or specific triggers.
- **`StateAttribute.cs`**: Defines a state and its associated `OnEntry` and `OnExit` callbacks.
- **`StateMachineAttribute.cs`**: Marks a class as a state machine and configures its core properties.
- **`TransitionAttribute.cs`**: Defines a state transition between two states.

## Project: `StateMachine`

This project contains the core runtime components of the state machine.

### `Builder`

- **`StateMachineBuilder<TState, TTrigger>`**: A builder for creating state machine instances at runtime.
  - `Build(TState initialState)`: Builds and returns a state machine instance.

### `Contracts`

- **`IExtensibleStateMachine`**: A marker interface for state machines that support extensions.
- **`IExtensibleStateMachineAsync<TState, TTrigger>`**: Asynchronous extensible state machine interface.
- **`IExtensibleStateMachineSync<TState, TTrigger>`**: Synchronous extensible state machine interface.
- **`IStateMachineAsync<TState, TTrigger>`**: Asynchronous state machine interface.
  - `TState CurrentState`: Gets the current state of the machine.
  - `bool IsStarted`: Indicates whether the state machine has been started.
  - `ValueTask StartAsync(CancellationToken cancellationToken)`: Starts the state machine.
  - `ValueTask<bool> TryFireAsync(TTrigger trigger, object? payload, CancellationToken cancellationToken)`: Tries to fire a trigger.
  - `ValueTask FireAsync(TTrigger trigger, object? payload, CancellationToken cancellationToken)`: Fires a trigger, throwing an exception if the transition is not valid.
  - `ValueTask<bool> CanFireAsync(TTrigger trigger, CancellationToken cancellationToken)`: Checks if a trigger can be fired.
  - `ValueTask<IReadOnlyList<TTrigger>> GetPermittedTriggersAsync(CancellationToken cancellationToken)`: Gets all triggers that can be fired.
- **`IStateMachineBuilder<TState, TTrigger>`**: Builder interface for compile-time configuration validation.
- **`IStateMachineContext<TState, TTrigger>`**: Generic context with state and trigger information.
- **`IStateMachineContext`**: Context passed to extensions.
- **`IStateMachineExtension`**: Extension interface for adding cross-cutting concerns.
- **`IStateMachineFactory<TStateMachine, TState, TTrigger>`**: Factory interface for creating state machines.
- **`IStateMachineSync<TState, TTrigger>`**: Synchronous state machine interface.
- **`IStateSnapshot`**: Provides a non-generic, reflection-free snapshot of a transition's key properties.

### `DependencyInjection`

- **`FsmServiceCollectionExtensions`**: Extension methods for registering state machines with a dependency injection container.
- **`StateMachineFactory<TInterface, TImplementation, TState, TTrigger>`**: A factory that selects the appropriate state machine variant.

### `Exceptions`

- **`ExceptionContext<TState, TTrigger>`**: Provides context information about an exception that occurred during a state transition.
- **`ExceptionDirective`**: Specifies how to handle exceptions in state machine callbacks.
- **`SyncCallOnAsyncMachineException`**: The exception that is thrown when a synchronous method is called on an asynchronous state machine.
- **`TransitionStage`**: Identifies the stage of a transition where an exception occurred.

### `Runtime`

- **`AsyncStateMachineBase<TState, TTrigger>`**: Base implementation for generated asynchronous state machines.
- **`ExtensionRunner`**: Executes extension hooks and logs errors.
- **`StateMachineBase<TState, TTrigger>`**: Base class providing common functionality for generated state machines.
- **`StateMachineContext<TState, TTrigger>`**: Concrete implementation of the state machine context.
- **`TransitionEntry<TState, TTrigger>`**: Represents a single transition in the state machine.
- **`TransitionResult`**: Result of a transition attempt (for internal use).

## Project: `IndentedStringBuilder`

This project provides a helper class for building indented strings.

- **`IndentedStringBuilder`**: A lightweight wrapper around `StringBuilder` that provides indentation control.
  - `IDisposable Indent()`: Increases the indentation level.
  - `IndentedStringBuilder AppendLine(string? text)`: Appends a line with the current indentation.
  - `IDisposable Block(string header)`: Creates a block with a header and curly braces.
  - `void WriteSummary(string summary)`: Writes an XML documentation summary comment.

## Project: `Generator`

This project contains the source generator for creating state machines.

- **`StateMachineGenerator`**: The main entry point for the source generator.
  ```pseudocode
  CLASS StateMachineGenerator IMPLEMENTS IIncrementalGenerator
  {
      // Methods
      PUBLIC VOID Initialize(IncrementalGeneratorInitializationContext context);
      PRIVATE STATIC BOOLEAN IsPotentialStateMachine(SyntaxNode node);
      PRIVATE STATIC ClassDeclarationSyntax? GetStateMachineClass(GeneratorSyntaxContext context);
      PRIVATE STATIC BOOLEAN IsPartial(INamedTypeSymbol classSymbol);
      PRIVATE STATIC BOOLEAN IsPotentialFsmClassWithoutAttribute(SyntaxNode node, CancellationToken _);
      PRIVATE STATIC INamedTypeSymbol? GetClassIfMissingStateMachine(GeneratorSyntaxContext context, CancellationToken _);
      PRIVATE STATIC VOID Execute(SourceProductionContext context, ... data ...);
  }
  ```

### `Analyzers`

- **`StateMachineAnalyzer`**: A Roslyn analyzer that validates state machine definitions.

### `Helpers`

- **`AsyncGenerationHelper`**: Centralizes sync/async transformations for code generation.
- **`CallbackGenerationHelper`**: Generates callback invocations with support for all variants.
- **`CallbackSignatureAnalyzer`**: Analyzes callback method signatures to determine their characteristics.
- **`DiagnosticFactory`**: Creates Roslyn diagnostic messages from rule definitions.
- **`FactoryGenerationModelBuilder`**: Creates a model for generating dependency injection factories.
- **`GuardGenerationHelper`**: Generates guard check blocks with support for all variants.
- **`TypeSystemHelper`**: Provides centralized, testable operations for type system manipulation.

### `Parsers`

- **`StateMachineParser`**: Parses state machine definitions from source code.

  ```pseudocode
  CLASS StateMachineParser
  {
      // Properties
      PRIVATE READONLY Compilation compilation;
      PRIVATE READONLY SourceProductionContext context;
      PRIVATE READONLY InvalidMethodSignatureRule _invalidMethodSignatureRule;
      PRIVATE READONLY InvalidTypesInAttributeRule _invalidTypesInAttributeRule;
      PRIVATE READONLY InvalidEnumValueInTransitionRule _invalidEnumValueRule;
      PRIVATE READONLY DuplicateTransitionRule _duplicateTransitionRule;
      PRIVATE READONLY MissingStateMachineAttributeRule _missingStateMachineAttributeRule;
      PRIVATE READONLY UnreachableStateRule _unreachableStateRule;
      PRIVATE READONLY GuardWithPayloadInNonPayloadMachineRule _guardWithPayloadRule;
      PRIVATE READONLY TypeSystemHelper _typeHelper;
      PRIVATE READONLY AsyncSignatureAnalyzer _asyncAnalyzer;
      PRIVATE READONLY HashSet<TransitionDefinition> _processedTransitionsInCurrentFsm;
      PRIVATE READONLY MixedModeRule _mixedModeRule;

      // Constructor
      PUBLIC StateMachineParser(Compilation compilation, SourceProductionContext context);

      // Methods
      PUBLIC BOOLEAN TryParse(ClassDeclarationSyntax classDeclaration, OUT StateMachineModel? model, Action<string>? report = NULL)
      {
          // 1. Initialize variables and clear state
          // 2. Get semantic model and class symbol from classDeclaration
          // 3. Create a new StateMachineModel
          // 4. Parse [StateMachine] attribute and its arguments (DefaultPayloadType, GenerateStructuralApi, etc.)
          // 5. Validate that the class is partial and has the [StateMachine] attribute
          // 6. Validate that State and Trigger types are enums
          // 7. Build the basic model with state and trigger types
          // 8. Parse all member attributes ([Transition], [InternalTransition], [State], [OnException], [PayloadType])
          // 9. Build the HSM hierarchy if enabled
          // 10. Determine the generation variant (Pure, Basic, WithPayload, etc.)
          // 11. Validate state reachability
          // 12. Finalize the model and return true if no critical errors occurred
      }

      PRIVATE VOID ParseMemberAttributes(INamedTypeSymbol classSymbol, StateMachineModel model, INamedTypeSymbol stateTypeSymbol, INamedTypeSymbol triggerTypeSymbol, REF BOOLEAN criticalErrorOccurred, REF BOOLEAN? isMachineAsyncMode, Action<string>? report = NULL)
      {
          // 1. Parse [PayloadType] attributes
          // 2. Parse [Transition] attributes
          // 3. Parse [InternalTransition] attributes
          // 4. Parse [State] attributes
          // 5. Parse [OnException] attribute
      }

      PRIVATE VOID ParseTransitionAttributes(INamedTypeSymbol classSymbol, StateMachineModel model, INamedTypeSymbol stateTypeSymbol, INamedTypeSymbol triggerTypeSymbol, REF BOOLEAN criticalErrorOccurred, REF BOOLEAN? isMachineAsyncMode);
      PRIVATE VOID ParseInternalTransitionAttributes(INamedTypeSymbol classSymbol, StateMachineModel model, INamedTypeSymbol stateTypeSymbol, INamedTypeSymbol triggerTypeSymbol, REF BOOLEAN criticalErrorOccurred, REF BOOLEAN? isMachineAsyncMode);
      PRIVATE VOID ParseStateAttributes(INamedTypeSymbol classSymbol, StateMachineModel model, INamedTypeSymbol stateTypeSymbol, REF BOOLEAN criticalErrorOccurred, REF BOOLEAN? isMachineAsyncMode);
      PRIVATE VOID ParseOnExceptionAttribute(INamedTypeSymbol classSymbol, StateMachineModel model, INamedTypeSymbol stateTypeSymbol, INamedTypeSymbol triggerTypeSymbol, REF BOOLEAN criticalErrorOccurred, REF BOOLEAN? isMachineAsyncMode, Action<string>? report = NULL);
      PRIVATE VOID BuildHierarchy(StateMachineModel model, REF BOOLEAN criticalErrorOccurred, Action<string>? report);
      PRIVATE BOOLEAN HasCircularDependency(STRING state, Dictionary<STRING, STRING> parentOf, HashSet<STRING> visited, HashSet<STRING> recursionStack);
      PRIVATE INT CalculateDepth(STRING state, Dictionary<STRING, STRING> parentOf);
      PRIVATE STRING? GetEnumMemberName(TypedConstant enumValueConstant, INamedTypeSymbol enumTypeSymbol, AttributeData attributeDataForLocation, REF BOOLEAN criticalErrorOccurred);
      PRIVATE VOID ParsePayloadTypeAttributes(INamedTypeSymbol classSymbol, StateMachineModel model, REF BOOLEAN criticalErrorOccurred);
      PRIVATE BOOLEAN ValidateCallbackMethodSignature(INamedTypeSymbol classSymbol, STRING methodName, STRING callbackType, AttributeData attributeData, REF BOOLEAN criticalErrorOccurred, REF BOOLEAN? isMachineAsyncMode, OUT BOOLEAN isAsync, OUT BOOLEAN expectsPayload, BOOLEAN machineHasPayload, OUT IMethodSymbol? selectedMethod, STRING? expectedPayloadType = NULL);
      PRIVATE VOID AnalyzeAndSetCallbackSignature(IMethodSymbol methodSymbol, STRING callbackType, Action<CallbackSignatureInfo> setSigAction);
      PRIVATE VOID ProcessRuleResults(IEnumerable<ValidationResult> ruleResults, Location defaultLocation, REF BOOLEAN criticalErrorOccurredFlag);
  }
  ```

### `SourceGenerators`

- **`StateMachineCodeGenerator`**: The base class for all variant generators.
  ```pseudocode
  ABSTRACT CLASS StateMachineCodeGenerator
  {
      // Properties
      PROTECTED READONLY StateMachineModel Model;
      PROTECTED IndentedStringBuilder Sb;
      PROTECTED READONLY TypeSystemHelper TypeHelper;
      PROTECTED READONLY BOOLEAN IsAsyncMachine;
      PROTECTED BOOLEAN ShouldGenerateLogging;
      PROTECTED HashSet<STRING> AddedUsings;

      // Constructor
      PUBLIC StateMachineCodeGenerator(StateMachineModel model);

      // Methods
      PUBLIC VIRTUAL STRING Generate();
      PROTECTED ABSTRACT VOID WriteNamespaceAndClass();
      PROTECTED VIRTUAL VOID WriteHeader();
      PROTECTED VIRTUAL VOID WriteTransitionLogic(TransitionModel transition, STRING stateTypeForUsage, STRING triggerTypeForUsage);
      PROTECTED VIRTUAL VOID WriteGuardCheck(TransitionModel transition, STRING stateTypeForUsage, STRING triggerTypeForUsage);
      PROTECTED VIRTUAL VOID WriteActionCall(TransitionModel transition);
      PROTECTED VIRTUAL VOID WriteOnEntryCall(StateModel state, STRING? expectedPayloadType);
      PROTECTED VIRTUAL VOID WriteOnExitCall(StateModel fromState, STRING? expectedPayloadType);
      // ... and many other helper methods for code generation
  }
  ```
- **`CoreVariantGenerator`**: Generates code for the "Core" and "Basic" variants.
  ```pseudocode
  CLASS CoreVariantGenerator INHERITS StateMachineCodeGenerator
  {
      // Constructor
      PUBLIC CoreVariantGenerator(StateMachineModel model);

      // Methods
      PROTECTED OVERRIDE VOID WriteNamespaceAndClass();
      PROTECTED OVERRIDE VOID WriteGetPermittedTriggersMethod(STRING stateTypeForUsage, STRING triggerTypeForUsage);
      PROTECTED OVERRIDE VOID WriteCanFireMethod(STRING stateTypeForUsage, STRING triggerTypeForUsage);
      // ... and other private methods for generating specific parts of the code
  }
  ```
- **`ExtensionsFeatureWriter`**: Writes code for the extensions feature.
  ```pseudocode
  CLASS ExtensionsFeatureWriter
  {
      // Methods
      PUBLIC VOID WriteFields(IndentedStringBuilder sb);
      PUBLIC VOID WriteConstructorBody(IndentedStringBuilder sb, BOOLEAN generateLogging);
      PUBLIC VOID WriteManagementMethods(IndentedStringBuilder sb);
  }
  ```
- **`ExtensionsVariantGenerator`**: Generates code for the "WithExtensions" variant.
  ```pseudocode
  CLASS ExtensionsVariantGenerator INHERITS StateMachineCodeGenerator
  {
      // Properties
      PRIVATE READONLY ExtensionsFeatureWriter _ext;

      // Constructor
      PUBLIC ExtensionsVariantGenerator(StateMachineModel model);

      // Methods
      PROTECTED OVERRIDE VOID WriteNamespaceAndClass();
      // ... and other private/protected methods for generating specific parts of the code
  }
  ```
- **`FullVariantGenerator`**: Generates code for the "Full" variant (Payloads + Extensions).
  ```pseudocode
  CLASS FullVariantGenerator INHERITS PayloadVariantGenerator
  {
      // Properties
      PRIVATE READONLY ExtensionsFeatureWriter _ext;

      // Constructor
      PUBLIC FullVariantGenerator(StateMachineModel model);

      // Methods
      PROTECTED OVERRIDE VOID WriteNamespaceAndClass();
      // ... and other protected methods for handling extension hooks
  }
  ```
- **`PayloadVariantGenerator`**: Generates code for variants with payloads.
  ```pseudocode
  CLASS PayloadVariantGenerator INHERITS StateMachineCodeGenerator
  {
      // Constructor
      PUBLIC PayloadVariantGenerator(StateMachineModel model);

      // Methods
      PROTECTED OVERRIDE VOID WriteNamespaceAndClass();
      PROTECTED VIRTUAL VOID WriteTryFireMethods(STRING stateTypeForUsage, STRING triggerTypeForUsage);
      // ... and many other protected methods for generating payload-related code
  }
  ```
- **`VariantSelector`**: Determines the appropriate generation variant based on the state machine's features.
  ```pseudocode
  CLASS VariantSelector
  {
      // Methods
      PUBLIC VOID DetermineVariant(StateMachineModel model, INamedTypeSymbol classSymbol);
      PRIVATE VOID DetectUsedFeatures(StateMachineModel model, INamedTypeSymbol classSymbol, GenerationConfig config);
      PRIVATE GenerationVariant? GetForcedVariant(INamedTypeSymbol classSymbol, GenerationConfig config);
      PRIVATE GenerationVariant SelectVariantBasedOnFeatures(GenerationConfig config);
      PRIVATE VOID AdjustFlagsForVariant(GenerationConfig config);
  }
  ```

## Project: `Generator.Model`

This project contains the data models used by the generator.

- **`CallbackSignatureInfo`**: Describes a callback method's signature, including overloads.
- **`ExceptionHandlerModel`**: Represents an exception handler method.
- **`GenerationConfig`**: Contains configuration for the generation process.
- **`GenerationVariant`**: Defines the generation variant for a state machine.
- **`HistoryMode`**: Defines the history behavior for composite states.
- **`StateMachineModel`**: The main model representing a state machine.
- **`StateModel`**: Represents a state in the state machine.
- **`TransitionModel`**: Represents a single transition in the state machine.

### `Dtos`

- **`FactoryGenerationModel`**: Data model for the `FactoryCodeGenerator`.
- **`TypeGenerationInfo`**: Contains pre-processed information about a type.

## Project: `Generator.Rules`

This project contains the validation rules for state machine definitions.

### `Contexts`

- Contains various context classes used by the validation rules.

### `Definitions`

- **`RuleDefinition`**: Defines a validation rule.
- **`RuleIdentifiers`**: Contains the unique identifiers for all validation rules.
- **`RuleSeverity`**: Defines the severity of a validation rule.
- **`TransitionDefinition`**: Represents a transition for validation purposes.
- **`ValidationResult`**: Represents the result of a validation rule.

### `Rules`

- Contains the implementation of all validation rules.

## Project: `Generator.DependencyInjection`

This project contains the code generator for dependency injection integration.

- **`FactoryCodeGenerator`**: Generates a factory and extension methods for the dependency injection container.