using CloudFabric.EAV.Json.Utilities;

using System.Text.Json.Serialization;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeInstanceCreateUpdateRequest>))]
    public abstract class AttributeInstanceCreateUpdateRequest
    {
        public string ConfigurationAttributeMachineName { get; set; }
    }
}
