using System.Threading.Tasks;
using CloudFabric.EAV.Domain.Models.Attributes;

namespace CloudFabric.EAV.Domain.Models.AttributeValidationRules;

public class MaximumValueValidationRule: AttributeValidationRule
{
    public MaximumValueValidationRule(float maximumValue)
    {
        MaximumValue = maximumValue;
    }

    public MaximumValueValidationRule()
    {
    }

    public override string ValidationError => $"Must be less or equal to {MaximumValue}";
    public float MaximumValue { get; set; }
    public override Task<bool> Validate(object value)
    {
        var instance = value as NumberAttributeInstance;
        var floatValue = instance?.Value ?? 0;
        return Task.FromResult(floatValue <= MaximumValue);
    }
}
