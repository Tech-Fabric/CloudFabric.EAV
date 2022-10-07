using System.Collections.Generic;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class LocalizedTextAttributeInstanceViewModel : AttributeInstanceViewModel
    {
        public List<LocalizedStringCreateRequest> Value { get; set; }
    }
}