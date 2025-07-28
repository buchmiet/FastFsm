using System;
using System.Collections.Generic;
using System.Linq;

namespace Generator.ModernGeneration.Registries
{
    /// <summary>
    /// Rejestr pól klasy - zapobiega duplikatom i kontroluje kolejność
    /// </summary>
    public sealed class FieldRegistry
    {
        private readonly Dictionary<string, FieldSpec> _fields;
        private readonly List<string> _fieldOrder;

        public FieldRegistry()
        {
            _fields = new Dictionary<string, FieldSpec>();
            _fieldOrder = new List<string>();
        }

        public void Register(FieldSpec field)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));

            if (_fields.ContainsKey(field.Name))
            {
                throw new InvalidOperationException(
                    $"Field '{field.Name}' is already registered. " +
                    $"Existing: {_fields[field.Name].Type}, " +
                    $"New: {field.Type}");
            }

            _fields.Add(field.Name, field);
            _fieldOrder.Add(field.Name);
        }

        public bool TryRegister(FieldSpec field)
        {
            if (field == null) return false;

            if (_fields.ContainsKey(field.Name))
            {
                // Jeśli pole już istnieje z tym samym typem, to OK
                var existing = _fields[field.Name];
                return existing.Type == field.Type;
            }

            Register(field);
            return true;
        }

        public FieldSpec GetField(string name)
        {
            return _fields.ContainsKey(name) ? _fields[name] : null;
        }

        public IEnumerable<FieldSpec> GetAll()
        {
            // Zwracamy w kolejności rejestracji
            return _fieldOrder.Select(name => _fields[name]);
        }

        public bool Contains(string fieldName)
        {
            return _fields.ContainsKey(fieldName);
        }
    }

    /// <summary>
    /// Specyfikacja pola klasy
    /// </summary>
    public sealed class FieldSpec
    {
        public string Visibility { get; }      // private, public, protected
        public string Modifiers { get; }       // readonly, static, const
        public string Type { get; }            // string, int, etc.
        public string Name { get; }            // _fieldName
        public string Initializer { get; }     // = "value"

        public FieldSpec(
            string visibility,
            string type,
            string name,
            string modifiers = null,
            string initializer = null)
        {
            if (string.IsNullOrWhiteSpace(visibility))
                throw new ArgumentException("Visibility cannot be empty", nameof(visibility));
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("Type cannot be empty", nameof(type));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            Visibility = visibility;
            Type = type;
            Name = name;
            Modifiers = modifiers;
            Initializer = initializer;
        }

        public override string ToString()
        {
            var parts = new List<string> { Visibility };
            if (!string.IsNullOrEmpty(Modifiers))
                parts.Add(Modifiers);
            parts.Add(Type);
            parts.Add(Name);

            var result = string.Join(" ", parts);
            if (!string.IsNullOrEmpty(Initializer))
                result += $" = {Initializer}";

            return result + ";";
        }
    }
}