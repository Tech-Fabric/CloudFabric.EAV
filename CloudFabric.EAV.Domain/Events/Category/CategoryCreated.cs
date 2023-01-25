using System.Collections.ObjectModel;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.LocalEventSourcingPackages.Events.Category
{
    
    public record CategoryCreated: Event
    {
        public string PartitionKey { get; }
        public Guid EntityConfigurationId { get; }
        public ReadOnlyCollection<AttributeInstance> Attributes { get; }
        public Guid? TenantId { get; }
        public string CategoryPath { get; }
        public DateTime Timestamp { get; }

        public CategoryCreated()
        {
        }
        
        public CategoryCreated(
            Guid id,
            string partitionKey,
            Guid entityConfigurationId,
            ReadOnlyCollection<AttributeInstance> attributes,
            Guid? tenantId,
            string categoryPath,
            DateTime timestamp)
        {
            this.PartitionKey = partitionKey;
            this.EntityConfigurationId = entityConfigurationId;
            this.Attributes = attributes;
            TenantId = tenantId;
            CategoryPath = categoryPath;
            Timestamp = timestamp;
            AggregateId = id;
        }    
    };
    
}