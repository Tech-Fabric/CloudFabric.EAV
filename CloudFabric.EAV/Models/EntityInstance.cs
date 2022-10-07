using System;
using System.Collections.Generic;
using CloudFabric.EAV.Domain.Events.Instance;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models
{
    public class EntityInstance : AggregateBase
    {
        public override string PartitionKey => Id;
        
        public Guid EntityConfigurationId { get; protected set; }

        public List<AttributeInstance> Attributes { get; protected set; }

        public EntityInstance(IEnumerable<IEvent> events) : base(events)
        {
        }

        public EntityInstance(Guid id, Guid entityConfigurationId, List<AttributeInstance> attributes)
        {
            Apply(new EntityInstanceCreated(id, entityConfigurationId, attributes));
        }
        
        #region Event Handlers

        protected void On()
        {
            
        }
        #endregion
    }
}