using System.Text.Json.Serialization;
using CloudFabric.EAV.Json.Utilities;

namespace CloudFabric.EAV.Domain.Models
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeInstance>))]
    public class AttributeInstance
    {
        public string ConfigurationAttributeMachineName { get; protected set; }
    }
}