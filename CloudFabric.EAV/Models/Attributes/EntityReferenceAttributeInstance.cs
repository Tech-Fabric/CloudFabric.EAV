using System;
using System.Collections.Generic;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class EntityReferenceAttributeInstance : AttributeInstance
    {
        public Guid Value { get; set; }

        public EntityReferenceAttributeInstance(IEnumerable<IEvent> events) : base(events)
        {
        }

        public EntityReferenceAttributeInstance(string configurationAttributeMachineName) : base(configurationAttributeMachineName)
        {
        }
    }
}