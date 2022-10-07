using System;
using System.Collections.Generic;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.ViewModels
{
    public class EntityConfigurationViewModel
    {
        public Guid Id { get; set; }
        
        public List<LocalizedStringViewModel> Name { get; set; }
        
        public string MachineName { get; set; }
        
        public List<AttributeConfigurationViewModel> Attributes { get; set; }
    }
}