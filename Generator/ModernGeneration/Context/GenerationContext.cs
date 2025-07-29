using System;
using System.Collections.Generic;
using System.Linq;
using Generator.Model;
using Generator.ModernGeneration.Registries;
using Generator.ModernGeneration.Policies;
using Generator.ModernGeneration.Features;
using IndentedStringBuilder;

namespace Generator.ModernGeneration.Context
{
    /// <summary>
    /// Główny kontekst generacji przechowujący stan i zasoby
    /// używane podczas generowania kodu maszyny stanów.
    /// </summary>
    public sealed class GenerationContext
    {
        public StateMachineModel Model { get; }
        public IndentedStringBuilder.IndentedStringBuilder Sb { get; }

        // Rejestry
        public UsingsCollector Usings { get; }
        public FieldRegistry Fields { get; }
        public MethodRegistry Methods { get; }

        // Polityki (na razie tylko interfejsy)
        public IAsyncPolicy AsyncPolicy { get; private set; } 
        public IGuardPolicy GuardPolicy { get; private set; } 
        public IHookDispatchPolicy HookPolicy { get; private set; } 

        // Zarejestrowane moduły (na razie pusta lista)
        public IReadOnlyList<IFeatureModule> Modules { get; private set; }

        // Bufory dla różnych części kodu
        private readonly Dictionary<string, List<string>> _codeBuffers;

        public GenerationContext(StateMachineModel model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Sb = new IndentedStringBuilder.IndentedStringBuilder();

            Usings = new UsingsCollector();
            Fields = new FieldRegistry();
            Methods = new MethodRegistry();

            _codeBuffers = new Dictionary<string, List<string>>();
            Modules = new List<IFeatureModule>();

            // Domyślne polityki będą ustawiane przez SetPolicies
        }

        public void SetPolicies(IAsyncPolicy asyncPolicy, IGuardPolicy guardPolicy, IHookDispatchPolicy hookPolicy)
        {
            AsyncPolicy = asyncPolicy ?? throw new ArgumentNullException(nameof(asyncPolicy));
            GuardPolicy = guardPolicy ?? throw new ArgumentNullException(nameof(guardPolicy));
            HookPolicy = hookPolicy ?? throw new ArgumentNullException(nameof(hookPolicy));
        }

        public void RegisterModules(IEnumerable<IFeatureModule> modules)
        {
            if (modules == null) throw new ArgumentNullException(nameof(modules));
            Modules = modules.ToList();
        }

        // Metody pomocnicze do buforowania kodu
        public void AddToBuffer(string bufferName, string code)
        {
            if (!_codeBuffers.ContainsKey(bufferName))
            {
                _codeBuffers[bufferName] = new List<string>();
            }
            _codeBuffers[bufferName].Add(code);
        }

        public IEnumerable<string> GetBuffer(string bufferName)
        {
            return _codeBuffers.ContainsKey(bufferName)
                ? _codeBuffers[bufferName]
                : Enumerable.Empty<string>();
        }

        // Helpery do generowania
        public void FlushUsings()
        {
            foreach (var ns in Usings.GetSorted())
            {
                Sb.AppendLine($"using {ns};");
            }
            if (Usings.GetSorted().Any())
            {
                Sb.AppendLine();
            }
        }

        public void FlushFields()
        {
            foreach (var field in Fields.GetAll())
            {
                Sb.Append(field.Visibility);
                if (!string.IsNullOrEmpty(field.Modifiers))
                {
                    Sb.Append(" ");
                    Sb.Append(field.Modifiers);
                }
                Sb.Append(" ");
                Sb.Append(field.Type);
                Sb.Append(" ");
                Sb.Append(field.Name);

                if (!string.IsNullOrEmpty(field.Initializer))
                {
                    Sb.Append(" = ");
                    Sb.Append(field.Initializer);
                }

                Sb.AppendLine(";");
            }

            if (Fields.GetAll().Any())
            {
                Sb.AppendLine();
            }
        }
    }

}
