using System.Threading.Tasks;

namespace CloudFabric.EAV.Domain.Models.AttributeValidationRules
{
    public class RequiredAttributeValidationRule : AttributeValidationRule
    {
        public override string ValidationError => "Is Required";

        public override Task<bool> Validate(object value)
        {
            return Task.FromResult(value != null);
        }
    }
}
