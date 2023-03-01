using System.Text.Json.Serialization;

using CloudFabric.EAV.Json.Utilities;

namespace CloudFabric.EAV.Domain.Models
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeInstance>))]
    public abstract class AttributeInstance
    {
        public string ConfigurationAttributeMachineName { get; protected set; }

        public abstract object? GetValue();
    }
}
