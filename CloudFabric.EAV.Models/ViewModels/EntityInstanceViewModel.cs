﻿using System;
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
        
        public Dictionary<string, string> CategoryPath { get; set; }
        
    }
    
    public class EntityTreeInstanceViewModel
    {
        public Guid Id { get; set; }
        
        public Guid EntityConfigurationId { get; set; }
        
        public List<AttributeInstanceViewModel> Attributes { get; set; }
        
        public Guid? TenantId { get; set; }

        public string PartitionKey { get; set; }
        
        public string CategoryPath { get; set; }
        
        public List<EntityTreeInstanceViewModel> Children { get; set; }

    }
}