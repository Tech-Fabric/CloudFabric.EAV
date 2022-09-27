using CloudFabric.EAV.Data.Enums;
using CloudFabric.EAV.Data.Models.Base;
using CloudFabric.EAV.Json.Utilities;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudFabric.EAV.Data.Models
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfiguration>))]
    public abstract class AttributeConfiguration
    {
        public List<LocalizedString> Name { get; set; }

        public List<LocalizedString> Description { get; set; }

        public string MachineName { get; set; }

        public List<AttributeValidationRule> ValidationRules { get; set; }

        public abstract EavAttributeType ValueType { get; }
    }
}