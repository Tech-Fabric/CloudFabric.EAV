using System.Collections.Generic;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class TextAttributeInstance : AttributeInstance
    {
        public string Value { get; set; }

        public TextAttributeInstance(IEnumerable<IEvent> events) : base(events)
        {
        }

        public TextAttributeInstance(string configurationAttributeMachineName) : base(configurationAttributeMachineName)
        {
        }
    }
}