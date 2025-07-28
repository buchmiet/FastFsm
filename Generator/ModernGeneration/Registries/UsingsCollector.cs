using System;
using System.Collections.Generic;
using System.Linq;

namespace Generator.ModernGeneration.Registries
{
    /// <summary>
    /// Zbiera i deduplikuje using statements
    /// </summary>
    public sealed class UsingsCollector
    {
        private readonly HashSet<string> _namespaces;
        private readonly HashSet<string> _standardNamespaces;

        public UsingsCollector()
        {
            _namespaces = new HashSet<string>();

            // Standardowe namespace'y które zawsze dodajemy na początku
            _standardNamespaces = new HashSet<string>
            {
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Runtime.CompilerServices"
            };
        }

        public void Add(string ns)
        {
            if (string.IsNullOrWhiteSpace(ns)) return;
            _namespaces.Add(ns);
        }

        public void AddRange(IEnumerable<string> namespaces)
        {
            if (namespaces == null) return;

            foreach (var ns in namespaces)
            {
                Add(ns);
            }
        }

        public IEnumerable<string> GetSorted()
        {
            // Najpierw standardowe, potem reszta alfabetycznie
            var standard = _namespaces
                .Where(ns => _standardNamespaces.Contains(ns))
                .OrderBy(ns => ns);

            var custom = _namespaces
                .Where(ns => !_standardNamespaces.Contains(ns))
                .OrderBy(ns => ns);

            return standard.Concat(custom);
        }

        public bool Contains(string ns)
        {
            return _namespaces.Contains(ns);
        }

        public void Clear()
        {
            _namespaces.Clear();
        }
    }
}