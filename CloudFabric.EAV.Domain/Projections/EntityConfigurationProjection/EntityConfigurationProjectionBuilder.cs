using CloudFabric.EAV.Domain.Events.Configuration.Entity;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.Projections;

namespace CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;

public class EntityConfigurationProjectionBuilder : ProjectionBuilder<EntityConfigurationProjectionDocument>,
    IHandleEvent<EntityConfigurationCreated>,
    IHandleEvent<EntityConfigurationNameUpdated>,
    IHandleEvent<EntityConfigurationAttributeAdded>,
    IHandleEvent<EntityConfigurationAttributeRemoved>,
    IHandleEvent<AggregateUpdatedEvent<EntityConfiguration>>
{
    public EntityConfigurationProjectionBuilder(
        ProjectionRepositoryFactory projectionRepositoryFactory
    ) : base(projectionRepositoryFactory)
    {
    }

    public async Task On(EntityConfigurationCreated @event)
    {
        await UpsertDocument(
            new EntityConfigurationProjectionDocument
            {
                Id = @event.AggregateId,
                Name = @event.Name,
                MachineName = @event.MachineName,
                TenantId = @event.TenantId
            },
            @event.PartitionKey,
            @event.Timestamp
        );
    }

    public async Task On(EntityConfigurationNameUpdated @event)
    {
        await UpdateDocument(@event.AggregateId,
            @event.PartitionKey,
            @event.Timestamp,
            (document) =>
            {
                var name = document.Name?.FirstOrDefault(n => n.CultureInfoId == @event.CultureInfoId);

                if (name == null)
                {
                    document.Name.Add(new LocalizedString
                    {
                        CultureInfoId = @event.CultureInfoId,
                        String = @event.NewName
                    });
                }
                else
                {
                    name.String = @event.NewName;
                }
            }
        );
    }

    public async Task On(EntityConfigurationAttributeAdded @event)
    {
        await UpdateDocument(@event.AggregateId,
            @event.PartitionKey,
            @event.Timestamp,
            (document) =>
            {
                // var attributes = document.Attributes) as List<AttributeConfiguration>;
                // attributes ??= new();

                //attributes.Add(@event.Attribute);
            }
        );
    }

    public async Task On(EntityConfigurationAttributeRemoved @event)
    {
        await UpdateDocument(@event.AggregateId,
            @event.PartitionKey,
            @event.Timestamp,
            (document) =>
            {
                // var attributes = document[nameof(EntityConfigurationProjectionDocument.Attributes)] as List<AttributeConfiguration>;
                // var attributeToRemove = attributes?.FirstOrDefault(x => x. == @event..AttributeMachineName);
                //
                // if (attributeToRemove != null)
                // {
                //     attributes.Remove(attributeToRemove);
                // }
            }
        );
    }

    public async Task On(AggregateUpdatedEvent<EntityConfiguration> @event)
    {
        await SetDocumentUpdatedAt(@event.AggregateId, @event.PartitionKey, @event.UpdatedAt);
    }
}