using Nastolkino.Json.Utilities;
using Nastolkino.Service.Models.RequestModels.EAV.Attributes;

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nastolkino.Service.Models.RequestModels.EAV
{
    public class EntityConfigurationCreateRequest
    {
        public List<LocalizedStringCreateRequest> Name { get; set; }
        
        public string MachineName { get; set; }

        //[JsonConverter(typeof(PolymorphicJsonConverter<List<AttributeConfigurationCreateUpdateRequest>>))]
        public List<AttributeConfigurationCreateUpdateRequest> Attributes { get; set; }
    }
}