using CloudFabric.EAV.Domain.Events.Configuration.Entity;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.Projections;

namespace CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;

public class EntityConfigurationProjectionBuilder : ProjectionBuilder<EntityConfigurationProjectionDocument>,
    IHandleEvent<EntityConfigurationCreated>,
    IHandleEvent<EntityConfigurationNameUpdated>,
    IHandleEvent<EntityConfigurationAttributeAdded>,
    IHandleEvent<EntityConfigurationAttributeRemoved>
{
    public EntityConfigurationProjectionBuilder(
        ProjectionRepositoryFactory projectionRepositoryFactory
    ) : base(projectionRepositoryFactory)
    {
    }

    public async Task On(EntityConfigurationCreated @event)
    {
        await UpsertDocument(new EntityConfigurationProjectionDocument
        {
            Id = @event.Id,
            Name = @event.Name,
            MachineName = @event.MachineName,
            TenantId = @event.TenantId
        },
        @event.PartitionKey);
    }

    public async Task On(EntityConfigurationNameUpdated @event)
    {
        await UpdateDocument(@event.Id,
            @event.PartitionKey,
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
        await UpdateDocument(@event.EntityConfigurationId,
            @event.PartitionKey,
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
        await UpdateDocument(@event.EntityConfigurationId,
            @event.PartitionKey,
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
}