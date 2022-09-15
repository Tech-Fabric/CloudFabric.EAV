using CloudFabric.EAV.Json.Utilities;

using System.Text.Json.Serialization;

namespace CloudFabric.EAV
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeInstance>))]
    public class AttributeInstance
    {
        public string ConfigurationAttributeMachineName { get; set; }
    }
}