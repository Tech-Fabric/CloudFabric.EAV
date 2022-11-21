using System;
using System.Collections.Generic;
using CloudFabric.EAV.Models.RequestModels.Attributes;


namespace CloudFabric.EAV.Models.RequestModels
{
    public class EntityInstanceCreateRequest
    {
        public Guid EntityConfigurationId { get; set; }
        
        public List<AttributeInstanceCreateUpdateRequest> Attributes { get; set; }
        
        public Guid? TenantId { get; set; }
    }
}