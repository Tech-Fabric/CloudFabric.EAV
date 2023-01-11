using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.LocalEventSourcingPackages.Events.Category
{
    public record CategoryPathChanged : Event
    {
        public CategoryPathChanged()
        {
            
        }
        public CategoryPathChanged(Guid id, Guid entityConfigurationId, string currentCategoryPath, string newCategoryPath, Guid childConfigurationId)
        {
            this.entityConfigurationId = entityConfigurationId;
            this.currentCategoryPath = currentCategoryPath;
            this.newCategoryPath = newCategoryPath;
            this.childConfigurationId = childConfigurationId;
            AggregateId = id;
        }

        public Guid entityConfigurationId { get; init; }
        public string currentCategoryPath { get; init; }
        public string newCategoryPath { get; init; }
        public Guid childConfigurationId { get; init; }

        public void Deconstruct(out Guid entityConfigurationId, out string currentCategoryPath, out string newCategoryPath, out Guid childConfigurationId)
        {
            entityConfigurationId = this.entityConfigurationId;
            currentCategoryPath = this.currentCategoryPath;
            newCategoryPath = this.newCategoryPath;
            childConfigurationId = this.childConfigurationId;
        }
    }
}