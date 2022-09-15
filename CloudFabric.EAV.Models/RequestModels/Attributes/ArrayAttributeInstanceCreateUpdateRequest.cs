using Nastolkino.Json.Utilities;

using System.Collections.Generic;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class ArrayAttributeInstanceCreateUpdateRequest: AttributeInstanceCreateUpdateRequest
    {
        public List<AttributeInstanceCreateUpdateRequest> Items { get; set; }
    }
}