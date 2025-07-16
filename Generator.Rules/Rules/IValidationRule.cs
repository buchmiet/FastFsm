using System.Collections.Generic;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

public interface IValidationRule<in TContext> where TContext : class
{
    IEnumerable<ValidationResult> Validate(TContext context);
}