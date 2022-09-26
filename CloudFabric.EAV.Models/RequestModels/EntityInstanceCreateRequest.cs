using CloudFabric.EAV.Service.Models.RequestModels.Attributes;

using System;
using System.Collections.Generic;


namespace CloudFabric.EAV.Service.Models.RequestModels.EAV
{
    public class EntityInstanceCreateRequest
    {
        public Guid EntityConfigurationId { get; set; }
        
        public List<AttributeInstanceCreateUpdateRequest> Attributes { get; set; }
    }
}