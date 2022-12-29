using CloudFabric.EAV.Domain.Events.Configuration.Entity;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
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
            Id = @event.Id,
            Name = @event.Name,
            MachineName = @event.MachineName,
            TenantId = @event.TenantId,
            Attributes = attributes
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
                var attributeToRemove = document.Attributes.FirstOrDefault(a => a.AttributeConfigurationId == @event.AttributeConfigurationId);

                if (attributeToRemove != null)
                {
                    document.Attributes = document.Attributes.Where(a => a.AttributeConfigurationId != attributeToRemove.AttributeConfigurationId).ToList();
                }
            }
        );
    }
}