using CloudFabric.EAV.Domain.Models;
using System.Collections.Generic;
using System;
using CloudFabric.Projections;
using CloudFabric.Projections.Attributes;

namespace CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;

[ProjectionDocument]
public class EntityInstanceProjectionDocument : ProjectionDocument
{
    [ProjectionDocumentProperty(IsFilterable = true)]
    public Guid EntityConfigurationId { get; set; }

    [ProjectionDocumentProperty(IsNestedArray = true)]
    public List<AttributeInstance> Attributes { get; set; }
}
