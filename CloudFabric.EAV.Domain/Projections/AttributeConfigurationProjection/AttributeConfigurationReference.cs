using CloudFabric.Projections.Attributes;

namespace CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;

public class AttributeConfigurationReference
{
    [ProjectionDocumentProperty] public Guid AttributeConfigurationId { get; set; }
}
