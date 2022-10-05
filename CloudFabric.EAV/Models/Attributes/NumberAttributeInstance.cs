using System.Collections.Generic;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class NumberAttributeInstance : AttributeInstance
    {
        public float Value { get; set; }

        public NumberAttributeInstance(IEnumerable<IEvent> events) : base(events)
        {
        }

        public NumberAttributeInstance(string configurationAttributeMachineName) : base(configurationAttributeMachineName)
        {
        }
    }
}