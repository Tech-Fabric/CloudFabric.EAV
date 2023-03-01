using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance
{
    public record CategoryTreeCreated : Event
    {
        // ReSharper disable once UnusedMember.Global
        // This constructor is required for Event Store to properly deserialize from json
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
