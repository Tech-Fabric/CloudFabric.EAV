using Nastolkino.Json.Utilities;

using System.Text.Json.Serialization;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeInstanceCreateUpdateRequest>))]
    public abstract class AttributeInstanceCreateUpdateRequest
    {
        public string ConfigurationAttributeMachineName { get; set; }
    }
}
