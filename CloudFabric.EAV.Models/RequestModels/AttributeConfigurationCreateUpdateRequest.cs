using Nastolkino.Data.Enums;
using Nastolkino.Json.Utilities;

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
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
