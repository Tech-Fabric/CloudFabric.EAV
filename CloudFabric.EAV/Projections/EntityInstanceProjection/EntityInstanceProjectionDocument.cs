using CloudFabric.EAV.Domain.Models;
using System.Collections.Generic;
using System;
using CloudFabric.Projections;

namespace CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;

public class EntityInstanceProjectionDocument : ProjectionDocument
{
    public Guid EntityConfigurationId { get; set; }

    public List<AttributeInstance> Attributes { get; set; }
}
