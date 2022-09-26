using CloudFabric.EAV.Json.Utilities;
using CloudFabric.EAV.Service.Models.RequestModels.Attributes;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudFabric.EAV.Service.Models.RequestModels.EAV
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