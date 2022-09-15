using System.Collections.Generic;
using Nastolkino.Service.Models.ViewModels.EAV.Attributes;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class ArrayAttributeInstanceViewModel: AttributeInstanceViewModel
    {
        public List<AttributeInstanceViewModel> Items { get; set; }
    }
}