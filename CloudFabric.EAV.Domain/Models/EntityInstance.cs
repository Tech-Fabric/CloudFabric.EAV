using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models;

public class EntityInstance : EntityInstanceBase
{
    public Dictionary<string, string> CategoryPath { get; protected set; }

    public EntityInstance(IEnumerable<IEvent> events) : base(events)
    {
        
    }

    public EntityInstance(Guid id, Guid entityConfigurationId, List<AttributeInstance> attributes, Guid? tenantId, Dictionary<string, string> categoryPath) : base(id, entityConfigurationId, attributes, tenantId)
    {
        if (CategoryPath != null)
        {
            foreach (KeyValuePair<string, string> kvp in CategoryPath)
            {
                Apply(new EntityCategoryPathChanged(id, EntityConfigurationId, kvp.Key, kvp.Value));
            }
        }
    }
    
    public void On(EntityCategoryPathChanged @event)
    {
        CategoryPath[@event.CategoryTreeId.ToString()] = @event.CategoryPath;
    }
    
    public void ChangeCategoryPath(string treeId, string categoryPath)
    {
        Apply(new EntityCategoryPathChanged(Id, EntityConfigurationId, treeId, categoryPath));
    }
}