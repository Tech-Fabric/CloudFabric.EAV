using Nastolkino.Json.Utilities;
using Nastolkino.Service.Models.RequestModels.EAV.Attributes;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nastolkino.Service.Models.RequestModels.EAV
{
    public class EntityConfigurationUpdateRequest
    {
        public Guid Id { get; set; }
        
        public List<LocalizedStringCreateRequest> Name { get; set; }
        
        public string MachineName { get; set; }

        //[JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfigurationCreateUpdateRequest>))]
        public List<AttributeConfigurationCreateUpdateRequest> Fields { get; set; }
    }
}