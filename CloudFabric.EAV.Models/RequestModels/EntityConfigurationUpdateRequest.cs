using CloudFabric.EAV.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CloudFabric.EAV.Models.RequestModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels
{
    public class EntityConfigurationUpdateRequest
    {
        public Guid Id { get; set; }
        
        public string PartitionKey { get; set; }

        public List<LocalizedStringCreateRequest> Name { get; set; }
        
        public string MachineName { get; set; }

        //[JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfigurationCreateUpdateRequest>))]
        public List<AttributeConfigurationCreateUpdateRequest> Attributes { get; set; }
    }
}