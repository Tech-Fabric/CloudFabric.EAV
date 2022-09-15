using Nastolkino.Service.Models.ViewModels.EAV.Attributes;

using System;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class EntityReferenceAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public Guid ReferenceEntityConfiguration { get; set; }
        
        public Guid DefaultValue { get; set; }
    }
}