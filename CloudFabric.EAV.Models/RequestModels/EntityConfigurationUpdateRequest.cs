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

        public List<LocalizedStringCreateRequest> Name { get; set; }
        
        public List<EntityAttributeConfigurationCreateUpdateRequest> Attributes { get; set; }
        
        public Dictionary<string, object> Metadata { get; set; }
    }
}