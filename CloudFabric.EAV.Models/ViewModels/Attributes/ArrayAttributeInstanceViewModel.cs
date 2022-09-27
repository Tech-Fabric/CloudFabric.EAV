using System.Collections.Generic;
using CloudFabric.EAV.Service.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Service.Models.RequestModels.Attributes
{
    public class ArrayAttributeInstanceViewModel: AttributeInstanceViewModel
    {
        public List<AttributeInstanceViewModel> Items { get; set; }
    }
}