using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudFabric.EAV.Domain.Events.Configuration.Entity;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.Projections;

namespace CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;

public class EntityConfigurationProjectionBuilder : ProjectionBuilder,
    IHandleEvent<EntityConfigurationCreated>,
    IHandleEvent<EntityConfigurationNameUpdated>,
    IHandleEvent<EntityConfigurationAttributeAdded>,
    IHandleEvent<EntityConfigurationAttributeRemoved>
{
    public EntityConfigurationProjectionBuilder(IProjectionRepository repository) : base(repository)
    {
    }

    public async Task On(EntityConfigurationCreated @event)
    {
        await UpsertDocument(new Dictionary<string, object?>()
        {
            { nameof(EntityConfigurationProjectionDocument.Id), @event.Id.ToString() },
            { nameof(EntityConfigurationProjectionDocument.Name), @event.Name },
            { nameof(EntityConfigurationProjectionDocument.MachineName), @event.MachineName },
            { nameof(EntityConfigurationProjectionDocument.Attributes), @event.Attributes },
            { nameof(EntityConfigurationProjectionDocument.TenantId), @event.TenantId },
            { nameof(EntityConfigurationProjectionDocument.Metadata), @event.Metadata },
        },
        @event.PartitionKey);
    }

    public async Task On(EntityConfigurationNameUpdated @event)
    {
        await UpdateDocument(@event.Id,
            @event.PartitionKey,
            (document) =>
            {
                var localizedName = document[nameof(EntityConfigurationProjectionDocument.Name)] as List<LocalizedString>;
                var name = localizedName?.FirstOrDefault(n => n.CultureInfoId == @event.CultureInfoId);

                if (name == null)
                {
                    localizedName.Add(new LocalizedString()
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
                var attributes = document[nameof(EntityConfigurationProjectionDocument.Attributes)] as List<AttributeConfiguration>;
                attributes ??= new();

                attributes.Add(@event.Attribute);
            }
        );
    }

    public async Task On(EntityConfigurationAttributeRemoved @event)
    {
        await UpdateDocument(@event.EntityConfigurationId,
            @event.PartitionKey,
            (document) =>
            {
                var attributes = document[nameof(EntityConfigurationProjectionDocument.Attributes)] as List<AttributeConfiguration>;
                var attributeToRemove = attributes?.FirstOrDefault(x => x.MachineName == @event.AttributeMachineName);

                if (attributeToRemove != null)
                {
                    attributes.Remove(attributeToRemove);
                }
            }
        );
    }
    
    public async Task On(EntityConfigurationMetadataUpdated @event)
    {
        await UpdateDocument(@event.Id,
            @event.PartitionKey,
            (document) =>
            {
                document[nameof(EntityConfigurationProjectionDocument.Metadata)] = @event.Metadata;
            }
        );
    }
}