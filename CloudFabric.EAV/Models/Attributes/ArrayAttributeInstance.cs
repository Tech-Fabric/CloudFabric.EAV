using System.Collections.Generic;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ArrayAttributeInstance : AttributeInstance
    {
        public List<AttributeInstance> Items { get; set; }
    }
}