using System;
using System.Collections.Generic;
using System.Linq;

namespace Generator.ModernGeneration.Registries
{
    /// <summary>
    /// Rejestr metod - zapobiega duplikatom sygnatur
    /// </summary>
    public sealed class MethodRegistry
    {
        private readonly Dictionary<string, MethodSignature> _methods;

        public MethodRegistry()
        {
            _methods = new Dictionary<string, MethodSignature>();
        }

        public void Register(MethodSignature method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            var key = method.GetSignatureKey();
            if (_methods.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    $"Method with signature '{key}' is already registered");
            }

            _methods.Add(key, method);
        }

        public bool IsRegistered(string signatureKey)
        {
            return _methods.ContainsKey(signatureKey);
        }

        public IEnumerable<MethodSignature> GetAll()
        {
            return _methods.Values;
        }
    }

    /// <summary>
    /// Reprezentuje sygnaturę metody
    /// </summary>
    public class MethodSignature
    {
        public string Name { get; }
        public string ReturnType { get; }
        public List<ParameterInfo> Parameters { get; }
        public string Visibility { get; }
        public bool IsAsync { get; }
        public bool IsOverride { get; }

        public MethodSignature(
            string name,
            string returnType,
            string visibility = "public",
            bool isAsync = false,
            bool isOverride = false)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
            Visibility = visibility;
            IsAsync = isAsync;
            IsOverride = isOverride;
            Parameters = new List<ParameterInfo>();
        }

        public void AddParameter(string type, string name, string defaultValue = null)
        {
            Parameters.Add(new ParameterInfo(type, name, defaultValue));
        }

        public string GetSignatureKey()
        {
            var paramTypes = string.Join(",", Parameters.Select(p => p.Type));
            return $"{Name}({paramTypes})";
        }

        public class ParameterInfo
        {
            public string Type { get; }
            public string Name { get; }
            public string DefaultValue { get; }

            public ParameterInfo(string type, string name, string defaultValue = null)
            {
                Type = type ?? throw new ArgumentNullException(nameof(type));
                Name = name ?? throw new ArgumentNullException(nameof(name));
                DefaultValue = defaultValue;
            }
        }
    }
}