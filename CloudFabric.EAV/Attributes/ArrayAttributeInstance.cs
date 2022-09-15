using System.Collections.Generic;

namespace CloudFabric.EAV.Attributes
{
    public class ArrayAttributeInstance : AttributeInstance
    {
        public List<AttributeInstance> Items { get; set; }
    }
}