using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.Projections;

namespace CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;

public class EntityInstanceProjectionBuilder : ProjectionBuilder,
    IHandleEvent<EntityInstanceCreated>,
    // IHandleEvent<AttributeInstanceAdded>,
    IHandleEvent<AttributeInstanceUpdated>,
    // IHandleEvent<AttributeInstanceRemoved>,
    IHandleEvent<EntityCategoryPathChanged>,
IHandleEvent<AggregateUpdatedEvent<EntityInstance>>
{
    private readonly AggregateRepositoryFactory _aggregateRepositoryFactory;

    public EntityInstanceProjectionBuilder(
        AggregateRepositoryFactory aggregateRepositoryFactory,
        ProjectionRepositoryFactory projectionRepositoryFactory
    ) : base(projectionRepositoryFactory)
    {
        _aggregateRepositoryFactory = aggregateRepositoryFactory;
    }

    private async Task<ProjectionDocumentSchema> BuildProjectionDocumentSchemaForEntityConfigurationIdAsync(
        Guid entityConfigurationId
    )
    {
        var entityConfiguration = await _aggregateRepositoryFactory
            .GetAggregateRepository<EntityConfiguration>()
            .LoadAsyncOrThrowNotFound(entityConfigurationId, entityConfigurationId.ToString())
            .ConfigureAwait(false);

        List<AttributeConfiguration> attributes = new List<AttributeConfiguration>();

        foreach (var attributeReference in entityConfiguration.Attributes)
        {
            var attribute = await _aggregateRepositoryFactory
                .GetAggregateRepository<AttributeConfiguration>()
                .LoadAsyncOrThrowNotFound(attributeReference.AttributeConfigurationId,
                    attributeReference.AttributeConfigurationId.ToString()
                )
                .ConfigureAwait(false);
            attributes.Add(attribute);
        }

        return ProjectionDocumentSchemaFactory.FromEntityConfiguration(entityConfiguration, attributes);
    }

    public async Task On(EntityInstanceCreated @event)
    {
        var projectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationIdAsync(
            @event.EntityConfigurationId
        ).ConfigureAwait(false);

        var document = new Dictionary<string, object?>()
        {
            { "Id", @event.AggregateId },
            { "EntityConfigurationId", @event.EntityConfigurationId },
            { "TenantId", @event.TenantId },
        };

        foreach (var attribute in @event.Attributes)
        {
            document.Add(attribute.ConfigurationAttributeMachineName, attribute.GetValue());
        }

        await UpsertDocument(
            projectionDocumentSchema,
            document,
            @event.PartitionKey,
            @event.Timestamp
        );
    }
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
        var projectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationIdAsync(
            @event.EntityConfigurationId
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
            .LoadAsyncOrThrowNotFound(@event.AggregateId, @event.PartitionKey);

        var schema = await BuildProjectionDocumentSchemaForEntityConfigurationIdAsync(
            entityInstance.EntityConfigurationId
        );

        await SetDocumentUpdatedAt(schema, @event.AggregateId, @event.PartitionKey, @event.UpdatedAt);
    }

    public async Task On(EntityCategoryPathChanged @event)
    {
        var projectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationIdAsync(
            @event.EntityConfigurationId
        ).ConfigureAwait(false);

        await UpdateDocument(
            projectionDocumentSchema,
            @event.AggregateId,
            @event.PartitionKey,
            @event.Timestamp,
            (document) =>
            {
                var categoryPaths = document["CategoryPaths"] as List<CategoryPath> ?? new List<CategoryPath>();
                    var categoryPath = categoryPaths.FirstOrDefault(x => x.TreeId == @event.CategoryTreeId);
                    if (categoryPath == null)
                    {
                        categoryPaths.Add(new CategoryPath()
                        {
                            Path = @event.CategoryPath,
                            TreeId = @event.CategoryTreeId
                        });
                    }
                    else
                    {
                        categoryPath.Path = @event.CategoryPath;
                    }
                    document["CategoryPaths"] = categoryPaths;
            }
        ).ConfigureAwait(false);
    }
}
