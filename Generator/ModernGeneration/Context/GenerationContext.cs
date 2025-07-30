using System;
using System.Collections.Generic;
using System.Linq;
using Generator.Model;
using Generator.ModernGeneration.Registries;
using Generator.ModernGeneration.Policies;
using Generator.ModernGeneration.Features;
using Generator.ModernGeneration.Hooks;
using IndentedStringBuilder;

namespace Generator.ModernGeneration.Context
{
    /// <summary>
    /// Główny kontekst generacji – trzyma stan, rejestry oraz narzędzia
    /// wykorzystywane przez wszystkie moduły podczas budowania kodu maszyny.
    /// </summary>
    public sealed class GenerationContext
    {
        // ───────────────────────────────────── Public API ─────────────────────────────────────

        public StateMachineModel Model { get; }
        public IndentedStringBuilder.IndentedStringBuilder Sb { get; }

        // Rejestry
        public UsingsCollector Usings { get; }
        public FieldRegistry Fields { get; }
        public MethodRegistry Methods { get; }

        // Polityki (ustawiane przez etap konfiguracji Director‑a)
        public IAsyncPolicy AsyncPolicy { get; private set; } = default!;
        public IGuardPolicy GuardPolicy { get; private set; } = default!;
        public IHookDispatchPolicy HookPolicy { get; private set; } = default!;

        /// <summary>Dispatcher hooków – moduły rejestrują w nim emitery kodu.</summary>
        public HookDispatcher Hooks { get; }

        /// <summary>Lista wszystkich zarejestrowanych modułów funkcjonalnych.</summary>
        public IReadOnlyList<IFeatureModule> Modules { get; private set; }

        // ───────────────────────────────────── Internal state ──────────────────────────────────

        private readonly Dictionary<string, List<string>> _codeBuffers;

        // ───────────────────────────────────── Constructor ─────────────────────────────────────

        public GenerationContext(StateMachineModel model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Sb = new IndentedStringBuilder.IndentedStringBuilder();

            Usings = new UsingsCollector();
            Fields = new FieldRegistry();
            Methods = new MethodRegistry();
            Hooks = new HookDispatcher();

            _codeBuffers = new Dictionary<string, List<string>>();
            Modules = new List<IFeatureModule>();
        }

        // ───────────────────────────────────── Configuration ──────────────────────────────────

        public void SetPolicies(IAsyncPolicy asyncPolicy,
                                IGuardPolicy guardPolicy,
                                IHookDispatchPolicy hookPolicy)
        {
            AsyncPolicy = asyncPolicy ?? throw new ArgumentNullException(nameof(asyncPolicy));
            GuardPolicy = guardPolicy ?? throw new ArgumentNullException(nameof(guardPolicy));
            HookPolicy = hookPolicy ?? throw new ArgumentNullException(nameof(hookPolicy));
        }

        public void RegisterModules(IEnumerable<IFeatureModule> modules)
        {
            if (modules is null) throw new ArgumentNullException(nameof(modules));
            Modules = modules.ToList();
        }

        // ───────────────────────────────────── Buffer helpers ──────────────────────────────────

        public void AddToBuffer(string bufferName, string code)
        {
            if (!_codeBuffers.TryGetValue(bufferName, out var list))
            {
                list = new List<string>();
                _codeBuffers[bufferName] = list;
            }
            list.Add(code);
        }

        public IEnumerable<string> GetBuffer(string bufferName) =>
            _codeBuffers.TryGetValue(bufferName, out var list)
                ? list
                : Enumerable.Empty<string>();

        // ───────────────────────────────────── Flush helpers ───────────────────────────────────

        public void FlushUsings()
        {
            foreach (var ns in Usings.GetSorted())
                Sb.AppendLine($"using {ns};");

            if (Usings.GetSorted().Any())
                Sb.AppendLine();
        }

        public void FlushFields()
        {
            foreach (var field in Fields.GetAll())
            {
                Sb.Append(field.Visibility);

                if (!string.IsNullOrEmpty(field.Modifiers))
                    Sb.Append($" {field.Modifiers}");

                Sb.Append($" {field.Type} {field.Name}");

                if (!string.IsNullOrEmpty(field.Initializer))
                    Sb.Append($" = {field.Initializer}");

                Sb.AppendLine(";");
            }

            if (Fields.GetAll().Any())
                Sb.AppendLine();
        }
    }
}
