using System;
using Nastolkino.Service.Models.ViewModels.EAV.Attributes;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class EntityReferenceAttributeInstanceViewModel : AttributeInstanceViewModel
    {
        public Guid Value { get; set; }
    }
}