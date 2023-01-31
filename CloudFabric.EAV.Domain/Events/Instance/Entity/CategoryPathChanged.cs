using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity
{
    public record CategoryPathChanged: Event
    {
        public string CategoryPath { get; set; }
        public Guid EntityConfigurationId { get; set; }

        public CategoryPathChanged(Guid entityConfigurationId, string categoryPath)
        {
            CategoryPath = categoryPath;
        }
    }
    
}