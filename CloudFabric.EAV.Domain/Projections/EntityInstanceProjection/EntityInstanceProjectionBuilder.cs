using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EAV.Domain.LocalEventSourcingPackages.Events.Category;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.Projections;
// ReSharper disable AsyncConverter.AsyncMethodNamingHighlighting

namespace CloudFabric.EAV.Domain.LocalEventSourcingPackages.Projections.EntityInstanceProjection;

public class EntityInstanceProjectionBuilder : InstanceProjectionBuilder,
    IHandleEvent<EntityInstanceCreated>,
    IHandleEvent<AggregateUpdatedEvent<EntityInstance>>,
    // IHandleEvent<AttributeInstanceAdded>,
    IHandleEvent<AttributeInstanceUpdated>,
    IHandleEvent<CategoryPathChanged>
    // IHandleEvent<AttributeInstanceRemoved>,
{
    
    public EntityInstanceProjectionBuilder(
        AggregateRepositoryFactory aggregateRepositoryFactory,
        ProjectionRepositoryFactory projectionRepositoryFactory
    ) : base(projectionRepositoryFactory, aggregateRepositoryFactory)
    {
    }
    
    
    public async Task On(EntityInstanceCreated @event)
    {
        // Build schema for entity instance considering all parent attributes and their configurations
        var projectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
            @event.EntityConfigurationId,
            null
        ).ConfigureAwait(false);
        var document = new Dictionary<string, object?>()
        {
            {
                "Id", @event.AggregateId
            },
            {
                "EntityConfigurationId", @event.EntityConfigurationId
            },
            {
                "TenantId", @event.TenantId
            },
            {
                "CategoryPath", @event.CategoryPath
            },
            {
                "Attributes", new Dictionary<string, object?>()
            }
        };

        // fill attributes
        foreach (var attribute in @event.Attributes)
        {
            document.Add(attribute.ConfigurationAttributeMachineName, attribute.GetValue());
        }

        // Add document
        await UpsertDocument(
            projectionDocumentSchema,
            document,
            @event.PartitionKey,
            @event.Timestamp
        ).ConfigureAwait(false);
    }

    // Update all children instances if exist after current instance changed
    
    //
    // public async Task On(AttributeInstanceAdded @event)
    // {
    //     await UpdateDocument(@event.EntityInstanceId,
    //         @event.PartitionKey,
    //         (document) =>
    //         {
    //             var attributes =
    //                 document[nameof(EntityInstanceProjectionDocument.Attributes)] as List<AttributeInstance>;
    //             attributes ??= new();
    //
    //             attributes.Add(@event.AttributeInstance);
    //         }
    //     );
    // }
    //
    public async Task On(AttributeInstanceUpdated @event)
    {

        var projectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
            @event.EntityConfigurationId,
            null
        ).ConfigureAwait(false);

        await UpdateDocument(
            projectionDocumentSchema,
            @event.AggregateId,
            @event.PartitionKey,
            @event.Timestamp,
            (document) =>
            {
                document[@event.AttributeInstance.ConfigurationAttributeMachineName] =
                    @event.AttributeInstance.GetValue();
            }
        ).ConfigureAwait(false);
    }
    //
    // public async Task On(AttributeInstanceRemoved @event)
    // {
    //     await UpdateDocument(@event.EntityInstanceId,
    //         @event.PartitionKey,
    //         (document) =>
    //         {
    //             var attributes =
    //                 document[nameof(EntityInstanceProjectionDocument.Attributes)] as List<AttributeInstance>;
    //             var attributeToRemove = attributes?.FirstOrDefault(x =>
    //                 x.ConfigurationAttributeMachineName == @event.AttributeMachineName
    //             );
    //
    //             if (attributeToRemove != null)
    //             {
    //                 attributes.Remove(attributeToRemove);
    //             }
    //         }
    //     );
    // }

    public async Task On(AggregateUpdatedEvent<EntityInstance> @event)
    {
        var entityInstance = await _aggregateRepositoryFactory
            .GetAggregateRepository<EntityInstance>()
            .LoadAsyncOrThrowNotFound(@event.AggregateId, @event.PartitionKey).ConfigureAwait(false);

        var schema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
            entityInstance.EntityConfigurationId, 
            null
        ).ConfigureAwait(false);
        
        await SetDocumentUpdatedAt(schema, @event.AggregateId, @event.PartitionKey, @event.UpdatedAt).ConfigureAwait(false);
    }

    public Task On(CategoryPathChanged @event)
    {
        throw new NotImplementedException();
    }
}
