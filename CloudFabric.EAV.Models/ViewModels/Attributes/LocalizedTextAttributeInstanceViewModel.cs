using System.Collections.Generic;
using CloudFabric.EAV.Service.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Service.Models.RequestModels.Attributes
{
    public class LocalizedTextAttributeInstanceViewModel : AttributeInstanceViewModel
    {
        public List<LocalizedStringCreateRequest> Value { get; set; }
    }
}