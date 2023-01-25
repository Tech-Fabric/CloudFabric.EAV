using System.Text.Json.Serialization;

using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Models.JsonConverters;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    [JsonConverter(typeof(AttributeInstanceRequestJsonConverter<AttributeInstanceCreateUpdateRequest>))]
    public abstract class AttributeInstanceCreateUpdateRequest
    {
        public string ConfigurationAttributeMachineName { get; set; }

        // Needed for deserialization
        public EavAttributeType ValueType { get; set; }
    }
}