using System;
using System.Threading.Tasks;
using CloudFabric.EAV.Domain.Models.Attributes;

namespace CloudFabric.EAV.Domain.Models.AttributeValidationRules;

public class MinimumValueValidationRule: AttributeValidationRule
{
    public MinimumValueValidationRule(float minimumValue)
    {
        MinimumValue = minimumValue;
    }

    public MinimumValueValidationRule()
    {
    }

    public override string ValidationError => $"Must be greater or equal to {MinimumValue}";
    public float MinimumValue { get; set; }
    public override Task<bool> Validate(object value)
    {
        var instance = value as NumberAttributeInstance;
        var floatValue = instance?.Value ?? 0;
        return Task.FromResult(floatValue >= MinimumValue);
    }
}
