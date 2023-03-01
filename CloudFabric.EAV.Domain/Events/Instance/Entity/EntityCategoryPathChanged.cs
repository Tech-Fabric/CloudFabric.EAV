using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record EntityCategoryPathChanged : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public EntityCategoryPathChanged()
    {
    }

    public EntityCategoryPathChanged(Guid id, Guid entityConfigurationId, Guid categoryTreeId, string categoryPath)
    {
        AggregateId = id;
        EntityConfigurationId = entityConfigurationId;
        CategoryPath = categoryPath;
        CategoryTreeId = categoryTreeId;
    }

    public string CategoryPath { get; set; }
    public Guid EntityConfigurationId { get; set; }

    public Guid CategoryTreeId { get; set; }
}
