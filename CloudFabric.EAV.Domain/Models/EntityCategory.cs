using System.Collections.ObjectModel;

using CloudFabric.EAV.Domain.LocalEventSourcingPackages.Events.Category;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models
{
    public class EntityCategory: AggregateBase
    {
        public override string PartitionKey { get; }
        public Guid EntityConfigurationId { get; protected set; }
        public ReadOnlyCollection<AttributeInstance> Attributes { get; protected set; }
        public Guid? TenantId { get; protected set; }
        public string CategoryPath { get; protected set; }
        public Guid ChildEntityConfigurationId { get; set; }

        public EntityCategory(IEnumerable<IEvent> events) : base(events)
        {
        }

        public EntityCategory(Guid id, string partitionKey, Guid entityConfigurationId, ReadOnlyCollection<AttributeInstance> attributes, Guid? tenantId, string categoryPath, Guid childEntityConfigurationId)
        {
            Apply(new CategoryCreated(id, partitionKey, entityConfigurationId, attributes, tenantId, categoryPath, childEntityConfigurationId, DateTime.Now));
        }
        
        public void On(CategoryCreated @event) {
            EntityConfigurationId = @event.EntityConfigurationId;
            Attributes = @event.Attributes;
            TenantId = @event.TenantId;
            CategoryPath = @event.CategoryPath;
            ChildEntityConfigurationId = @event.ChildEntityConfigurationId;
            Id = @event.AggregateId;
        }
    }
}