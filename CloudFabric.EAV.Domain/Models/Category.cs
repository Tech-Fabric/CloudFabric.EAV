using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models
{
    public class Category : EntityInstanceBase
    {
        public Guid CategoryTreeId { get; protected set; }
        public string CategoryPath { get; protected set; }
        public Category(IEnumerable<IEvent> events) : base(events)
        {
        }

        public Category(Guid id, Guid entityConfigurationId, List<AttributeInstance> attributes, Guid? tenantId, string categoryPath, string categoryTreeId) : base(id, entityConfigurationId, attributes, tenantId)
        {
            Apply(new EntityCategoryPathChanged(id, EntityConfigurationId, categoryTreeId, categoryPath));
        }
        public void On(EntityCategoryPathChanged @event)
        {
            CategoryPath = @event.CategoryPath;
            CategoryTreeId = new Guid(@event.CategoryTreeId);
        }
        
    }
}