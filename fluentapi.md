\## Implementation Plan for FastFSM 0.7.5 (Fluent API Integration)



1\. \*\*Review Current Codebase (Branch fluent)\*\*: Start by familiarizing with the existing FastFSM code, especially the gen-upd branch. Understand how state machines are currently defined using attributes and how the Roslyn source generator processes them. Note any initial implementations or scaffolding for a fluent API in this branch. This review will guide the design to ensure the new fluent approach aligns with the existing architecture (e.g. how states, triggers, transitions, and hierarchical states (HSM) are represented internally).

2\. \*\*Design of the Fluent API Structure\*\*:  

&nbsp;   <br/>\\> Zasady (niezmienne):

3\. \\> • tylko \\`enum\\`/literały/\\`nameof\\`/\\`typeof\\` w argumentach DSL

4\. \\> • \\\*\\\*zero\\\*\\\* lambd/delegatów w DSL (akcje/guardy wyłącznie przez \\`nameof\\`)

5\. \\> • \\\*\\\*żadnej\\\*\\\* logiki imperatywnej w metodzie \\`Configure()\\` (brak \\`if/for/...\\`)

6\. \\> • wszystko zamknięte w klasie maszyny (enums, payloady, metody, DSL)

7\. \\---

8\. \\## 1) Simple FSM (minimal)

9\. \\`\\`\\`csharp

10\. \\\[StateMachine(typeof(State), typeof(Trigger))\\]

11\. public partial class SimpleMachine

12\. {

13\. public enum State { A, B }

14\. public enum Trigger { Next }

15\. private static void Configure() => FSM

16\. .State(State.A)

17\. .On(Trigger.Next).GoTo(State.B)

18\. .State(State.B);

19\. }

20\. \\`\\`\\`

21\. \\---

22\. \\## 2) FSM z akcjami i guardami

23\. \\`\\`\\`csharp

24\. \\\[StateMachine(typeof(State), typeof(Trigger))\\]

25\. public partial class GuardActionMachine

26\. {

27\. public enum State { Idle, Running, Stopped }

28\. public enum Trigger { Start, Stop }

29\. private int \\\_quota;

30\. private static void Configure() => FSM

31\. .State(State.Idle)

32\. .OnEntry(nameof(OnIdleEntry))

33\. .On(Trigger.Start).GoTo(State.Running)

34\. .Guard(nameof(HasQuota)).Action(nameof(OnStart))

35\. .State(State.Running)

36\. .On(Trigger.Stop).GoTo(State.Stopped)

37\. .Action(nameof(OnStop))

38\. .State(State.Stopped)

39\. .OnExit(nameof(OnStoppedExit));

40\. // GUARD / ACTIONS

41\. private bool HasQuota() => \\\_quota > 0;

42\. private void OnStart() { \\\_quota--; }

43\. private void OnStop() { /\\\* ... \\\*/ }

44\. private void OnIdleEntry() { /\\\* ... \\\*/ }

45\. private void OnStoppedExit() { /\\\* ... \\\*/ }

46\. }

47\. \\`\\`\\`

48\. \\---

49\. \\## 3) FSM z \\\*\\\*pojedynczym payloadem\\\*\\\* (DefaultPayloadType)

50\. \\`\\`\\`csharp

51\. \\\[StateMachine(typeof(State), typeof(Trigger), DefaultPayloadType = typeof(JobData))\\]

52\. public partial class SinglePayloadMachine

53\. {

54\. public enum State { Idle, Running }

55\. public enum Trigger { Start, Update, Stop }

56\. public sealed class JobData

57\. {

58\. public required string Id { get; init; }

59\. public int Priority { get; init; }

60\. }

61\. private int \\\_runningCount;

62\. private static void Configure() => FSM

63\. .State(State.Idle)

64\. .On(Trigger.Start).GoTo(State.Running)

65\. .Guard(nameof(CanStart)).Action(nameof(StartJob))

66\. .State(State.Running)

67\. .On(Trigger.Update).GoTo(State.Running)

68\. .Action(nameof(UpdateJob))

69\. .On(Trigger.Stop).GoTo(State.Idle)

70\. .Action(nameof(StopJob));

71\. // payload-aware guard/action signatures:

72\. private bool CanStart(JobData data) => \\\_runningCount \&lt; 4 \&\& data.Priority \&gt;= 0;

73\. private void StartJob(JobData data) { \\\_runningCount++; /\\\* ... \\\*/ }

74\. private void UpdateJob(JobData data) { /\\\* ... \\\*/ }

75\. private void StopJob() { \\\_runningCount--; /\\\* ... \\\*/ }

76\. }

77\. \\`\\`\\`

78\. \\---

79\. \\## 4) FSM z \\\*\\\*wieloma danymi\\\*\\\* (multi-payload przez rekord/struct)

80\. \\> FastFSM preferuje jeden typ payloadu — multi-payload robimy przez typ złożony.

81\. \\`\\`\\`csharp

82\. \\\[StateMachine(typeof(State), typeof(Trigger), DefaultPayloadType = typeof(OperationData))\\]

83\. public partial class MultiPayloadMachine

84\. {

85\. public enum State { Ready, Busy }

86\. public enum Trigger { Begin, Tick, End }

87\. public sealed class OperationData

88\. {

89\. public required string CorrelationId { get; init; }

90\. public required string User { get; init; }

91\. public int Attempt { get; init; }

92\. }

93\. private static void Configure() => FSM

94\. .State(State.Ready)

95\. .On(Trigger.Begin).GoTo(State.Busy)

96\. .Guard(nameof(ValidateBegin)).Action(nameof(OnBegin))

97\. .State(State.Busy)

98\. .On(Trigger.Tick).GoTo(State.Busy)

99\. .Action(nameof(OnTick))

100..On(Trigger.End).GoTo(State.Ready)

101..Action(nameof(OnEnd));

102.private bool ValidateBegin(OperationData d) => d.Attempt >= 0 \&\& d.User != null;

103.private void OnBegin(OperationData d) { /\\\* ... \\\*/ }

104.private void OnTick(OperationData d) { /\\\* ... \\\*/ }

105.private void OnEnd() { /\\\* ... \\\*/ }

106.}

107.\\`\\`\\`

108.\\---

109.\\## 5) FSM z \\\*\\\*Extensions\\\*\\\* (hooki generatora)

110.\\`\\`\\`csharp

111.\\\[StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = true)\\]

112.public partial class ExtensibleMachine

113.{

114.public enum State { S1, S2 }

115.public enum Trigger { Go }

116.private static void Configure() => FSM

117..State(State.S1)

118..On(Trigger.Go).GoTo(State.S2)

119..Action(nameof(OnGo))

120..State(State.S2);

121.private void OnGo() { /\\\* ... \\\*/ }

122.// EXTENSION HOOKS (wywoływane przez wygenerowaną bazę, nazwy kontraktowe)

123.protected void OnBeforeTransition(object ctx) { /\\\* trace start \\\*/ }

124.protected void OnGuardEvaluation(object ctx, string guardName) { /\\\* ... \\\*/ }

125.protected void OnGuardEvaluated(object ctx, string guardName, bool result) { /\\\* ... \\\*/ }

126.protected void OnAfterTransition(object ctx, bool success) { /\\\* trace end \\\*/ }

127.}

128.\\`\\`\\`

129.\\---

130.\\## 6) HSM — podstawy (parent/child + internal)

131.\\`\\`\\`csharp

132.\\\[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)\\]

133.public partial class HsmBasicMachine

134.{

135.public enum State

136.{

137.Idle,

138.Processing, // parent

139.Processing\_Loading, // child

140.Processing\_Working, // child

141.Done

142.}

143.public enum Trigger { Start, Progress, Finish }

144.private static void Configure() => FSM

145.// PARENT z Internal:

146..State(State.Processing)

147..OnInternal(Trigger.Progress).Action(nameof(LogProgress))

148.// CHILDREN + initial:

149..State(State.Processing\_Loading).Parent(State.Processing).IsInitial()

150..State(State.Processing\_Working).Parent(State.Processing)

151.// Wejście do rodzica -> auto wejdzie w Initial child:

152..State(State.Idle)

153..On(Trigger.Start).GoTo(State.Processing)

154.// Wyjście do Done (np. z Working):

155..State(State.Processing\_Working)

156..On(Trigger.Finish).GoTo(State.Done)

157..State(State.Done);

158.private void LogProgress() { /\\\* internal action, bez exit/entry \\\*/ }

159.}

160.\\`\\`\\`

161.\\---

162.\\## 7) HSM — \\\*\\\*History\\\*\\\* (Shallow \& Deep)

163.\\`\\`\\`csharp

164.\\\[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)\\]

165.public partial class HsmHistoryMachine

166.{

167.public enum State

168.{

169.Root,

170.A, A1, A2, A2a, // A – z historią

171.B

172.}

173.public enum Trigger { ToA, ToB, Next }

174.private static void Configure() => FSM

175.// Parent A z historią SHALLOW:

176..State(State.A).WithHistory(HistoryMode.Shallow)

177..State(State.A1).Parent(State.A).IsInitial()

178..State(State.A2).Parent(State.A)

179..State(State.A2a).Parent(State.A2) // zagnieżdżony potomek

180.// Root -> A; A pamięta ostatni bezpośredni child (A1 albo A2):

181..State(State.Root)

182..On(Trigger.ToA).GoTo(State.A)

183.// Przykładowa nawigacja wewnątrz A:

184..State(State.A1)

185..On(Trigger.Next).GoTo(State.A2)

186..State(State.A2)

187..On(Trigger.Next).GoTo(State.A1)

188.// Wyjście z A do B i powrót do A odtworzy shallow-history:

189..State(State.B)

190..On(Trigger.ToA).GoTo(State.A)

191.// przejście do B z dowolnego miejsca w A:

192..State(State.A2a)

193..On(Trigger.ToB).GoTo(State.B);

194.// wariant DEEP (drugi parent) – przykład alternatywny:

195.// .State(State.A).WithHistory(HistoryMode.Deep) // zapamięta najgłębszy potomek (np. A2a)

196.}

197.\\`\\`\\`

198.\\---

199.\\## 8) HSM — \\\*\\\*Priorytety przejść\\\*\\\* (parent vs child)

200.\\`\\`\\`csharp

201.\\\[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)\\]

202.public partial class HsmPriorityMachine

203.{

204.public enum State { Parent, Child, Other }

205.public enum Trigger { X }

206.private static void Configure() => FSM

207..State(State.Parent)

208.// Parent reaguje na X, ale z niższym priorytetem:

209..On(Trigger.X).GoTo(State.Other).Priority(0)

210..State(State.Child).Parent(State.Parent)

211.// Child też reaguje na X – chcemy, by WYGRAŁ child:

212..On(Trigger.X).GoTo(State.Parent).Priority(10);

213.// Semantyka: przy X będą rozważone oba przejścia;

214.// wyższy Priority decyduje o wyborze (Child wins).

215.}

216.\\`\\`\\`

217.\\---

218.\\## 9) HSM — \\\*\\\*Internal w parent + normal w child\\\*\\\* (równoległe reguły)

219.\\`\\`\\`csharp

220.\\\[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)\\]

221.public partial class HsmInternalVsChildMachine

222.{

223.public enum State { Parent, Child }

224.public enum Trigger { Ping }

225.private static void Configure() => FSM

226..State(State.Parent)

227..OnInternal(Trigger.Ping).Action(nameof(ParentPing)) // bez zmiany stanu

228..State(State.Child).Parent(State.Parent)

229..On(Trigger.Ping).GoTo(State.Child).Action(nameof(ChildPing)); // self-loop w child

230.private void ParentPing() { /\\\* ... \\\*/ }

231.private void ChildPing() { /\\\* ... \\\*/ }

232.}

233.\\`\\`\\`

234.\\---

235.\\## 10) FSM — \\\*\\\*Async\\\*\\\* akcje (ValueTask/Task) – bez payloadu

236.\\`\\`\\`csharp

237.\\\[StateMachine(typeof(State), typeof(Trigger))\\]

238.public partial class AsyncMachine

239.{

240.public enum State { Disconnected, Connecting, Connected }

241.public enum Trigger { Connect, ConnectedOk, Disconnect }

242.private static void Configure() => FSM

243..State(State.Disconnected)

244..On(Trigger.Connect).GoTo(State.Connecting).Action(nameof(BeginConnectAsync))

245..State(State.Connecting)

246..On(Trigger.ConnectedOk).GoTo(State.Connected)

247..State(State.Connected)

248..On(Trigger.Disconnect).GoTo(State.Disconnected).Action(nameof(CloseAsync));

249.private async ValueTask BeginConnectAsync()

250.{

251.await Task.Yield(); // symulacja

252.}

253.private async Task CloseAsync()

254.{

255.await Task.Yield(); // symulacja

256.}

257.}

258.\\`\\`\\`

259.\\---

260.\\## 11) HSM + \\\*\\\*Async\\\*\\\* (internal async w parent)

261.\\`\\`\\`csharp

262.\\\[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)\\]

263.public partial class HsmAsyncInternalMachine

264.{

265.public enum State { Parent, Child }

266.public enum Trigger { Tick }

267.private static void Configure() => FSM

268..State(State.Parent)

269..OnInternal(Trigger.Tick).Action(nameof(ParentTickAsync))

270..State(State.Child).Parent(State.Parent).IsInitial();

271.private async ValueTask ParentTickAsync()

272.{

273.await Task.Yield();

274.}

275.}

276.\\`\\`\\`

277.\\---

278.\\### Notatki implementacyjne DSL (stała gramatyka)

279.Dla powyższych przykładów zakładamy następujący \\\*\\\*sztywny\\\*\\\* zestaw metod (wszystkie compile-time only, runtime no-op):

280.\\\* \\\*\\\*Definicje stanów\\\*\\\*

281.\\\* \\`.State(TState)\\`

282.\\\* \\`.Parent(TState)\\` \\\*(HSM)\\\*

283.\\\* \\`.IsInitial()\\` \\\*(HSM)\\\*

284.\\\* \\`.WithHistory(HistoryMode)\\` \\\*(HSM parent)\\\*

285.\\\* \\`.OnEntry(string methodName)\\`

286.\\\* \\`.OnExit(string methodName)\\`

287.\\\* \\\*\\\*Przejścia z/do\\\*\\\*

288.\\\* \\`.On(TTrigger).GoTo(TState)\\`

289.\\\* \\`.OnInternal(TTrigger)\\` \\\*(HSM internal)\\\*

290.\\\* \\\*\\\*Modyfikatory przejść\\\*\\\*

291.\\\* \\`.Guard(string methodName)\\`

292.\\\* \\`.Action(string methodName)\\`

293.\\\* \\`.Priority(int priority)\\` \\\*(HSM konflikt parent/child)\\\*

294.Ensure the design covers \*\*all features\*\* currently supported by attributes – including hierarchical state relationships (parent/child states for HSMs), internal transitions, entry/exit actions, guard conditions, and event payloads (both single and multiple parameters). The design should also determine how the fluent definitions will be recognized by the source generator (e.g. perhaps still using a \\\[StateMachine\\] attribute to mark the class and indicate state/trigger types, or an alternative marker). Aim for a clear, intuitive API that will become the default way to define state machines, while coexisting with the attribute system.

295.\*\*Implement Fluent API Classes (FastFsm.Fluent)\*\*: Develop the classes and methods that realize the fluent interface designed in the previous step. Create a new namespace or module FastFsm.Fluent (or similar) containing the builder classes and extension methods needed for the DSL. For example, implement a StateMachineBuilder\&lt;TState, TTrigger\&gt; class that accumulates the state machine definition. Provide methods to add states (State(stateEnumValue)), mark initial state (IsInitial() or AsInitial()), define state hierarchy (AsSubstateOf(parentState) for HSM), and configure transitions (On(trigger).MoveTo(state) or similar fluent syntax). Include methods to attach \*\*guards\*\* (WithGuard(Func\&lt;bool\&gt;) or guard method names), \*\*actions\*\* (WithAction(Action) or action method names), and \*\*payload\*\* types (this might be through generic parameters or separate builder for events with data). Make sure the builder internally stores the definition (states, transitions, etc.) in a structure that can later be consumed by the source generator. Also implement support for special cases like asynchronous actions (if the generator needs to know about async Task methods, ensure the fluent API can mark or accept those). This step results in a fully functional fluent DSL in code, but not yet hooked into generation.

296.\*\*Extend the Source Generator to Parse Fluent Definitions\*\*: Modify the FastFSM Roslyn source generator so that it can detect and process the new fluent-style state machine definitions \*\*in parallel\*\* with the existing attribute approach. This likely involves updating the syntax analysis phase: in addition to scanning for attributes on classes and methods, the generator should scan for usage of the fluent API (e.g. find classes that call StateMachineBuilder or a specific fluent initialization method). Implement logic to recognize a fluent definition pattern (for example, a partial class might call a ConfigureStateMachine() method or builder in a static constructor). Once detected, parse the fluent calls to build the state machine model (states, triggers, transitions) similar to how attribute metadata was parsed. You may need to traverse the syntax tree of method bodies: identify builder method calls and their arguments (state enums, trigger enums, target states, guard/action method names, etc.). Populate the generator’s internal model with this information. Ensure that the generator can handle both approaches seamlessly – for instance, it might first gather all attribute-defined machines, then gather all fluent-defined machines, before emitting code for all. The parsing should be robust: consider using the new \*\*GenTest\*\* tool in development to continuously test parsing of fluent definitions until the generator produces a correct model (the GenTest’s lenient mode can help since the fluent builder might use types not fully resolved in isolation). Essentially, this step integrates the fluent DSL into the code generation pipeline.

297.\*\*Maintain Backward Compatibility (Parallel Attribute \& Fluent Support)\*\*: As you implement the above, ensure that the old attribute-based system remains fully functional and unchanged for existing users. The fluent implementation should be additive – \*\*no breaking changes\*\*. The source generator must support projects that use attributes, projects that use fluent, or even a mix of both styles, without conflicts. Verify that a class using fluent DSL does not require attribute annotations (aside from possibly the main \\\[StateMachine\\] attribute for types, if that’s part of the design), and vice versa. If both systems are used in one project, the generator should be able to process them concurrently. This may involve differentiating definitions by context (e.g. if a class has attribute-defined transitions, use that data; if it instead uses the builder, use that). Ensure that there are no name collisions or double-processing (the generator should not generate duplicate code). It might be useful to unify the internal representation such that whether a state machine is defined by attributes or fluent calls, they end up in the same data model for code generation. By the end of this task, the code generation output for a given state machine (its auto-generated .g.cs code) should be equivalent regardless of definition style. This guarantees that existing attribute-based machines behave exactly the same after the update, while new fluent-defined machines generate correct code too.

298.\*\*Testing the Fluent Implementation Thoroughly\*\*: Create a comprehensive test suite for the new fluent API alongside existing tests. For each kind of state machine feature, write a sample definition using the fluent API and verify that the generated code is correct:

299.\*\*Basic FSM\*\*: A simple two-state machine with a transition, defined in fluent style. Ensure the generator outputs the expected state enum handling and methods.

300.\*\*Guards \& Actions\*\*: Define transitions with guard conditions and actions (pointing to methods in the partial class) using fluent syntax, verify they appear correctly in generated code (e.g., guard methods are invoked, action methods called).

301.\*\*Payload Events\*\*: If triggers can carry data, test a machine where triggers have associated payload types (single or multiple payload parameters). Use the fluent API to specify these and ensure the generator correctly handles method signatures for actions/guards that accept payloads.

302.\*\*Hierarchical States (HSM)\*\*: Create an example with parent-child states defined via fluent API (e.g., a top-level state with a nested substate). Check that the generator preserves the hierarchy (parent state classes or state ID relationships as it does with attributes).

303.\*\*Mixed Definitions\*\*: (if applicable) have one state machine class defined with attributes and another with fluent in the same project to confirm the generator can handle both in one compilation.

304.Leverage the \*\*GenTest\*\* tool for rapid iteration: run it on test files containing fluent definitions to see generated output and diagnostics immediately. Use watch mode to tweak definitions until all generator diagnostics (in diagnostics.txt) are resolved. This will help refine the fluent parsing logic. Also ensure that in GenTest’s lenient mode (limited type info) the fluent parsing still succeeds or at least fails gracefully with informative diagnostics.

305.All existing unit tests and examples for attribute-based definitions should still pass. If any regressions appear, fix them promptly to maintain backward compatibility. By the end of this testing phase, you should have high confidence that the fluent API works for all intended use cases and does not break existing functionality.

306.\*\*Update Documentation and Examples\*\*: Revise the FastFSM documentation to incorporate the new fluent API as the primary method of defining state machines. This includes updating the README.md and any other docs or example projects:

307.Provide \*\*side-by-side examples\*\*: for instance, show a simple state machine defined using the fluent interface (as the main example in the README), and then show the equivalent using the attribute approach. Clearly label fluent as the recommended default going forward (from version 0.7.5), while noting that the attribute method is still supported for compatibility or alternate use.

308.Update code snippets in documentation for defining states, transitions, guards, etc., using the fluent syntax. Ensure to also include an example of hierarchical state definition with fluent, and how to define payloads or internal transitions fluently.

309.If the package has XML comments or a wiki, update those to describe the new FastFsm.Fluent namespace, classes, and methods. Document how to get started with fluent API, and mention that no special flags are needed – it’s integrated by default when using FastFSM 0.7.5.

310.Double-check that all example code in docs runs correctly with the new version (maybe using GenTest or a sample project to validate).

311.Emphasize that there are \*\*no breaking changes\*\* in this update: existing code using attributes will continue to work as before. The documentation should reassure users of backward compatibility, while encouraging them to try the more readable fluent style for new state machines.

312.\*\*Release Preparation and Version 0.7.5 Rollout\*\*: Finalize the update for release. Bump the library version to 0.7.5 and update any version identifiers in the code or project files. Compile a \*\*changelog or release notes\*\* that highlights the introduction of the Fluent API as the major new feature. Note that Fluent API is now the default recommended way to define state machines, with the attribute-based approach still available. Ensure all tests pass in the CI pipeline and that the NuGet package (if applicable) includes the new FastFsm.Fluent components. Once verified, publish the 0.7.5 release. After release, be prepared to handle any user feedback or bug reports related to the new feature. (If any minor issues are found, they can be addressed in a follow-up patch.) This completes the implementation, with FastFSM 0.7.5 successfully integrating the Fluent DSL alongside the existing attribute system.

