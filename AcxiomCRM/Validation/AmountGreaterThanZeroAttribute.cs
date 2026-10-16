using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AcxiomCRM.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class AmountGreaterThanZeroAttribute : ValidationAttribute, IClientModelValidator
{
    public AmountGreaterThanZeroAttribute()
    {
        ErrorMessage = "Opportunity Amount must be greater than 0.";
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return false;
        if (decimal.TryParse(value.ToString(), out var val))
        {
            return val > 0;
        }
        return false;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-amountgreaterthanzero", ErrorMessageString);
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }
}
