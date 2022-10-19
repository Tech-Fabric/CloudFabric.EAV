using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models;

public class EntityInstance : AggregateBase
{
    public override string PartitionKey => Id;

    public Guid EntityConfigurationId { get; protected set; }

    public ReadOnlyCollection<AttributeInstance> Attributes { get; protected set; }

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
        Attributes = new List<AttributeInstance>(@event.Attributes).AsReadOnly();
    }

    public void On(AttributeInstanceAdded @event)
    {
        var newCollection = Attributes == null ? new List<AttributeInstance>() : new List<AttributeInstance>(Attributes);
        newCollection.Add(@event.AttributeInstance);
        Attributes = newCollection.AsReadOnly();
    }

    public void On(AttributeInstanceUpdated @event)
    {
        var attribute = Attributes?.FirstOrDefault(x => x.ConfigurationAttributeMachineName == @event.AttributeInstance.ConfigurationAttributeMachineName);
        
        if (attribute != null)
        {
            var newCollection = new List<AttributeInstance>(Attributes);
            
            newCollection.Remove(attribute);
            newCollection.Add(@event.AttributeInstance);

            Attributes = newCollection.AsReadOnly();
        }
    }

    public void On(AttributeInstanceRemoved @event)
    {
        var attribute = Attributes?.FirstOrDefault(x => x.ConfigurationAttributeMachineName == @event.AttributeMachineName);

        if (attribute != null)
        {
            var newCollection = new List<AttributeInstance>(Attributes);
            
            newCollection.Remove(attribute);

            Attributes = newCollection.AsReadOnly();
        }
    }

    #endregion
}
