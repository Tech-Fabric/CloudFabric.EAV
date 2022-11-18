using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Domain.Models;
using System.Collections.Generic;
using CloudFabric.Projections;
using CloudFabric.Projections.Attributes;

namespace CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;

[ProjectionDocument]
public class EntityConfigurationProjectionDocument : ProjectionDocument
{
    [ProjectionDocumentProperty(IsNestedArray = true, IsSearchable = true)]
    public List<LocalizedString> Name { get; set; }

    [ProjectionDocumentProperty(IsFilterable = true)]
    public string MachineName { get; set; }

    [ProjectionDocumentProperty(IsFilterable = true)]
    public Guid? TenantId { get; set; }
}
