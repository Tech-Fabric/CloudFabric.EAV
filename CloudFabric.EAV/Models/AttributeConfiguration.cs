using CloudFabric.EAV.Data.Enums;
using CloudFabric.EAV.Data.Models.Base;
using CloudFabric.EAV.Json.Utilities;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudFabric.EAV.Data.Models
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfiguration>))]
    public abstract class AttributeConfiguration: Model 
    {
        public List<LocalizedString> Name { get; protected set; }

        public List<LocalizedString> Description { get; protected set; }

        public string MachineName { get; protected set; }

        public List<AttributeValidationRule> ValidationRules { get; protected set; }

        public abstract EavAttributeType ValueType { get; }
    }
}