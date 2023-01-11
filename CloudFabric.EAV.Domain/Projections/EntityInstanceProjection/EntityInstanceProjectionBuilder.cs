using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.Projections;

namespace CloudFabric.EAV.Domain.LocalEventSourcingPackages.Projections.EntityInstanceProjection;

public class EntityInstanceProjectionBuilder : InstanceProjectionBuilder,
    IHandleEvent<EntityInstanceCreated>,
    IHandleEvent<AggregateUpdatedEvent<EntityInstance>>,
    // IHandleEvent<AttributeInstanceAdded>,
    IHandleEvent<AttributeInstanceUpdated>
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
        await CreateInstanceProjection(@event.AggregateId,
            @event.EntityConfigurationId,
            @event.TenantId,
            @event.CategoryPath,
            @event.Attributes.AsReadOnly(),
            @event.PartitionKey,
            @event.Timestamp);        // Get all parent attributes from inheritance branch
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
        (List<AttributeConfiguration> allParentalAttributesConfigurations, _) = await BuildBranchAttributes(@event.CategoryPath);

        var projectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
            @event.EntityConfigurationId,
            allParentalAttributesConfigurations
        );

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
        );
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
            .LoadAsyncOrThrowNotFound(@event.AggregateId, @event.PartitionKey);

        (List<AttributeConfiguration> allParentalAttributesConfigurations, _) = await BuildBranchAttributes(entityInstance.CategoryPath);

        
        var schema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
            entityInstance.EntityConfigurationId, allParentalAttributesConfigurations
        );
        
        await SetDocumentUpdatedAt(schema, @event.AggregateId, @event.PartitionKey, @event.UpdatedAt);
    }
}
