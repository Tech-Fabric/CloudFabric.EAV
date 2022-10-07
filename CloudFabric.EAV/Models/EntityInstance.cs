using System;
using System.Collections.Generic;
using System.Linq;
using CloudFabric.EAV.Data.Events.Configuration.Entity;
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

        public void EntityConfigurationChanged(EntityConfiguration configuration)
        {
            Apply(new EntityConfigurationChanged(configuration));
        }

        public void AddAttribute(AttributeInstance attributeInstance)
        {
            Apply(new AddAttributeInstance(attributeInstance));
        }

        public void RemoveAttribute(Guid id)
        {
            Apply(new RemoveAttributeInstance(id));
        }

        public void UpdateAttribute(AttributeInstance attributeInstance)
        {
            Apply(new UpdateAttributeInstance(attributeInstance));
        }
        #region Events

        public void On(EntityInstanceCreated @event)
        {
            EntityConfigurationId = @event.EntityConfigurationId;
            EntityConfiguration = @event.EntityConfiguration;
            Attributes = @event.Attributes;
        }

        public void On(EntityConfigurationChanged @event)
        {
            EntityConfiguration = @event.Configuration;
        }

        public void On(AddAttributeInstance @event)
        {
        }

        #endregion
    }
}