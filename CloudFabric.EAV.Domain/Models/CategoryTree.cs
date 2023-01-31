using CloudFabric.EAV.Domain.Events.Instance;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models
{
    public class CategoryTree : AggregateBase
    {
    
        public string MachineName { get; protected set; }
        public Guid EntityConfigurationId { get; protected set; }
        public override string PartitionKey => Id.ToString();

        public Guid? TenantId { get; set; }

        public CategoryTree(IEnumerable<IEvent> events) : base(events)
        {
        }

        public CategoryTree(Guid id, Guid entityConfigurationId, string machineName, Guid? tenantId)
        {
            Apply(new CategoryTreeCreated(id, entityConfigurationId, machineName, tenantId));
        }
        
        public void On(CategoryTreeCreated @event)
        {
            Id = @event.Id;
            MachineName = @event.MachineName;
            EntityConfigurationId = @event.EntityConfigurationId;
            TenantId = @event.TenantId;
        }

    }

}