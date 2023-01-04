using System;
using System.Collections.Generic;
using CloudFabric.EAV.Models.ViewModels.Attributes;


namespace CloudFabric.EAV.Models.ViewModels.EAV
{
    public class EntityInstanceViewModel
    {
        public Guid Id { get; set; }
        
        public Guid EntityConfigurationId { get; set; }
        
        public List<AttributeInstanceViewModel> Attributes { get; set; }
        
        public Guid? TenantId { get; set; }

        public string PartitionKey { get; set; }
    }
}