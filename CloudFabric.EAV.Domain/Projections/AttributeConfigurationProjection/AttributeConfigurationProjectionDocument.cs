using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.Projections;
using CloudFabric.Projections.Attributes;

namespace CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;

[ProjectionDocument]
public class AttributeConfigurationProjectionDocument : ProjectionDocument
{
    [ProjectionDocumentProperty(IsNestedArray = true)]
    public List<SearchableLocalizedString> Name { get; set; }

    [ProjectionDocumentProperty(IsNestedArray = true)]
    public List<LocalizedString> Description { get; set; }

    [ProjectionDocumentProperty(IsFilterable = true)]
    public string MachineName { get; set; }

    [ProjectionDocumentProperty(IsFilterable = true)]
    public bool IsRequired { get; set; }

    [ProjectionDocumentProperty(IsFilterable = true)]
    public Guid? TenantId { get; set; }

    [ProjectionDocumentProperty(IsFilterable = true)]
    public DateTime UpdatedAt { get; set; }

    [ProjectionDocumentProperty(IsFilterable = true)]
    public EavAttributeType ValueType { get; set; }

    [ProjectionDocumentProperty(IsFilterable = true)]
    public int NumberOfEntityInstancesWithAttribute { get; set; }

    [ProjectionDocumentProperty] public string? Metadata { get; set; }
}
