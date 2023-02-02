using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance
{
    public record CategoryTreeCreated : Event
    {

        public CategoryTreeCreated()
        {
        }

        public CategoryTreeCreated(Guid id, Guid entityConfigurationId, string machineName, Guid? tenantId)
        {
            AggregateId = id;
            MachineName = machineName;
            TenantId = tenantId;
            EntityConfigurationId = entityConfigurationId;
        }

        public string MachineName { get; set; }
        public Guid? TenantId { get; }

        public Guid EntityConfigurationId { get; set; }
    }
}