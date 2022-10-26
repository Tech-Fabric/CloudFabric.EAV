
using System.Collections.Generic;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class LocalizedTextAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
    {
        public List<LocalizedStringCreateRequest> Value { get; set; }
    }
}