using CloudFabric.EAV.Json.Utilities;
using CloudFabric.EAV.Service.Models.ViewModels.Attributes;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudFabric.EAV.Service.Models.ViewModels.EAV
{
    public class EntityConfigurationViewModel
    {
        public Guid Id { get; set; }
        
        public List<LocalizedStringViewModel> Name { get; set; }
        
        public string MachineName { get; set; }
        
        public List<AttributeConfigurationViewModel> Attributes { get; set; }
    }
}