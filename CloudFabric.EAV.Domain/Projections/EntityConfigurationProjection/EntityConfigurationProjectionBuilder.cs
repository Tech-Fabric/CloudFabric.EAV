using CloudFabric.EAV.Domain.Events.Configuration.Entity;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
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
        List<AttributeConfigurationReference> attributes = new();

        foreach (var eventAttribute in @event.Attributes)
        {
            attributes.Add(new AttributeConfigurationReference
            {
                AttributeConfigurationId = eventAttribute.AttributeConfigurationId
            });
        }

        await UpsertDocument(new EntityConfigurationProjectionDocument
        {
            Id = @event.AggregateId,
            Name = @event.Name,
            MachineName = @event.MachineName,
            TenantId = @event.TenantId,
            Attributes = attributes
        },
        @event.PartitionKey,
        @event.Timestamp);
    }

    public async Task On(EntityConfigurationNameUpdated @event)
    {
        await UpdateDocument(@event.AggregateId!.Value,
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
        await UpdateDocument(@event.AggregateId!.Value,
            @event.PartitionKey,
            @event.Timestamp,
            (document) =>
            {
                document.Attributes.Add(new AttributeConfigurationReference
                {
                    AttributeConfigurationId = @event.AttributeReference.AttributeConfigurationId
                });
            }
        );
    }

    public async Task On(EntityConfigurationAttributeRemoved @event)
    {
        await UpdateDocument(@event.AggregateId!.Value,
            @event.PartitionKey,
            @event.Timestamp,
            (document) =>
            {
                var attributeToRemove = document.Attributes.FirstOrDefault(a => a.AttributeConfigurationId == @event.AttributeConfigurationId);

                if (attributeToRemove != null)
                {
                    document.Attributes.Remove(attributeToRemove);
                }
            }
        );
    }

    public async Task On(AggregateUpdatedEvent<EntityConfiguration> @event)
    {
        await SetDocumentUpdatedAt(@event.AggregateId!.Value, @event.PartitionKey, @event.UpdatedAt);
    }
}