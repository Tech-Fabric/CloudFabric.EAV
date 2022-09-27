
using System;

namespace CloudFabric.EAV.Service.Models.RequestModels.Attributes
{
    public class EntityReferenceAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
    {
        public Guid Value { get; set; }
    }
}