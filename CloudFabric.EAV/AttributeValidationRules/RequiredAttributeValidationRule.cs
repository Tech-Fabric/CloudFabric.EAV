using System.Threading.Tasks;

namespace CloudFabric.EAV.AttributeValidationRules
{
    public class RequiredAttributeValidationRule : AttributeValidationRule
    {
        public override Task<bool> Validate(object value)
        {
            return Task.FromResult(value != null);
        }
    }
}
