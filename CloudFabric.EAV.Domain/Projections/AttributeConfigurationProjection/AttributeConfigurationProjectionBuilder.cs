using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CloudFabric.EAV.Domain.Events.Configuration.Attribute;
using CloudFabric.EAV.Domain.Events.Configuration.Entity;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.Projections;

namespace CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;

public class AttributeConfigurationProjectionBuilder : ProjectionBuilder<AttributeConfigurationProjectionDocument>,
    IHandleEvent<AttributeConfigurationCreated>//,
    //IHandleEvent<AttributeConfigurationNameUpdated>,
    //IHandleEvent<AttributeConfigurationDescriptionUpdated>,
    //IHandleEvent<AttributeConfigurationIsRequiredFlagUpdated>, 
{
    public AttributeConfigurationProjectionBuilder(
        IProjectionRepository<AttributeConfigurationProjectionDocument> repository
    ) : base(repository) {
    }

    public async Task On(AttributeConfigurationCreated @event)
    {
        await UpsertDocument(new AttributeConfigurationProjectionDocument()
            {
                Id = @event.Id,
                IsRequired = @event.IsRequired,
                Name = @event.Name,
                MachineName = @event.MachineName,
                PartitionKey = @event.PartitionKey
            },
            @event.PartitionKey
        );
    }
}