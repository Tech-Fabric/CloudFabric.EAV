using CloudFabric.EAV.Domain.Events.Instance.Attribute;
using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.Projections;

namespace CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;

/// <summary>
/// Entities are stored as c# dictionaries in projections - something similar to json.
/// That is required to not overload search engines with additional complexity of entity instances and attributes
/// allowing us to simply store
/// photo.likes = 4 instead of photo.attributes.where(a => a.machineName == "likes").value = 4
///
/// That comes with a price though - we now have to decode json-like dictionary back to entity instance view model.
/// Also it becomes not clear where is a serialization part and where is a deserializer.
///
/// The following structure seems logical, not very understandable from the first sight however:
///
///
/// Serialization happens in <see cref="CloudFabric.EAV.Domain/Projections/EntityInstanceProjection/EntityInstanceProjectionBuilder.cs"/>
/// Projection builder creates dictionaries from EntityInstances and is responsible for storing projections data in
/// the best way suitable for search engines like elasticsearch.
///
/// The segregation of reads and writes moves our decoding code out of ProjectionBuilder
/// and even out of CloudFabric.EAV.Domain because our ViewModels are on another layer - same layer as a service.
/// That means it's a service concern to decode dictionary into a ViewModel.
///
/// <see cref="CloudFabric.EAV.Service/Serialization/EntityInstanceFromDictionaryDeserializer.cs"/>
/// </summary>
public class EntityInstanceProjectionBuilder : ProjectionBuilder,
    IHandleEvent<EntityInstanceCreated>,
    IHandleEvent<CategoryCreated>,
// IHandleEvent<AttributeInstanceAdded>,
    IHandleEvent<AttributeInstanceUpdated>,
    // IHandleEvent<AttributeInstanceRemoved>,
    IHandleEvent<EntityCategoryPathChanged>,
    IHandleEvent<AggregateUpdatedEvent<EntityInstance>>
{
    private readonly AggregateRepositoryFactory _aggregateRepositoryFactory;

    public EntityInstanceProjectionBuilder(
        ProjectionRepositoryFactory projectionRepositoryFactory,
        AggregateRepositoryFactory aggregateRepositoryFactory
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
                    categoryPaths.Add(new CategoryPath { TreeId = @event.CategoryTreeId,
                        Path = @event.CategoryPath,
                        ParentId = @event.ParentId,
                        ParentMachineName = @event.ParentMachineName
                    });
                }
                else
                {
                    categoryPath.Path = @event.CategoryPath;
                    categoryPath.ParentMachineName = @event.ParentMachineName;
                    categoryPath.ParentId = @event.ParentId;
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

    public async Task On(CategoryCreated @event)
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
            { "CategoryPaths", new List<CategoryPath>() },
            { "MachineName", @event.MachineName},
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
