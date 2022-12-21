using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.Projections;

namespace CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;

public class EntityInstanceProjectionBuilder : ProjectionBuilder,
    IHandleEvent<EntityInstanceCreated>,
    IHandleEvent<AttributeInstanceAdded>,
    IHandleEvent<AttributeInstanceUpdated>,
    IHandleEvent<AttributeInstanceRemoved>
{
    public EntityInstanceProjectionBuilder(ProjectionRepositoryFactory repositoryFactory) : base(repositoryFactory)
    {
    }

    public async Task On(EntityInstanceCreated @event)
    {
        // await UpsertDocument(new Dictionary<string, object?>()
        // {
        //     { nameof(EntityInstanceProjectionDocument.Id), @event.Id },
        //     { nameof(EntityInstanceProjectionDocument.EntityConfigurationId), @event.EntityConfigurationId },
        //     { nameof(EntityInstanceProjectionDocument.Attributes), @event.Attributes },
        //     { nameof(EntityInstanceProjectionDocument.TenantId), @event.TenantId }
        // },
        // @event.PartitionKey);
    }

    public async Task On(AttributeInstanceAdded @event)
    {
        // await UpdateDocument(@event.EntityInstanceId,
        //     @event.PartitionKey,
        //     (document) =>
        //     {
        //         var attributes = document[nameof(EntityInstanceProjectionDocument.Attributes)] as List<AttributeInstance>;
        //         attributes ??= new();

        //         attributes.Add(@event.AttributeInstance);
        //     }
        // );
    }

    public async Task On(AttributeInstanceUpdated @event)
    {
        // await UpdateDocument(@event.EntityInstanceId,
        //     @event.PartitionKey,
        //     (document) =>
        //     {
        //         var attributes = document[nameof(EntityInstanceProjectionDocument.Attributes)] as List<AttributeInstance>;
        //         var attributeToUpdate = attributes?.FirstOrDefault(x => x.ConfigurationAttributeMachineName == @event.AttributeInstance.ConfigurationAttributeMachineName);

        //         if (attributeToUpdate != null)
        //         {
        //             attributes.Remove(attributeToUpdate);
        //             attributes.Add(@event.AttributeInstance);
        //         }
        //     }
        // );
    }

    public async Task On(AttributeInstanceRemoved @event)
    {
        // await UpdateDocument(@event.EntityInstanceId,
        //     @event.PartitionKey,
        //     (document) =>
        //     {
        //         var attributes = document[nameof(EntityInstanceProjectionDocument.Attributes)] as List<AttributeInstance>;
        //         var attributeToRemove = attributes?.FirstOrDefault(x => x.ConfigurationAttributeMachineName == @event.AttributeMachineName);

        //         if (attributeToRemove != null)
        //         {
        //             attributes.Remove(attributeToRemove);
        //         }
        //     }
        // );
    }
}
