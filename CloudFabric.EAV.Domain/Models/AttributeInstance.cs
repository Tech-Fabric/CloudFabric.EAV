using CloudFabric.EAV.Json.Utilities;

using System.Text.Json.Serialization;

namespace CloudFabric.EAV.Domain.Models
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeInstance>))]
    public abstract class AttributeInstance
    {
        public string ConfigurationAttributeMachineName { get; init; }

        public abstract object? GetValue();
    }
}