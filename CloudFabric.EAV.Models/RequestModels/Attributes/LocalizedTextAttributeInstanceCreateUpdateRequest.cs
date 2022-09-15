
using System.Collections.Generic;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class LocalizedTextAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
    {
        public List<LocalizedStringCreateRequest> Value { get; set; }
    }
}