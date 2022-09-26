using CloudFabric.EAV.Service.Models.ViewModels.Attributes;

using System;

namespace CloudFabric.EAV.Service.Models.RequestModels.Attributes
{
    public class EntityReferenceAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public Guid ReferenceEntityConfiguration { get; set; }
        
        public Guid DefaultValue { get; set; }
    }
}