using System;
using System.Collections.Generic;
using System.Linq;
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

        public void AddAttributeInstance(AttributeInstance attribute)
        {
            Apply(new AttributeInstanceAdded(Guid.Parse(Id), attribute));
        }

        public void UpdateAttributeInstance(AttributeInstance attribute)
        {
            Apply(new AttributeInstanceUpdated(Guid.Parse(Id), attribute));
        }

        public void RemoveAttributeInstance(string attributeMachineName)
        {
            Apply(new AttributeInstanceRemoved(Guid.Parse(Id), attributeMachineName));
        }
        
        #region Event Handlers

        public void On(EntityInstanceCreated @event)
        {
            Id = @event.Id.ToString();
            EntityConfigurationId = @event.EntityConfigurationId;
            Attributes = @event.Attributes;
        }

        public void On(AttributeInstanceAdded @event)
        {
            Attributes ??= new();
            Attributes.Add(@event.AttributeInstance);
        }

        public void On(AttributeInstanceUpdated @event)
        {
            var attribute = Attributes?.FirstOrDefault(x => x.ConfigurationAttributeMachineName == @event.AttributeInstance.ConfigurationAttributeMachineName);

            if (attribute != null)
            {
                Attributes.Remove(attribute);
                Attributes.Add(@event.AttributeInstance);
            }
        }

        public void On(AttributeInstanceRemoved @event)
        {
            var attribute = Attributes?.FirstOrDefault(x => x.ConfigurationAttributeMachineName == @event.AttributeMachineName);

            if (attribute != null)
            {
                Attributes.Remove(attribute);
            }
        }

        #endregion
    }
}