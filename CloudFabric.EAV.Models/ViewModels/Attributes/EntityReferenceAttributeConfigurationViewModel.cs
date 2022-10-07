using System;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class EntityReferenceAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public Guid ReferenceEntityConfiguration { get; set; }
        
        public Guid DefaultValue { get; set; }
    }
}