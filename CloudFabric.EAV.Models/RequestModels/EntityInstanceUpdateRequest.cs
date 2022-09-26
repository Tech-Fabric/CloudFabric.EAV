using CloudFabric.EAV.Service.Models.RequestModels.Attributes;

using System;
using System.Collections.Generic;


namespace CloudFabric.EAV.Service.Models.RequestModels.EAV
{
    public class EntityInstanceUpdateRequest
    {
        public Guid Id { get; set; }
        
        public Guid EntityConfigurationId { get; set; }
        
        public List<AttributeInstanceCreateUpdateRequest> Fields { get; set; }
    }
}