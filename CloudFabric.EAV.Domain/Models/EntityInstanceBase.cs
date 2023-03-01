using System.Collections.ObjectModel;

using CloudFabric.EAV.Domain.Events.Instance.Attribute;
using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models
{
    public class EntityInstanceBase : AggregateBase
    {
        public override string PartitionKey => EntityConfigurationId.ToString();
        public List<CategoryPath> CategoryPaths { get; protected set; }

        public Guid EntityConfigurationId { get; protected set; }

        public ReadOnlyCollection<AttributeInstance> Attributes { get; protected set; }

        public Guid? TenantId { get; protected set; }


        public EntityInstanceBase(IEnumerable<IEvent> events) : base(events)
        {
        }

        public EntityInstanceBase(Guid id, Guid entityConfigurationId, List<AttributeInstance> attributes, Guid? tenantId)
        {
            Apply(new EntityInstanceCreated(id, entityConfigurationId, attributes, tenantId));
        }

        public void AddAttributeInstance(AttributeInstance attribute)
        {
            Apply(new AttributeInstanceAdded(Id, attribute));
        }

        public void UpdateAttributeInstance(AttributeInstance attribute)
        {
            Apply(new AttributeInstanceUpdated(Id, EntityConfigurationId, attribute));
        }

        public void RemoveAttributeInstance(string attributeMachineName)
        {
            Apply(new AttributeInstanceRemoved(Id, EntityConfigurationId, attributeMachineName));
        }

        #region Event Handlers

        public void On(EntityInstanceCreated @event)
        {
            Id = @event.AggregateId;
            EntityConfigurationId = @event.EntityConfigurationId;
            Attributes = new List<AttributeInstance>(@event.Attributes).AsReadOnly();
            TenantId = @event.TenantId;
            CategoryPaths = new List<CategoryPath>();
        }
        public void ChangeCategoryPath(Guid treeId, string categoryPath)
        {
            Apply(new EntityCategoryPathChanged(Id, EntityConfigurationId, treeId, categoryPath));
        }

        public void On(EntityCategoryPathChanged @event)
        {
            var categoryPath = CategoryPaths.FirstOrDefault(x => x.TreeId == @event.CategoryTreeId);
            if (categoryPath == null)
            {
                CategoryPaths.Add(new CategoryPath
                {
                    TreeId = @event.CategoryTreeId,
                    Path = @event.CategoryPath
                });
            }
            else
            {
                categoryPath.Path = @event.CategoryPath;
            }
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
}
