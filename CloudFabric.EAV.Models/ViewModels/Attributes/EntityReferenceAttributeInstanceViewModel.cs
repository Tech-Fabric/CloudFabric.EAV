using System;
using CloudFabric.EAV.Service.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Service.Models.RequestModels.Attributes
{
    public class EntityReferenceAttributeInstanceViewModel : AttributeInstanceViewModel
    {
        public Guid Value { get; set; }
    }
}