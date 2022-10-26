using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Json.Utilities;

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfigurationCreateUpdateRequest>))]
    public abstract class AttributeConfigurationCreateUpdateRequest
    {
        public List<LocalizedStringCreateRequest> Name { get; set; }

        public List<LocalizedStringCreateRequest> Description { get; set; }

        public string MachineName { get; set; }

        public abstract EavAttributeType ValueType { get; }
    }
}
