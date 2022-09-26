using CloudFabric.EAV.Service.Models.ViewModels.Attributes;

using System;
using System.Collections.Generic;


namespace CloudFabric.EAV.Service.Models.ViewModels.EAV
{
    public class EntityInstanceViewModel
    {
        public Guid Id { get; set; }
        public Guid EntityConfigurationId { get; set; }
        
        public List<AttributeInstanceViewModel> Attributes { get; set; }
    }
}