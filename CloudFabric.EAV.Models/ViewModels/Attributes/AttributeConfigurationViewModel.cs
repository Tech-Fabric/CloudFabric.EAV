using CloudFabric.EAV.Data.Enums;
using CloudFabric.EAV.Json.Utilities;

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudFabric.EAV.Service.Models.ViewModels.Attributes
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfigurationViewModel>))]
    public abstract class AttributeConfigurationViewModel
    {
        public List<LocalizedStringViewModel> Name { get; set; }

        public List<LocalizedStringViewModel> Description { get; set; }

        public string MachineName { get; set; }

        public EavAttributeType ValueType { get; set; }
    }
}