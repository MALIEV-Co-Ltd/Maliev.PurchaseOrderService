using System.ComponentModel.DataAnnotations;

namespace Maliev.PurchaseOrderService.Api.Attributes;

/// <summary>
/// Custom validation attribute for WHT rate according to Thailand tax regulations
/// </summary>
public class WHTRateValidationAttribute : ValidationAttribute
{
    private const decimal MaxWHTRate = 15.00m;
    private const decimal MinWHTRate = 0.00m;

    public override bool IsValid(object? value)
    {
        if (value == null)
            return true; // Allow null values

        if (value is decimal whtRate)
        {
            return whtRate >= MinWHTRate && whtRate <= MaxWHTRate;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        if (ErrorMessage != null)
            return ErrorMessage;

        // This will be overridden in the UpdatePurchaseOrderRequest
        return $"WHT rate must be between {MinWHTRate}% and {MaxWHTRate}%";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        if (value is decimal whtRate)
        {
            if (whtRate < MinWHTRate)
            {
                return new ValidationResult("WHT rate cannot be negative");
            }

            if (whtRate > MaxWHTRate)
            {
                return new ValidationResult("WHT rate cannot exceed 15% as per Thailand tax regulations");
            }

            return ValidationResult.Success;
        }

        return new ValidationResult("Invalid WHT rate format");
    }
}