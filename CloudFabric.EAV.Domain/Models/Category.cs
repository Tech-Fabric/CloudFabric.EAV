using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models;

public class Category : EntityInstanceBase
{
    public string MachineName { get; set; }
    public Category(IEnumerable<IEvent> events) : base(events)
    {
    }

    public Category(Guid id,
        string machineName,
        Guid entityConfigurationId,
        List<AttributeInstance> attributes,
        Guid? tenantId)
    {
        Apply(new CategoryCreated(id, machineName, entityConfigurationId, attributes, tenantId));

    }

    public Category(
        Guid id,
        string machineName,
        Guid entityConfigurationId,
        List<AttributeInstance> attributes,
        Guid? tenantId,
        string categoryPath,
        Guid? parentId,
        Guid categoryTreeId
    ) : this(id, machineName, entityConfigurationId, attributes, tenantId)
    {
        Apply(new EntityCategoryPathChanged(id, EntityConfigurationId, categoryTreeId, categoryPath, parentId));
    }

    public void On(CategoryCreated @event)
    {
        Id = @event.AggregateId;
        EntityConfigurationId = @event.EntityConfigurationId;
        Attributes = new List<AttributeInstance>(@event.Attributes).AsReadOnly();
        TenantId = @event.TenantId;
        CategoryPaths = new List<CategoryPath>();
        MachineName = @event.MachineName;
    }
}
