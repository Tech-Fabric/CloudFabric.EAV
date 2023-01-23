using System;

namespace CloudFabric.EAV.Domain.Models;

/// <summary>
/// Links attribute configuration to entity configuration
/// </summary>
public class EntityConfigurationAttributeReference
{
    public Guid AttributeConfigurationId { get; set; }

    public List<object> Overrides { get; set; } = new List<object>();
}