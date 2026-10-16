using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AcxiomCRM.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class IndianPhoneAttribute : ValidationAttribute, IClientModelValidator
{
    private static readonly Regex PhoneRegex = new(@"^(?:\+91[\-\s]?)?[6-9]\d{9}$", RegexOptions.Compiled);

    public IndianPhoneAttribute()
    {
        ErrorMessage = "Enter a valid phone number.";
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Let [Required] handle nulls if required
        var str = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(str)) return true;
        return PhoneRegex.IsMatch(str);
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-indianphone", ErrorMessageString);
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }
}
