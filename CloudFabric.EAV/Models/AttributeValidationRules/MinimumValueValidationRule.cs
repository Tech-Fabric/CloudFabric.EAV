using System;
using System.Threading.Tasks;

namespace CloudFabric.EAV.Domain.Models.AttributeValidationRules;

public class MinimumValueValidationRule: AttributeValidationRule
{
    public MinimumValueValidationRule(float minimumValue)
    {
        MinimumValue = minimumValue;
    }

    public override string ValidationError => $"Must be greater than {MinimumValue}";
    private float MinimumValue { get; }
    public override Task<bool> Validate(object value)
    {
        var floatValue = value is float f ? f : 0;
        return Task.FromResult(floatValue >= MinimumValue);
    }
}
