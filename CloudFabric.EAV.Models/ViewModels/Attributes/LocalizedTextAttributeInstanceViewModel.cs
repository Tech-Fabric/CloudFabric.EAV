using System.Collections.Generic;
using Nastolkino.Service.Models.ViewModels.EAV.Attributes;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class LocalizedTextAttributeInstanceViewModel : AttributeInstanceViewModel
    {
        public List<LocalizedStringCreateRequest> Value { get; set; }
    }
}