using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

using ProjectionDocumentSchemaFactory = CloudFabric.EAV.Domain.LocalEventSourcingPackages.Projections.ProjectionDocumentSchemaFactory;

namespace CloudFabric.EAV.Domain.LocalEventSourcingPackages.Projections.EntityInstanceProjection;

public class EntityInstanceProjectionBuilder : InstanceProjectionBuilder,
    IHandleEvent<EntityInstanceCreated>,
    IHandleEvent<AggregateUpdatedEvent<EntityInstance>>,
    // IHandleEvent<AttributeInstanceAdded>,
    IHandleEvent<AttributeInstanceUpdated>
    // IHandleEvent<AttributeInstanceRemoved>
{
    
    public EntityInstanceProjectionBuilder(
        AggregateRepositoryFactory aggregateRepositoryFactory,
        ProjectionRepositoryFactory projectionRepositoryFactory
    ) : base(projectionRepositoryFactory, aggregateRepositoryFactory)
    {
    }
    
    
    public async Task On(EntityInstanceCreated @event)
    {
        // Get all parent attributes from inheritance branch
        (List<AttributeConfiguration> allParentalAttributesConfigurations, Dictionary<string, object> allParentalAttributes) = await BuildBranchAttributes(@event.CategoryPath);

        // Build schema for entity instance considering all parent attributes and their configurations
        var projectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
            @event.EntityConfigurationId,
            allParentalAttributesConfigurations
        );

        var document = new Dictionary<string, object?>()
        {
            { "Id", @event.AggregateId },
            { "EntityConfigurationId", @event.EntityConfigurationId },
            { "TenantId", @event.TenantId },
            {"CategoryPath", @event.CategoryPath},
            {"ParentalAttributes", allParentalAttributes},
            {"Attributes", new Dictionary<string, object?>()}
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
        );
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
        var repo = _aggregateRepositoryFactory.GetAggregateRepository<EntityInstance>();
        // await UpdateDocument(@event.EntityInstanceId,
        //     @event.PartitionKey,
        //     (document) =>
        //     {
        //         var attributes =
        //             document[nameof(EntityInstanceProjectionDocument.Attributes)] as List<AttributeInstance>;
        //         var attributeToUpdate = attributes?.FirstOrDefault(x =>
        //             x.ConfigurationAttributeMachineName == @event.AttributeInstance.ConfigurationAttributeMachineName
        //         );
        //
        //         if (attributeToUpdate != null)
        //         {
        //             attributes.Remove(attributeToUpdate);
        //             attributes.Add(@event.AttributeInstance);
        //         }
        //     }
        // );
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
            .LoadAsyncOrThrowNotFound(@event.AggregateId, @event.AggregateId.ToString());

        var schema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
            entityInstance.EntityConfigurationId, null
        );

        await SetDocumentUpdatedAt(schema, @event.AggregateId, @event.PartitionKey, @event.UpdatedAt);
    }
}
