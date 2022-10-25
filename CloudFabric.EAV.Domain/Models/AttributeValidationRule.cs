using CloudFabric.EAV.Json.Utilities;

using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CloudFabric.EAV.Domain.Models
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeValidationRule>))]
    public abstract class AttributeValidationRule
    {
        public abstract string ValidationError { get; }
        public abstract Task<bool> Validate(object value);

        public AttributeValidationRule()
        {
            
        }
    }
}
