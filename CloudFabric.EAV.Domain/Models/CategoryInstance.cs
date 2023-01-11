using System.Collections.ObjectModel;

using CloudFabric.EAV.Domain.LocalEventSourcingPackages.Events.Category;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models
{
    public class CategoryInstance: AggregateBase
    {
        public override string PartitionKey => Id.ToString(); 
        public Guid EntityConfigurationId { get; protected set; }
        public ReadOnlyCollection<AttributeInstance> Attributes { get; protected set; }
        public Guid? TenantId { get; protected set; }
        public string CategoryPath { get; protected set; }
        public Guid ChildEntityConfigurationId { get; set; }

        public CategoryInstance(IEnumerable<IEvent> events) : base(events)
        {
        }

        public CategoryInstance(Guid id, Guid entityConfigurationId, string categoryPath, ReadOnlyCollection<AttributeInstance> attributes, Guid? tenantId, Guid childEntityConfigurationId)
        {
            Apply(new CategoryCreated(id, PartitionKey, entityConfigurationId, attributes, tenantId, categoryPath, childEntityConfigurationId, DateTime.Now));
        }
        
        public async Task ChangeCategoryPath(string newCategoryPath, Guid childEntityConfigurationId)
        {
            Apply(new CategoryPathChanged(EntityConfigurationId, CategoryPath, newCategoryPath, childEntityConfigurationId));
        }
        
        public void On(CategoryCreated @event) {
            EntityConfigurationId = @event.EntityConfigurationId;
            Attributes = @event.Attributes;
            TenantId = @event.TenantId;
            CategoryPath = @event.CategoryPath;
            ChildEntityConfigurationId = @event.ChildEntityConfigurationId;
            Id = @event.Id;
        }
        
        public void On(CategoryPathChanged @event) {
            CategoryPath = @event.newCategoryPath;
        }
    }
}