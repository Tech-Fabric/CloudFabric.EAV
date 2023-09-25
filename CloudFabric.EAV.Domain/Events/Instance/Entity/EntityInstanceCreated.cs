using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record EntityInstanceCreated : Event
{
    public string? MachineName { get; set; }

    public IReadOnlyCollection<CategoryPath>? CategoryPaths { get; set; }

    public Guid EntityConfigurationId { get; set; }

    public IReadOnlyCollection<AttributeInstance> Attributes { get; set; }

    public Guid? TenantId { get; set; }

    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public EntityInstanceCreated()
    {
    }

    public EntityInstanceCreated(
        Guid id,
        Guid entityConfigurationId,
        List<AttributeInstance> attributes,
        string? machineName,
        Guid? tenantId,
        List<CategoryPath>? categoryPaths
    ) {
        AggregateId = id;
        EntityConfigurationId = entityConfigurationId;
        MachineName = machineName;
        Attributes = attributes;
        TenantId = tenantId;
        CategoryPaths = categoryPaths;
    }
}
