using CloudFabric.EAV.Domain.Events.Configuration.Attribute;
using CloudFabric.Projections;

namespace CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;

public class AttributeConfigurationProjectionBuilder : ProjectionBuilder<AttributeConfigurationProjectionDocument>,
    IHandleEvent<AttributeConfigurationCreated>//,
    //IHandleEvent<AttributeConfigurationNameUpdated>,
    //IHandleEvent<AttributeConfigurationDescriptionUpdated>,
    //IHandleEvent<AttributeConfigurationIsRequiredFlagUpdated>, 
{
    public AttributeConfigurationProjectionBuilder(
        ProjectionRepositoryFactory projectionRepositoryFactory
    ) : base(projectionRepositoryFactory) {
    }

    public async Task On(AttributeConfigurationCreated @event)
    {
        await UpsertDocument(new AttributeConfigurationProjectionDocument()
            {
                Id = @event.Id,
                IsRequired = @event.IsRequired,
                Name = @event.Name,
                MachineName = @event.MachineName,
                PartitionKey = @event.PartitionKey,
                TenantId = @event.TenantId
            },
            @event.PartitionKey
        );
    }
}