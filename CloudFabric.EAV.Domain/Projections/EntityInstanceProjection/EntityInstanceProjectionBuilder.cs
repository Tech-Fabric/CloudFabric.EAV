using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

using ProjectionDocumentSchemaFactory = CloudFabric.EAV.Domain.Projections.EntityInstanceProjection.ProjectionDocumentSchemaFactory;

namespace CloudFabric.EAV.Domain.LocalEventSourcingPackages.Projections.EntityInstanceProjection;

public class EntityInstanceProjectionBuilder : ProjectionBuilder,
    IHandleEvent<EntityInstanceCreated>,
    IHandleEvent<AggregateUpdatedEvent<EntityInstance>>
    // IHandleEvent<AttributeInstanceAdded>,
    // IHandleEvent<AttributeInstanceUpdated>,
    // IHandleEvent<AttributeInstanceRemoved>
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
        Guid entityConfigurationId,
        List<AttributeConfiguration>? parentAttributeConfigurations)
    {
        var entityConfiguration = await _aggregateRepositoryFactory
            .GetAggregateRepository<EntityConfiguration>()
            .LoadAsyncOrThrowNotFound(entityConfigurationId, entityConfigurationId.ToString());

        List<AttributeConfiguration> attributes = await BuildAttributesListFromAttributesReferences(entityConfiguration.Attributes.ToList());

        return ProjectionDocumentSchemaFactory.FromEntityConfiguration(entityConfiguration, attributes, parentAttributeConfigurations);
    }

    private async Task<List<AttributeConfiguration>> BuildAttributesListFromAttributesReferences(List<EntityConfigurationAttributeReference> references)
    {
        List<AttributeConfiguration> attributes = new List<AttributeConfiguration>();
        //TODO optimize this
        foreach (var attributeReference in references)
        {  
            var attribute = await _aggregateRepositoryFactory
                .GetAggregateRepository<AttributeConfiguration>()
                .LoadAsyncOrThrowNotFound(attributeReference.AttributeConfigurationId,
                    attributeReference.AttributeConfigurationId.ToString()
                );
            attributes.Add(attribute);
        }
        return attributes;
    }
    public async Task On(EntityInstanceCreated @event)
    {
        var parentIds = @event.CategoryPath.Split("/").ToList();
        var allParentalAttributesConfigurations = new List<AttributeConfiguration>();
        var allParentalAttributes = new Dictionary<string, object?>();
        
        if (parentIds.Any())
        {
            var repo = _aggregateRepositoryFactory
                .GetAggregateRepository<EntityInstance>();
            foreach (var parentIdString in parentIds)
            {
                if (!string.IsNullOrEmpty(parentIdString))
                {
                    var parentEntityInstance = await repo.LoadAsyncOrThrowNotFound(Guid.Parse(parentIdString), parentIdString);
                    var entityConfiguration = await _aggregateRepositoryFactory
                        .GetAggregateRepository<EntityConfiguration>()
                        .LoadAsyncOrThrowNotFound(parentEntityInstance.EntityConfigurationId, parentEntityInstance.EntityConfigurationId.ToString());

                    List<AttributeConfiguration> attributes = await BuildAttributesListFromAttributesReferences(entityConfiguration.Attributes.ToList());
                
                    allParentalAttributesConfigurations.AddRange(attributes);
                
                    //TODO: rewrite on linq
                    foreach (var attribute in parentEntityInstance.Attributes)
                    {
                        allParentalAttributes.Add(attribute.ConfigurationAttributeMachineName, attribute.GetValue());
                    }
                }
            }
        }
        
        var projectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
            @event.EntityConfigurationId,
            allParentalAttributesConfigurations
        );

        var document = new Dictionary<string, object?>()
        {
            { "Id", @event.Id },
            { "EntityConfigurationId", @event.EntityConfigurationId },
            { "TenantId", @event.TenantId },
            {"CategoryPath", @event.CategoryPath},
            {"ParentalAttributes", allParentalAttributes},
            {"Attributes", new Dictionary<string, object?>()}
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

    private async Task UpdateChildren(Guid instanceId, Guid entityConfigurationId, string currentCategoryPath, string partitionKey, List<AttributeInstance>? newAttributes = null, string newCategoryPath = "")
    {
        var projectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
            entityConfigurationId,
            null
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
                    child.EntityConfigurationId,
                    null
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

    public async Task On(AggregateUpdatedEvent<EntityInstance> @event)
    {
        var entityInstance = await _aggregateRepositoryFactory
            .GetAggregateRepository<EntityInstance>()
            .LoadAsyncOrThrowNotFound(@event.AggregateId!.Value, @event.AggregateId!.Value.ToString());

        var schema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
            entityInstance.EntityConfigurationId
        );

        await SetDocumentUpdatedAt(schema, @event.AggregateId!.Value, @event.PartitionKey, @event.UpdatedAt);
    }
}
