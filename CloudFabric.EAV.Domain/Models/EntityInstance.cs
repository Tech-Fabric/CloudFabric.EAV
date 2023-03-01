using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models;

public class EntityInstance : EntityInstanceBase
{

    public EntityInstance(IEnumerable<IEvent> events) : base(events)
    {
        
    }

    public EntityInstance(Guid id, Guid entityConfigurationId, List<AttributeInstance> attributes, Guid? tenantId) : base(id, entityConfigurationId, attributes, tenantId)
    {
    }
    
    
}