namespace CloudFabric.EAV.Domain.Models;

/// <summary>
///     Links attribute configuration to entity configuration
///     External values - attribute data that must be stored on EntityConfiguration level, because one attribute can be
///     assigned to many entity configurations.
/// </summary>
public class EntityConfigurationAttributeReference
{
    public Guid AttributeConfigurationId { get; set; }

    public List<object> AttributeConfigurationExternalValues { get; set; } = new();
}
