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
        
        public Dictionary<string, string> CategoryPath { get; set; }
    }
    
    public class CategoryInstanceCreateRequest
    {
        public Guid CategoryConfigurationId { get; set; }

        public Guid CategoryTreeId { get; set; }
        
        public List<AttributeInstanceCreateUpdateRequest> Attributes { get; set; }
        
        public Guid? TenantId { get; set; }
        
        public string CategoryPath { get; set; }
    }
    
    public class CategoryTreeCreateRequest
    {
        public string MachineName { get; set; }
        public Guid EntityConfigurationId { get; set; }
        public Guid? TenantId { get; set; }

    }
}