using System;
using System.Collections.Generic;
using IndentedStringBuilder;

namespace Generator.ModernGeneration.Hooks
{
    /// <summary>
    /// Rejestruje i emituje fragmenty kodu przypięte do zdefiniowanych slotów.
    /// </summary>
    public sealed class HookDispatcher
    {
        private readonly Dictionary<HookSlot, List<Action<IndentedStringBuilder.IndentedStringBuilder>>> _registry = new();

        /// <summary> Moduły rejestrują swój emitter w danym slocie. </summary>
        public void Register(HookSlot slot, Action<IndentedStringBuilder.IndentedStringBuilder> emitter)
        {
            if (emitter == null) throw new ArgumentNullException(nameof(emitter));

            if (!_registry.TryGetValue(slot, out var list))
                _registry[slot] = list = new List<Action<IndentedStringBuilder.IndentedStringBuilder>>();

            list.Add(emitter);
        }

        /// <summary> Generuje całą zawartość zarejestrowaną w danym slocie. </summary>
        public void Emit(HookSlot slot, IndentedStringBuilder.IndentedStringBuilder sb)
        {
            if (sb == null) throw new ArgumentNullException(nameof(sb));
            if (_registry.TryGetValue(slot, out var list))
            {
                foreach (var emit in list)
                    emit(sb);
            }
        }
    }
}