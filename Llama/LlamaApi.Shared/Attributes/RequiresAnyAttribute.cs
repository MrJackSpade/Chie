using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;

namespace LlamaApi.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RequiresAnyAttribute : ValidationAttribute
    {
        public RequiresAnyAttribute(params string[] propertyList)
        {
            this.PropertyList = propertyList;
        }

        private string[] PropertyList { get; set; }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            foreach (string propertyName in this.PropertyList)
            {
                System.Reflection.PropertyInfo? propertyInfo = validationContext.ObjectType.GetProperty(propertyName);
                object? propertyValue = propertyInfo.GetValue(validationContext.ObjectInstance, null);

                if (propertyValue != null)
                {
                    return ValidationResult.Success;
                }
            }

            List<string> jsonNames = new();

            foreach (string propertyName in this.PropertyList)
            {
                PropertyInfo? pi = validationContext.ObjectType.GetProperty(propertyName);

                if (pi is null)
                {
                    throw new ArgumentException($"Property with name {propertyName} not found");
                }

                if (pi.GetCustomAttribute<JsonPropertyNameAttribute>() is JsonPropertyNameAttribute jpn)
                {
                    jsonNames.Add(jpn.Name);
                }
                else
                {
                    jsonNames.Add(propertyName);
                }
            }

            return new ValidationResult($"At least one of the properties '{string.Join(", ", jsonNames)}' must not be null");
        }
    }
}