using System;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class EntityReferenceAttributeInstanceViewModel : AttributeInstanceViewModel
    {
        public Guid Value { get; set; }
    }
}