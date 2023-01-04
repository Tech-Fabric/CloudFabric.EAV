using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.Projections;
using CloudFabric.Projections.Attributes;

namespace CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;

[ProjectionDocument]
public class EntityConfigurationProjectionDocument : ProjectionDocument
{
    [ProjectionDocumentProperty(IsNestedArray = true, IsSearchable = true)]
    public List<LocalizedString> Name { get; set; } = new();

    [ProjectionDocumentProperty(IsFilterable = true)]
    public string MachineName { get; set; }

    [ProjectionDocumentProperty(IsFilterable = true)]
    public Guid? TenantId { get; set; }

    [ProjectionDocumentProperty(IsNestedArray = true)]
    public List<AttributeConfigurationReference> Attributes { get; set; }
}
