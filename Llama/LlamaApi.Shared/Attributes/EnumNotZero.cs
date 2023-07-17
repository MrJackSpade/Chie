using System.ComponentModel.DataAnnotations;

namespace LlamaApi.Attributes
{
    public class EnumNotZero : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not Enum)
            {
                throw new ArgumentException($"{nameof(EnumNotZero)} can not be applied to non-enum property");
            }

            int eVal = (int)value;

            if (eVal == 0)
            {
                return new ValidationResult($"{validationContext.DisplayName} must be defined");
            }

            return ValidationResult.Success;
        }
    }
}