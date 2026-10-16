using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AcxiomCRM.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class CloseDateNotInPastAttribute : ValidationAttribute, IClientModelValidator
{
    public CloseDateNotInPastAttribute()
    {
        ErrorMessage = "Expected Close Date cannot be in the past.";
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Let [Required] handle nulls
        if (value is DateTime dt)
        {
            return dt.Date >= DateTime.UtcNow.Date || dt.Date >= DateTime.Today;
        }
        if (DateTime.TryParse(value.ToString(), out var parsedDate))
        {
            return parsedDate.Date >= DateTime.UtcNow.Date || parsedDate.Date >= DateTime.Today;
        }
        return false;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-closedatenotinpast", ErrorMessageString);
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }
}
