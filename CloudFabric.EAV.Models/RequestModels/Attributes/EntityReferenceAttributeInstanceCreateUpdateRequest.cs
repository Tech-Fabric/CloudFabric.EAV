
using System;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class EntityReferenceAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
    {
        public Guid Value { get; set; }
    }
}