using System.Collections.ObjectModel;

using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models;

public class EntityInstance : AggregateBase
{
    public override string PartitionKey => Id.ToString();

    public Guid EntityConfigurationId { get; protected set; }
    
    public ReadOnlyCollection<AttributeInstance> Attributes { get; protected set; }

    public Guid? TenantId { get; protected set; }
    public string CategoryPath { get; protected set; }

    public EntityInstance(IEnumerable<IEvent> events) : base(events)
    {
    }

    public EntityInstance(Guid id, Guid entityConfigurationId, string categoryPath, List<AttributeInstance> attributes, Guid? tenantId)
    {
        Apply(new EntityInstanceCreated(id, entityConfigurationId, categoryPath, attributes, tenantId));
    }
    
    public void AddAttributeInstance(AttributeInstance attribute)
    {
        Apply(new AttributeInstanceAdded(Id, attribute));
    }

    public void UpdateAttributeInstance(AttributeInstance attribute)
    {
        Apply(new AttributeInstanceUpdated(Id, EntityConfigurationId, attribute, CategoryPath));
    }

    public void RemoveAttributeInstance(string attributeMachineName)
    {
        Apply(new AttributeInstanceRemoved(Id, EntityConfigurationId, attributeMachineName));
    }
    
    public void CategoryPathChanged(string categoryPath)
    {
        Apply(new EntityInstanceCategoryPathChanged(Id, categoryPath));
    }

    #region Event Handlers

    public void On(EntityInstanceCreated @event)
    {
        Id = @event.AggregateId;
        EntityConfigurationId = @event.EntityConfigurationId;
        Attributes = new List<AttributeInstance>(@event.Attributes).AsReadOnly();
        TenantId = @event.TenantId;
        CategoryPath = @event.CategoryPath;

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
    
    public void On(EntityInstanceCategoryPathChanged @event)
    {
        CategoryPath = @event.NewCategoryPath;
    }

    #endregion
}
