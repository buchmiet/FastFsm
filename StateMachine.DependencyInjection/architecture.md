Great — I’ll analyze the current state of the `FastFsm` repository on the `main` branch, with a focus on how the functionality is now split across three NuGet packages: `FastFsm.Net`, `FastFsm.Net.Logging`, and `FastFsm.Net.DependencyInjection`. I’ll then fully update the markdown documentation to clearly reflect this structure, including detailed explanations of how each package works and integrates.

I’ll let you know once the updated document is ready.


# FastFsm Architectural Documentation

## 1. Core Philosophy & Goals

FastFsm is a .NET library for creating Finite State Machines (FSMs). Its design is driven by core principles aimed at addressing the shortcomings of traditional reflection-based or dictionary-heavy FSM libraries, especially in performance-critical, resource-constrained, or modern .NET environments. The primary goals are:

* **🚀 Maximum Performance:** Achieve near-native code speed for state transitions by eliminating runtime reflection, dictionary lookups, and dynamic dispatch. The transition logic should be as cheap as a `switch` statement on enums, allowing the JIT/AOT compiler to optimize heavily (e.g. via jump tables).

* **🗑️ Zero Allocations (in hot path):** State transitions should not allocate memory on the managed heap, preventing garbage collector pressure in high-throughput scenarios.

* **🛡️ AOT & Trimming Safety:** The library must be fully compatible with Ahead-of-Time (AOT) compilation and aggressive linker trimming. This makes it suitable for modern deployment models like Native AOT, Blazor WebAssembly, and mobile/IoT applications. There are no hidden reflection calls or dynamic code generation that would break under AOT or get stripped out by the linker.

* **✨ Superb Developer Experience:** Defining a state machine should be declarative, intuitive, and type-safe. The library should provide compile-time feedback and require minimal boilerplate from the user. Misconfigurations (like undefined transitions) are caught at compile time rather than runtime.

* **🧩 Modular & Pay-for-Play:** Core functionality is lean and has no heavy dependencies. Advanced features like logging, dependency injection, or complex state behaviors are opt-in. Users who don’t need a feature shouldn’t pay the cost (in performance or binary size) for it. FastFsm achieves this by splitting these features into separate packages that can be included as needed.

## 2. High-Level Architecture

FastFsm is composed of several components, each packaged separately to enforce the modular, pay-for-play philosophy:

1. **FastFsm.Abstractions:** A small .NET Standard 2.0 class library containing only the attribute definitions (e.g. `[StateMachine]`, `[Transition]`) needed to define an FSM. This wide compatibility ensures it can be referenced by any .NET project (including older frameworks or projects where you only want to include annotations). It has **no external dependencies**.

2. **FastFsm.Generator:** A C# Source Generator (Roslyn analyzer) that is the heart of the library. It analyzes user code decorated with attributes from the Abstractions library and generates high-performance C# implementations of the state machines. The generator is distributed as an analyzer and is included with the main FastFsm package (see below), so users typically don’t reference this directly.

3. **FastFsm.Net (Core StateMachine Runtime):** The main runtime and tooling package. This is a .NET library that references FastFsm.Abstractions and includes the FastFsm.Generator as an analyzer. It provides base classes (e.g. `StateMachineBase<TState, TTrigger>`), interfaces (e.g. `IStateMachine<TState, TTrigger>` and any generated state machine interfaces), and helper code needed at runtime. Crucially, this **core package has no dependency on logging or dependency injection** libraries – it’s a pure state machine engine. By default, logging and DI integration are stripped out to keep it lean.

4. **FastFsm.Net.Logging:** An **optional** *overlay* package that enables built-in logging for state transitions. This package is a bit unusual in that it contains **no compiled DLL** of its own – it leverages a clever build-time trick to inject logging into the state machine code **only if you opt-in by referencing this package**. It adds a reference to the `Microsoft.Extensions.Logging.Abstractions` library and uses MSBuild to signal the source generator to weave in logging functionality (detailed in Section 4). If you don’t need logging, you simply omit this package and no logging code or dependencies will be included.

5. **FastFsm.Net.DependencyInjection:** An **optional** extension package that provides integration with `Microsoft.Extensions.DependencyInjection` (DI). This allows you to register state machine instances or factories in a DI container with a single line, facilitating use in ASP.NET Core, console apps with Generic Host, etc. The DI package contains the minimal code needed for integration (such as extension methods for `IServiceCollection` and any supporting classes) and references the `Microsoft.Extensions.DependencyInjection.Abstractions` library. Like the logging package, it is entirely opt-in – if you don’t need DI support (e.g. on a tiny IoT device or a simple app), you won’t incur any of its overhead. Notably, the DI package **also enables the logging features** behind the scenes (it references the Logging.Abstractions and sets the same build flags as FastFsm.Net.Logging) so that if you are in a typical app using DI, you automatically get logging instrumentation in your state machines as well. (If you include the DI package, you do not need to separately include the Logging package – the DI package activates logging for you.)

This separation of core, logging, and DI ensures a clean dependency graph. The core FastFsm.Net package remains lightweight and free of unwanted dependencies, suitable for high-performance and trimmed environments. Developers can then mix and match the overlay packages: include FastFsm.Net.Logging if structured logging is desired, and/or FastFsm.Net.DependencyInjection if using a DI container – those packages bring in only the necessary extras. If neither is included, you get just the barebones, fastest possible state machine library. This design reinforces the "pay-for-play" principle: you only pay (in complexity, size, or dependencies) for the features you actually use.

---

## 3. The Source Generation Engine

The core innovation of FastFsm is its reliance on compile-time source generation to create the state machine logic. This yields zero runtime reflection and highly optimized code tailored to the user’s state and trigger types.

### 3.1. User-Facing API (Declaring a State Machine)

Using FastFsm starts with a developer defining a partial class and decorating it with attributes that describe the states and transitions. The API is declarative and minimizes boilerplate – the developer primarily writes enum definitions and annotates transitions. All the heavy lifting is done by the source generator.

**Example: User Code**

```csharp
// User defines enums for states and triggers
public enum OrderState { New, Submitted, Shipped }
public enum OrderTrigger { Submit, Ship }

// User defines the state machine structure using attributes
[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
public partial class OrderStateMachine
{
    // A dummy method (or multiple) to hold transition definitions via attributes
    [Transition(OrderState.New, OrderTrigger.Submit, OrderState.Submitted)]
    [Transition(OrderState.Submitted, OrderTrigger.Ship, OrderState.Shipped)]
    private void ConfigureTransitions() { }
}
```

In the above example, the user defines an `OrderStateMachine` with two possible triggers (`Submit` and `Ship`). The `[StateMachine]` attribute marks the class as a state machine with specific state and trigger enum types. Each `[Transition]` attribute on the `ConfigureTransitions` method declares a valid transition: e.g., from `New` state, on `Submit` trigger, move to `Submitted` state. The method itself is just a placeholder – it’s never called at runtime – but it provides a location for the source generator to attach these attributes. The use of attributes means the design is **declarative**; there’s no manual coding of the transition logic, no dictionaries or if/else chains for the developer to write.

### 3.2. Generated Implementation

The FastFsm.Generator will detect the presence of a `[StateMachine]` attribute on a partial class and generate the other part of that class behind the scenes. It reads the attributes like `[Transition]` to understand the allowed state/trigger transitions and then emits highly optimized code to implement the state machine. The key idea is that the generator translates the declarative definition into a fully static, compiled form – primarily using simple `switch` statements. This occurs at build time, so by the time your program runs, you have a compiled state machine with no need for reflection or dynamic lookup.

**Example: Generated Code (Simplified)**

```csharp
// <auto-generated/>
public partial class OrderStateMachine : StateMachineBase<OrderState, OrderTrigger>, IOrderStateMachine
{
    public OrderStateMachine(OrderState initialState = default) : base(initialState) { }

    public override bool TryFire(OrderTrigger trigger, object? payload = null)
    {
        // Core state transition logic as a nested switch
        switch (_currentState)
        {
            case OrderState.New:
                switch (trigger)
                {
                    case OrderTrigger.Submit:
                        _currentState = OrderState.Submitted;
                        return true;
                }
                break;
            case OrderState.Submitted:
                switch (trigger)
                {
                    case OrderTrigger.Ship:
                        _currentState = OrderState.Shipped;
                        return true;
                }
                break;
        }
        return false;
    }

    public override bool CanFire(OrderTrigger trigger)
    {
        // Similar switch-based logic for pre-checks (no state change)
        switch (_currentState)
        {
            case OrderState.New:
                return trigger == OrderTrigger.Submit;
            case OrderState.Submitted:
                return trigger == OrderTrigger.Ship;
        }
        return false;
    }

    public override IEnumerable<OrderTrigger> GetPermittedTriggers()
    {
        // Returns the list of triggers allowed in the current state
        switch (_currentState)
        {
            case OrderState.New:
                return new [] { OrderTrigger.Submit };
            case OrderState.Submitted:
                return new [] { OrderTrigger.Ship };
            // Shipped or others -> no triggers
        }
        return Array.Empty<OrderTrigger>();
    }
}
```

In this simplified generated code, the `OrderStateMachine` class now has a concrete implementation of `TryFire`, `CanFire`, and other methods. The logic is a straightforward translation of the attributes we provided:

* `TryFire` uses a nested switch on the current state and the given trigger to decide if a transition is defined. If so, it updates the state (`_currentState`) to the new state and returns true. If the trigger is not permitted in the current state, it falls through and returns false.
* `CanFire` simply checks, without changing state, whether a given trigger is valid in the current state.
* `GetPermittedTriggers` returns which triggers are valid from the current state.

Because all this logic is determined at compile time, the JIT or AOT compiler can optimize these switches heavily. In many cases, the JIT will convert a switch on an `enum` into a jump table, making state transitions extremely fast — essentially just an array index and a jump, plus the assignment for state update. This is as fast as you can reasonably get in managed code for a dynamic state machine.

### 3.3. Architectural Benefits

* **Performance:** The generated code is just plain C# with direct control flow. There are no dictionary lookups, no reflection, and no virtual dispatch on transitions (beyond the single virtual call to `TryFire` itself, which you typically call on a concrete class or via an interface). This means a state transition is essentially the cost of a couple of compares and a jump – comparable to an optimized `switch`/`case` you might write by hand. It’s significantly faster than approaches involving reflection or data-driven tables, and it’s on par with hand-written state machines.

* **Zero Allocations:** Looking at the generated code, there are no heap allocations during a transition. The state enum and trigger enum are value types. The switch and comparisons don’t allocate. We return a bool or an array (which in the case of `GetPermittedTriggers` is a short-lived array of triggers or an empty array singleton). In typical usage, even `GetPermittedTriggers` could be optimized further by the generator (for example, it could return a cached static array per state to avoid allocating a new array on each call). The key point is that nothing in `TryFire` or `CanFire` allocates memory, so you can fire triggers in tight loops without generating garbage.

* **AOT/Trimming Safety:** Since the implementation is pure static C# code with no runtime reflection on user types, it’s 100% compatible with AOT and linking. The generator knows all possible states and triggers at compile time, so the output code references everything explicitly. AOT compilation (like .NET Native or Xamarin iOS AOT or .NET 8 NativeAOT) can ahead-of-time compile the state machine with no dynamic behavior. Likewise, the linker (trimmer) sees all the code and will strip out anything not used. There’s no need to mark things as preserved via reflection, etc., because we don’t use reflection.

* **Type Safety:** Misuse of the state machine is largely caught at compile time. Since you define states and triggers as `enum` types (or you could use `int`/`string` but enums are typical), the generator ensures you only use valid combinations. For example, if you put an attribute `[Transition(OrderState.Shipped, OrderTrigger.Ship, ...)]` on a state that doesn’t actually exist in your enum, the code simply won’t compile (the attribute constructor would not match). The generator can also produce compile-time diagnostics if it detects something logically inconsistent (like duplicate transitions or unreachable states). This all means that, unlike many traditional FSM implementations, a FastFsm state machine is **correct by construction** – if your project compiles, the state machine is guaranteed not to have missing transition handlers for defined transitions.

* **Minimal Boilerplate:** The developer doesn’t write the state-handling logic; they just declare it. This saves a lot of repetitive code and potential for bugs. The pattern of `[StateMachine]` on a class and `[Transition]` on methods is straightforward and easy to follow. There’s no fluent API or configuration object to fiddle with – just simple attributes.

Importantly, all these benefits are achieved while still allowing advanced features (logging, DI, etc.) to layer on without breaking the core optimizations. The source generator focuses solely on core state machine logic; additional features are introduced in a way that doesn’t disturb this logic unless explicitly enabled by the user (as we’ll see next).

---

## 4. The Conditional Logging Overlay

One of the more sophisticated parts of FastFsm’s architecture is how it provides **optional** logging of state transitions. We wanted to offer built-in, high-performance, structured logging (using the `Microsoft.Extensions.Logging` abstractions) so that every state transition could be logged for audit or debugging purposes. However, we had a challenge: adding logging should not compromise the goals of performance, zero allocations, or compatibility with trimming/AOT for users who don’t need it. In other words, if a user doesn’t care about logging, the presence of logging code or dependencies in the state machine should be zero.

### 4.1. The Challenge

Logging in .NET (via `Microsoft.Extensions.Logging`) typically involves injecting an `ILogger` and calling methods like `_logger.LogInformation("Transition from {State} to {State}", fromState, toState)`. If we baked this into the state machine, it would force every user to take a dependency on the Logging.Abstractions DLL and also possibly incur runtime costs (even if logging is a no-op, just having the calls can add slight overhead). It would also complicate AOT in environments where `Microsoft.Extensions.Logging` might get trimmed out if unused. We needed a way to **inject logging only when it’s wanted**.

### 4.2. The Solution: Multi-Stage MSBuild & Source-Gen Activation

The solution is an innovative use of MSBuild properties and conditional compilation symbols in combination with the source generator. It involves orchestrating three pieces: the core package, the logging “activator” package, and the source generator’s conditional code emission. Here’s how it works step by step:

**Step 1: A Build Property Hook in the Core Package**

The core FastFsm.Net package defines an MSBuild property that acts as a global switch for logging generation. Specifically, in the core package’s `.props` file (which is automatically imported during the build of any project that references FastFsm.Net), we define a property `FsmGenerateLogging` with a default value of `false`. For example, **build/FastFsm.Net.props** might contain:

```xml
<Project>
  <PropertyGroup>
    <!-- Define a property to control logging generation. Default is false. -->
    <FsmGenerateLogging Condition="'$(FsmGenerateLogging)' == ''">false</FsmGenerateLogging>
  </PropertyGroup>
  <ItemGroup>
    <!-- Make this property visible to analyzers (source generator can read it) -->
    <CompilerVisibleProperty Include="FsmGenerateLogging" />
  </ItemGroup>
</Project>
```

By setting `CompilerVisibleProperty`, we ensure the source generator can query the value of `FsmGenerateLogging` at compile time. Out of the box, without any other packages, this property is `false` – meaning the generator will produce state machine code *without* any logging instrumentation.

**Step 2: The Logging Activator Package (FastFsm.Net.Logging)**

The FastFsm.Net.Logging package is essentially a toggle. Its job is to flip `FsmGenerateLogging` to `true` and bring in the logging dependency. The package’s `.csproj` is configured in a special way:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- Do not output any DLL from this package -->
    <IncludeBuildOutput>false</IncludeBuildOutput>

    <!-- When this package is installed, turn on logging generation -->
    <FsmGenerateLogging>true</FsmGenerateLogging>
    <!-- Define a compile-time symbol so shared source can detect logging -->
    <DefineConstants>$(DefineConstants);FSM_LOGGING_ENABLED</DefineConstants>
  </PropertyGroup>

  <ItemGroup>
    <!-- Bring in the Logging.Abstractions dependency for the consumer project -->
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="..." />
    <!-- Also depend on the core FastFsm.Net package -->
    <PackageReference Include="FastFsm.Net" Version="..." />
  </ItemGroup>

  <ItemGroup>
    <!-- Include a props file that will be merged into the consuming project's build -->
    <None Include="build\FastFsm.Net.Logging.props" Pack="true" PackagePath="buildTransitive" />
  </ItemGroup>
</Project>
```

Key points about this setup:

* `IncludeBuildOutput>false` means this NuGet package will not include any compiled library from the project itself. In fact, the FastFsm.Net.Logging project likely has no \*.cs files at all (or only minimal ones not producing a DLL). It exists primarily to manipulate the build.
* Setting `FsmGenerateLogging>true` here overrides the default false from the core package. When you install FastFsm.Net.Logging, the MSBuild properties imported from this package will set `FsmGenerateLogging` to true for that project.
* Defining the constant `FSM_LOGGING_ENABLED` ensures that if there are any conditional `#if` blocks in source (for example, in shared source files we’ll discuss) related to logging, they will be included.
* The package brings in `Microsoft.Extensions.Logging.Abstractions`. This is the only binary dependency needed for logging. It’s a small library (mostly interface definitions for `ILogger` etc., and some no-op logger implementations) and is safe for any environment.
* The package also references FastFsm.Net (core). This ensures that installing the logging package also pulls in the core package (so the user doesn’t have to reference both manually).

Additionally, the FastFsm.Net.Logging NuGet includes a `buildTransitive` props file (referenced in the `<None Include=... PackagePath="buildTransitive" />` line). In our case, we set the same properties in that props file as shown above (this is a common technique to ensure the MSBuild property is set in the consuming project even if the package is a transitive dependency). The end result is that **when a user adds a reference to FastFsm.Net.Logging, it flips the global `FsmGenerateLogging` switch to true in that project**.

> **Note:** The FastFsm.Net.DependencyInjection package performs a similar activation step. It also sets `FsmGenerateLogging` to true and defines `FSM_LOGGING_ENABLED` (because if you’re using DI, we assume you’d want logging as well). In other words, any optional package that needs logging will turn this switch on. The source generator doesn’t care *which* package set it – it just checks the final value of the property. So whether you include the Logging package, the DI package, or both, logging will be enabled exactly once.

**Step 3: Conditional Code Generation & Compilation**

Now, with the groundwork laid by MSBuild properties, the source generator and runtime code adapt accordingly:

* **Source Generator Behavior:** The FastFsm.Generator (running as a Roslyn analyzer during compilation) queries the `FsmGenerateLogging` property via the compiler’s MSBuild API. If the property is true, the generator emits an augmented version of the state machine code. Specifically, it injects an `ILogger` field into the partial class, modifies the constructor to accept an `ILogger` (or to obtain one, e.g., via DI or a logger factory), and inserts calls to a logging method at appropriate points in `TryFire` (and possibly on state entry/exit if that was part of the design). If the property is false, none of this logging-related code is generated – the state machine will have no reference to any `ILogger` and no logging calls, keeping it minimal. This means the presence of logging code in the output is entirely under the control of that MSBuild switch.

* **Shared Source (Extension Methods) Adaptation:** Aside from the generated state machine class code, FastFsm includes some shared source files that are added to the user’s project for utility functions. One example is a static `Log` helper or the `ExtensionRunner` (which handles external “extension” triggers or other runtime features of the state machine). These shared source files are included in the consuming project as linked code (discussed in Section 5 below). They contain conditional sections like `#if FSM_LOGGING_ENABLED` to output logging information. Because the FastFsm.Net.Logging package (or DI package) defines that symbol, those sections become active and call into `ILogger` as needed. If logging is not enabled, those code paths simply disappear at compile time.

All the pieces now fall into place seamlessly:

* **Users of FastFsm.Net (core only)** get a **lean, dependency-free** state machine library. No logging, no DI, no extra fluff – just the core functionality.
* **Users who add FastFsm.Net.Logging** get rich, structured logging injected into their state machines automatically. The state machine will log each transition (for example, using `ILogger.LogInformation` or similar with details of states and triggers) with essentially zero effort on the user’s part. Yet, because of how it’s implemented, this comes with **minimal performance overhead** and no allocations. If logging is turned off (log level raised or no logger configured), the overhead is just an if-check; if logging is on, the overhead is an extremely optimized, source-generated logging call (akin to using `LoggerMessage.Define` under the hood).
* **Users who add FastFsm.Net.DependencyInjection** will automatically have logging enabled as well (since the DI package sets the same switch). So they too get the benefits of structured logging. The DI integration itself is pay-for-play, which we will detail in the next section.
* The design ensures that **opting into logging or DI does not degrade the core state machine performance significantly**. When logging is enabled, the generated logging calls are still quite efficient (using static, pre-defined log message templates via source generation). And when logging is off, the JIT can even eliminate some of the logging code paths entirely. In summary, you get instrumentation when you want it and zero cost when you don’t.

This approach to optional logging is relatively advanced, but it achieves the best of both worlds: **the core engine remains ultra-fast and clean for those who need raw speed, while enterprise or application developers who need observability can get it with a single package reference**.

---

## 5. The ExtensionRunner.cs Shared Source Strategy

One tricky aspect of mixing optional features like logging into the library is handling pieces of *runtime* code that aren’t generated per state machine, but still need to adapt based on the presence of those features. In FastFsm, `ExtensionRunner.cs` is an example of such a piece. This is a helper that manages any “extensions” to the state machine’s behavior at runtime (for example, running entry/exit actions or other side effects when states change – in a fuller feature set, you might have such extensions).

The problem was: how can a file within the FastFsm.Net library conditionally include logging (or other optional behavior) without forcing multiple versions of the DLL? We didn’t want to ship two variants of FastFsm.Net.dll (one with logging code, one without). The solution was to use a **shared source** inclusion via NuGet `contentFiles`, combined with the compiler constants approach shown earlier.

### 5.1. Packaging Shared Source

In the FastFsm.Net (core) NuGet package, the `ExtensionRunner.cs` file is not compiled into the FastFsm.Net.dll at build time. Instead, it’s packaged as a source file to be included in the consumer’s project. The `.csproj` for FastFsm.Net contains instructions like:

```xml
<ItemGroup>
  <!-- Exclude ExtensionRunner.cs from the core DLL compilation -->
  <Compile Remove="Runtime\Extensions\ExtensionRunner.cs" />

  <!-- Package ExtensionRunner.cs as a content file for consumers, with BuildAction=Compile -->
  <None Include="Runtime\Extensions\ExtensionRunner.cs"
        Pack="true"
        PackagePath="contentFiles/cs/any"
        BuildAction="Compile"
        Visible="false" />
</ItemGroup>
```

Here’s what this accomplishes:

* The `Compile Remove` line ensures that when we build FastFsm.Net.dll, we do **not** include `ExtensionRunner.cs`. So the core assembly doesn’t have that code baked in (which avoids the issue of having logging calls compiled in when logging might not be wanted).
* The `<None Include=... contentFiles/cs/any BuildAction=Compile>` line means that the source file is embedded in the NuGet package as a content file. When a user installs FastFsm.Net, NuGet will automatically add `ExtensionRunner.cs` to the project, as a source file that gets compiled as part of the user’s project (the `BuildAction=Compile` and `contentFiles` designation handle this).

Because `ExtensionRunner.cs` is compiled as part of the user’s project, it will see whatever compilation constants are defined in that project. Notably, if `FSM_LOGGING_ENABLED` is defined (by the Logging or DI package), then inside `ExtensionRunner.cs` any code within `#if FSM_LOGGING_ENABLED` blocks will be included. If the symbol is not defined, those blocks are omitted. Thus, `ExtensionRunner` can contain logging logic (such as calls to the static Log helper or `ILogger`) guarded by that `#if`.

For example, `ExtensionRunner.cs` might have:

```csharp
public static class ExtensionRunner
{
    public static void RunTransitionExtensions(IStateMachine sm, TState fromState, TState toState, TTrigger trigger)
    {
        // ... (pseudo-code for running extension behaviors)
#if FSM_LOGGING_ENABLED
        Log.Transition(sm, fromState, toState, trigger);
#endif
    }
}
```

If logging is enabled, the call to `Log.Transition` (which would use an ILogger internally) is compiled in; if not, it’s as if that line doesn’t exist.

This shared-source approach is powerful: it lets us ship one core package, but that package’s behavior can be adapted at compile-time based on which optional packages are present. We don’t need multiple versions of the core DLL for different feature combinations. Instead, we inject or remove functionality at build time in the consumer’s project.

### 5.2. Why Shared Source?

One might wonder why we needed to go the route of shared source for something like `ExtensionRunner`. Couldn’t we simply include all possible code in the core and maybe throw `Debug.Assert` or no-op out if not used? The reason is that including even a reference to types like `ILogger` in the core DLL could be problematic for trimming or AOT. Even if the code isn’t actively used, a naive linker might see the reference and include the Logging.Abstractions assembly, defeating our goal of no unwanted dependencies. By using shared source with conditional compilation, we ensure that if logging is off, the compiled output has **zero** references to any logging types or members. It’s a way of achieving a form of “C++-like” conditional compilation in C# via the build.

Additionally, this technique allowed us to handle the dependency injection separation cleanly as well. In the next section, we’ll discuss how the DI integration was similarly made optional without complicating the core library.

---

## 6. Optional Dependency Injection Integration

Originally, the FastFsm.Net core package included built-in support for Microsoft.Extensions.DependencyInjection (DI) – for example, helper methods to register state machines into an `IServiceCollection` and a factory to create state machine instances via the DI container. While convenient, this meant that the core package took a dependency on the `Microsoft.Extensions.DependencyInjection.Abstractions` library. In ultra-lean environments (like a small IoT device or a Unity game, or any scenario not using the .NET Generic Host/DI system), that’s a dependency you might prefer to avoid. In line with our modular design philosophy, we decided to extract all DI-related code into a separate package. This refactoring is now complete: FastFsm’s DI integration is entirely optional and delivered via **FastFsm.Net.DependencyInjection**.

### 6.1. FastFsm.Net.DependencyInjection Package

The FastFsm.Net.DependencyInjection package is an extension package that provides out-of-the-box integration with the Microsoft.Extensions.DependencyInjection ecosystem. If you include this package in your project, you can easily configure state machines in your DI container. If you don’t include it, the core FastFsm library has no knowledge of DI whatsoever (no references to the DI abstractions, and no dormant code waiting to be trimmed).

**What the DI package contains:**

* **Extension Methods for IServiceCollection:** For example, it provides methods like `AddStateMachine<TStateMachine>(this IServiceCollection services, ...)` which might scan for triggers and states or utilize generated interfaces to register the state machine. In practice, the source generator creates an interface for each state machine (e.g., `IOrderStateMachine` for a class `OrderStateMachine`). The DI extension can register the concrete `OrderStateMachine` class as implementing `IOrderStateMachine` in the service container, so it can be retrieved or injected elsewhere. It may also register any internal factory or runner needed to operate the state machine within the container’s context.

* **StateMachineFactory and Support Classes:** If the architecture calls for a factory to create state machines with dependencies, the DI package includes those. For instance, if your state machine’s transitions involve calling other services or if the state machine itself needs constructor injection for an `ILogger` (when logging is enabled) or other services, the DI support code can handle wiring that up through the service provider.

* **Reference to Microsoft.Extensions.DependencyInjection.Abstractions:** The package has a NuGet dependency on the DI abstractions, so that it can use `IServiceCollection` and related types. This dependency is only brought in when you choose to use the DI package, keeping it out of the core otherwise.

* **Logging Activation:** The DI package also includes the build logic to enable logging, as noted earlier. Concretely, its build script will set `FsmGenerateLogging=true` and define `FSM_LOGGING_ENABLED` (just as FastFsm.Net.Logging does). It also depends on `Microsoft.Extensions.Logging.Abstractions`. This means that by installing FastFsm.Net.DependencyInjection, you automatically get the logging features activated in the generated state machine code. We made this design choice under the assumption that most applications using DI (e.g., ASP.NET Core apps, services, etc.) will likely also want logging. It simplifies things — you don’t need to add two packages, just the DI one. (For advanced scenarios, if someone truly wanted DI integration but absolutely no logging, we could revisit this decision, but the overhead of including Logging.Abstractions is very low, and if you never configure a logger the impact is negligible.)

**How it works under the hood:**

The DI integration doesn’t require the fancy shared-source injection that logging did, because the core state machine code doesn’t need to change for DI. All integration is done externally. The FastFsm.Net.DependencyInjection package is a normal DLL (unlike the logging package which was basically empty). It uses conventional extension method patterns. For example, it might have an implementation somewhat like:

```csharp
public static class FastFsmServiceCollectionExtensions
{
    public static IServiceCollection AddStateMachine<TStateMachine>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TStateMachine : class
    {
        // Register the state machine itself
        services.Add(new ServiceDescriptor(typeof(TStateMachine), typeof(TStateMachine), lifetime));
        // Also register the IStateMachine interface it implements (if any)
        foreach (var iface in typeof(TStateMachine).GetInterfaces())
        {
            if (typeof(IStateMachineBase).IsAssignableFrom(iface))
            {
                services.Add(new ServiceDescriptor(iface, sp => sp.GetRequiredService<TStateMachine>(), lifetime));
            }
        }
        return services;
    }
}
```

*(The above code is illustrative; the actual implementation might use source-generated information rather than reflection for performance, for instance, knowing the exact interface name via the generator.)*

This extension method would allow a user to do `services.AddStateMachine<OrderStateMachine>()`, and it would register both the concrete `OrderStateMachine` and its `IOrderStateMachine` interface in the DI container. If the state machine has dependencies (say it takes an `ILogger<OrderStateMachine>` in its constructor when logging is enabled), those will be fulfilled by DI as well, since the Logging.Abstractions provides the necessary interfaces and the user’s app likely has an `ILoggerFactory`/`ILogger<T>` setup.

Just like the logging overlay, the DI package uses an MSBuild *props* file (packaged as `buildTransitive`) to set up the compilation constants. So when you add the DI package:

* `FsmGenerateLogging` is set to `true` (activating logging in the source generator).
* `FSM_LOGGING_ENABLED` is defined (so that the `ExtensionRunner.cs` and any other shared source know to include logging calls).
* The project now has references to both Logging.Abstractions and DI.Abstractions, and of course to FastFsm.Net (core).

### 6.2. Clean Separation and Flexibility

This refactoring to separate the DI support confirms the robustness of the architecture. We were able to remove the DI code from the core FastFsm.Net library without any changes to the core state machine generation logic. The source generator did not need to know or care about DI at all – it continues to generate the same code for the state machines. All we did was carve out the convenience features (DI registration helpers) into a plugin package.

For users, this means maximum flexibility:

* If you’re in a constrained environment or simply don’t use Microsoft’s DI, you can stick to the FastFsm.Net core package (and maybe the Logging package if you want logs) and your deployment will have no reference to the DI library. Your binary stays smaller and you avoid any potential trimming concerns with unused DI code.
* If you do need DI, you get it by adding the FastFsm.Net.DependencyInjection package. You don’t have to configure anything else – just by virtue of adding it, the library’s source generator knows to include logging, and you have the extension methods ready to go. The core remains as performant as ever; the DI package doesn’t slow down the state machine logic, it only makes initial setup easier.
* This modular design also means we could extend FastFsm with other opt-in features in the future (for example, a hypothetical `FastFsm.Net.Analytics` or `FastFsm.Net.Monitoring` package) without cluttering the core.

In summary, FastFsm’s architecture is structured around one core engine and a set of optional feature packages. Each package cleanly layers on new functionality via compile-time switches and extension hooks, rather than via runtime polymorphism or configuration. This achieves an **architectural “holy grail”** for library design: you get the **performance and simplicity of a single-purpose library** with the **extensibility of a full-featured framework**, all without forcing unwanted baggage on the user. The successful separation of Logging and Dependency Injection into opt-in packages validates the soundness of this approach and provides a template for any future enhancements to follow the same modular pattern.
