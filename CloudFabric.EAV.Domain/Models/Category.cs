using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models
{
    public class Category : EntityInstanceBase
    {
        public Category(IEnumerable<IEvent> events) : base(events)
        {
        }

        public Category(Guid id, Guid entityConfigurationId, List<AttributeInstance> attributes, Guid? tenantId)
            : base(id, entityConfigurationId, attributes, tenantId)
        {
        }

        public Category(
            Guid id,
            Guid entityConfigurationId,
            List<AttributeInstance> attributes,
            Guid? tenantId,
            string categoryPath,
            Guid categoryTreeId
        ) : base(id, entityConfigurationId, attributes, tenantId)
        {
            Apply(new EntityCategoryPathChanged(id, EntityConfigurationId, categoryTreeId, categoryPath));
        }

    }
}
