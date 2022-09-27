using CloudFabric.EAV.Json.Utilities;

using System.Collections.Generic;

namespace CloudFabric.EAV.Service.Models.RequestModels.Attributes
{
    public class ArrayAttributeInstanceCreateUpdateRequest: AttributeInstanceCreateUpdateRequest
    {
        public List<AttributeInstanceCreateUpdateRequest> Items { get; set; }
    }
}