using System.Text.Json.Serialization;

using CloudFabric.EAV.Json.Utilities;

namespace CloudFabric.EAV.Models.ViewModels.Attributes
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeInstanceViewModel>))]
    public class AttributeInstanceViewModel
    {
        public string ConfigurationAttributeMachineName { get; set; }
    }
}