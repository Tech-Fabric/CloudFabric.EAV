using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

namespace CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;

public class EntityInstanceProjectionBuilder : ProjectionBuilder,
    IHandleEvent<EntityInstanceCreated>
//    IHandleEvent<AttributeInstanceAdded>,
//    IHandleEvent<AttributeInstanceUpdated>,
//    IHandleEvent<AttributeInstanceRemoved>,
//    IHandleEvent<EntityInstanceCategoryPathChanged>
{
    private readonly AggregateRepositoryFactory _aggregateRepositoryFactory;

    public EntityInstanceProjectionBuilder(
        AggregateRepositoryFactory aggregateRepositoryFactory,
        ProjectionRepositoryFactory projectionRepositoryFactory
    ) : base(projectionRepositoryFactory)
    {
        _aggregateRepositoryFactory = aggregateRepositoryFactory;
    }

    private async Task<ProjectionDocumentSchema> BuildProjectionDocumentSchemaForEntityConfigurationId(
        Guid entityConfigurationId
    )
    {
        var entityConfiguration = await _aggregateRepositoryFactory
            .GetAggregateRepository<EntityConfiguration>()
            .LoadAsyncOrThrowNotFound(entityConfigurationId, entityConfigurationId.ToString());

        List<AttributeConfiguration> attributes = new List<AttributeConfiguration>();

        foreach (var attributeReference in entityConfiguration.Attributes)
        {
            var attribute = await _aggregateRepositoryFactory
                .GetAggregateRepository<AttributeConfiguration>()
                .LoadAsyncOrThrowNotFound(attributeReference.AttributeConfigurationId,
                    attributeReference.AttributeConfigurationId.ToString()
                );
            attributes.Add(attribute);
        }

        return ProjectionDocumentSchemaFactory.FromEntityConfiguration(entityConfiguration, attributes, null);
    }

    public async Task On(EntityInstanceCreated @event)
    {
        var projectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
            @event.EntityConfigurationId
        );

        var document = new Dictionary<string, object?>()
        {
            { "Id", @event.Id },
            { "EntityConfigurationId", @event.EntityConfigurationId },
            { "TenantId", @event.TenantId },
            {"CategoryPath", @event.CategoryPath}
        };
        
        foreach (var attribute in @event.Attributes)
        {
            document.Add(attribute.ConfigurationAttributeMachineName, attribute.GetValue());
        }

        // TODO: Build parental attributes
        
        await UpsertDocument(
            projectionDocumentSchema,
            document,
            @event.PartitionKey
        );
    }

    private async Task UpdateChildren(Guid instanceId, Guid entityConfigurationId, string currentCategoryPath, string partitionKey, List<AttributeInstance>? newAttributes = null, string newCategoryPath = "")
    {
        var projectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
            entityConfigurationId
        );
        List<string> idsToRemove = null;
        List<string>? idsToAdd = null;
        if (!string.IsNullOrEmpty(newCategoryPath) && newCategoryPath != currentCategoryPath)
        {
            var currentCategoryIds = currentCategoryPath.Split(Path.DirectorySeparatorChar).ToList();
            var newCategoryIds = newCategoryPath.Split(Path.DirectorySeparatorChar).ToList();
            idsToRemove = currentCategoryIds.Except(newCategoryIds).ToList();
            idsToAdd = newCategoryIds.Except(currentCategoryIds).ToList();
        }
        var repo = ProjectionRepositoryFactory.GetProjectionRepository<EntityInstanceProjectionDocument>();
        var children = await repo.Query(new ProjectionQuery
        {
            Filters = new List<Filter>
            {
                new Filter
                {
                    PropertyName = "CategoryPath",
                    Operator = FilterOperator.StartsWith,
                    Value = currentCategoryPath
                }
            }
        });

        if (children.TotalRecordsFound > 0)
        {
            foreach (var resultDocument in children.Records)
            {
                var child = resultDocument.Document;
                if (child == null)
                {
                    //TODO: Log error
                }
                List<KeyValuePair<string, List<AttributeInstance>>> parentalAttributes = child?.ParentalAttributes ?? new List<KeyValuePair<string, List<AttributeInstance>>>();
                if (idsToAdd != null)
                {
                    foreach (var id in idsToAdd)
                    {
                        EntityInstanceProjectionDocument? parent = await repo.Single(new Guid(id), partitionKey, CancellationToken.None);
                        parentalAttributes.Add(new KeyValuePair<string, List<AttributeInstance>>(id, parent?.Attributes));
                    }
                }

                if (idsToRemove != null)
                {
                    foreach (var id in idsToRemove)
                    {
                        parentalAttributes.Remove(parentalAttributes.FirstOrDefault(x => x.Key == id));
                    }
                }
                if (newAttributes != null)
                {
                    parentalAttributes.Remove(parentalAttributes.FirstOrDefault(x => x.Key == instanceId.ToString()));
                    parentalAttributes.Add(new KeyValuePair<string, List<AttributeInstance>>(instanceId.ToString(), newAttributes));
                }

                // TODO: cache 
                var childProjectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
                    child.EntityConfigurationId
                );
                await UpdateDocument(childProjectionDocumentSchema,
                    child.Id.Value,
                    child.PartitionKey!,
                    dict =>
                    {
                        dict["ParentalAttributes"] = parentalAttributes;
                        dict["CategoryPath"] = string.IsNullOrEmpty(newCategoryPath) ? currentCategoryPath : newCategoryPath;
                    });
            }
        }
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
    // public async Task On(AttributeInstanceUpdated @event)
    // {
    //     await UpdateDocument(@event.EntityInstanceId,
    //         @event.PartitionKey,
    //         (document) =>
    //         {
    //             var attributes =
    //                 document[nameof(EntityInstanceProjectionDocument.Attributes)] as List<AttributeInstance>;
    //             var attributeToUpdate = attributes?.FirstOrDefault(x =>
    //                 x.ConfigurationAttributeMachineName == @event.AttributeInstance.ConfigurationAttributeMachineName
    //             );
    //
    //             if (attributeToUpdate != null)
    //             {
    //                 attributes.Remove(attributeToUpdate);
    //                 attributes.Add(@event.AttributeInstance);
    //             }
    //         }
    //     );
    // }
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
}
