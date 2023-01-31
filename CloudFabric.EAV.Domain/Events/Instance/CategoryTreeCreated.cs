using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance
{
    public record CategoryTreeCreated : Event
    {


        public CategoryTreeCreated(Guid id, Guid entityConfigurationId, string machineName, Guid? tenantId)
        {
            Id = id;
            MachineName = machineName;
            TenantId = tenantId;
            EntityConfigurationId = entityConfigurationId;
            
        }

        public Guid Id { get; }
        public string MachineName { get; set; }
        public Guid? TenantId { get; }

        public Guid EntityConfigurationId { get; set; }
    }
}