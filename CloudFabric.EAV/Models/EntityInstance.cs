using System;
using System.Collections.Generic;
using CloudFabric.EAV.Data.Events.Instance.Entity;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models
{
    public class EntityInstance : AggregateBase
    {
        public Guid EntityConfigurationId { get; protected set; }

        public EntityConfiguration EntityConfiguration { get; protected set; }

        public List<AttributeInstance> Attributes { get; protected set; }

        public EntityInstance(IEnumerable<IEvent> events) : base(events)
        {
        }

        public EntityInstance(Guid entityConfigurationId, EntityConfiguration entityConfiguration, List<AttributeInstance> attributes)
        {
            Apply(new EntityInstanceCreated(entityConfigurationId, entityConfiguration, attributes));
        }
    }
}