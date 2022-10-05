using System.Collections.Generic;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class ArrayAttributeInstance : AttributeInstance
    {
        public List<AttributeInstance> Items { get; set; }

        public ArrayAttributeInstance(IEnumerable<IEvent> events) : base(events)
        {
        }

        public ArrayAttributeInstance(string configurationAttributeMachineName) : base(configurationAttributeMachineName)
        {
        }
    }
}