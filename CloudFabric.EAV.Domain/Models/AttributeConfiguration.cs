using System.Collections.Generic;
using System.Text.Json.Serialization;
using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Json.Utilities;

namespace CloudFabric.EAV.Domain.Models
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfiguration>))]
    public abstract class AttributeConfiguration
    {
        public List<LocalizedString> Name { get; protected set; }

        public List<LocalizedString> Description { get; protected set; }

        public string MachineName { get; protected set; }

        public List<AttributeValidationRule> ValidationRules { get; protected set; }

        public abstract EavAttributeType ValueType { get; }
    }
}