using CloudFabric.EAV.Domain.Events.Instance.Attribute;
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
        EntityInstance entityInstance = await _aggregateRepositoryFactory
            .GetAggregateRepository<EntityInstance>()
            .LoadAsyncOrThrowNotFound(@event.AggregateId, @event.PartitionKey);

        ProjectionDocumentSchema schema = await BuildProjectionDocumentSchemaForEntityConfigurationIdAsync(
            entityInstance.EntityConfigurationId
        );

        await SetDocumentUpdatedAt(schema, @event.AggregateId, @event.PartitionKey, @event.UpdatedAt);
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
        ProjectionDocumentSchema projectionDocumentSchema =
            await BuildProjectionDocumentSchemaForEntityConfigurationIdAsync(
                @event.EntityConfigurationId
            ).ConfigureAwait(false);

        await UpdateDocument(
            projectionDocumentSchema,
            @event.AggregateId,
            @event.PartitionKey,
            @event.Timestamp,
            document =>
            {
                document[@event.AttributeInstance.ConfigurationAttributeMachineName] =
                    @event.AttributeInstance.GetValue();
            }
        ).ConfigureAwait(false);
    }

    public async Task On(EntityCategoryPathChanged @event)
    {
        ProjectionDocumentSchema projectionDocumentSchema =
            await BuildProjectionDocumentSchemaForEntityConfigurationIdAsync(
                @event.EntityConfigurationId
            ).ConfigureAwait(false);

        await UpdateDocument(
            projectionDocumentSchema,
            @event.AggregateId,
            @event.PartitionKey,
            @event.Timestamp,
            document =>
            {
                document.TryGetValue("CategoryPaths", out var categoryPathsObj);
                List<CategoryPath> categoryPaths =
                    categoryPathsObj as List<CategoryPath> ?? new List<CategoryPath>();
                CategoryPath? categoryPath = categoryPaths.FirstOrDefault(x => x.TreeId == @event.CategoryTreeId);
                if (categoryPath == null)
                {
                    categoryPaths.Add(new CategoryPath { Path = @event.CategoryPath, TreeId = @event.CategoryTreeId }
                    );
                }
                else
                {
                    categoryPath.Path = @event.CategoryPath;
                }

                document["CategoryPaths"] = categoryPaths;
            }
        ).ConfigureAwait(false);
    }

    public async Task On(EntityInstanceCreated @event)
    {
        ProjectionDocumentSchema projectionDocumentSchema =
            await BuildProjectionDocumentSchemaForEntityConfigurationIdAsync(
                @event.EntityConfigurationId
            ).ConfigureAwait(false);

        var document = new Dictionary<string, object?>
        {
            { "Id", @event.AggregateId },
            { "EntityConfigurationId", @event.EntityConfigurationId },
            { "TenantId", @event.TenantId },
            { "CategoryPaths", new List<CategoryPath>() }
        };

        foreach (AttributeInstance attribute in @event.Attributes)
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

    private async Task<ProjectionDocumentSchema> BuildProjectionDocumentSchemaForEntityConfigurationIdAsync(
        Guid entityConfigurationId
    )
    {
        EntityConfiguration entityConfiguration = await _aggregateRepositoryFactory
            .GetAggregateRepository<EntityConfiguration>()
            .LoadAsyncOrThrowNotFound(entityConfigurationId, entityConfigurationId.ToString())
            .ConfigureAwait(false);

        var attributes = new List<AttributeConfiguration>();

        foreach (EntityConfigurationAttributeReference attributeReference in entityConfiguration.Attributes)
        {
            AttributeConfiguration attribute = await _aggregateRepositoryFactory
                .GetAggregateRepository<AttributeConfiguration>()
                .LoadAsyncOrThrowNotFound(attributeReference.AttributeConfigurationId,
                    attributeReference.AttributeConfigurationId.ToString()
                )
                .ConfigureAwait(false);
            attributes.Add(attribute);
        }

        return ProjectionDocumentSchemaFactory.FromEntityConfiguration(entityConfiguration, attributes);
    }
}
