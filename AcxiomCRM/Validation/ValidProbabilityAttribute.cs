using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AcxiomCRM.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class ValidProbabilityAttribute : ValidationAttribute, IClientModelValidator
{
    public ValidProbabilityAttribute()
    {
        ErrorMessage = "Probability must be between 0 and 100.";
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return false;
        if (int.TryParse(value.ToString(), out var prob))
        {
            return prob >= 0 && prob <= 100;
        }
        return false;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-validprobability", ErrorMessageString);
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }
}
