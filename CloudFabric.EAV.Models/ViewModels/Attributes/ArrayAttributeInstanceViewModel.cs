using System.Collections.Generic;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class ArrayAttributeInstanceViewModel: AttributeInstanceViewModel
    {
        public List<AttributeInstanceViewModel>? Items { get; set; }
    }
}