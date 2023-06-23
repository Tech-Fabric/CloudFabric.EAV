using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record EntityCategoryPathChanged : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public EntityCategoryPathChanged()
    {
    }

    public EntityCategoryPathChanged(Guid id,
        Guid entityConfigurationId,
        Guid categoryTreeId,
        string categoryPath,
        Guid parentId)
    {
        AggregateId = id;
        EntityConfigurationId = entityConfigurationId;
        CategoryPath = categoryPath;
        CategoryTreeId = categoryTreeId;
        ParentId = parentId;
        ParentMachineName = categoryPath.Split('/').Last(x => !string.IsNullOrEmpty(x));
    }

    public string CategoryPath { get; set; }
    public Guid EntityConfigurationId { get; set; }

    public Guid CategoryTreeId { get; set; }
    public Guid ParentId { get; set; }
    public string ParentMachineName { get; set; }
}
