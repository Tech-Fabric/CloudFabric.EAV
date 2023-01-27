using System;

namespace CloudFabric.EAV.Domain.Models;

/// <summary>
/// Links attribute configuration to entity configuration
/// External values - values that inherent to an attribute, but assigned outside of it.
/// </summary>
public class EntityConfigurationAttributeReference
{
    public Guid AttributeConfigurationId { get; set; }

    public List<object> AttributeConfigurationExternalValues { get; set; } = new List<object>();
}