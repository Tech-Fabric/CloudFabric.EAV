using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

namespace CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;

public class EntityInstanceProjectionBuilder : ProjectionBuilder<EntityInstanceProjectionDocument>,
    IHandleEvent<EntityInstanceCreated>,
    IHandleEvent<EntityInstanceCategoryPathChanged>,
    IHandleEvent<AttributeInstanceAdded>,
    IHandleEvent<AttributeInstanceUpdated>,
    IHandleEvent<AttributeInstanceRemoved>
{
    public EntityInstanceProjectionBuilder(IProjectionRepository<EntityInstanceProjectionDocument> repository) : base(repository)
    {
    }

    public async Task On(AttributeInstanceAdded @event)
    {
        await UpdateDocument(@event.EntityInstanceId,
            @event.PartitionKey,
            async document =>
            {
                var currentCategoryPath = document.CategoryPath;
                List<AttributeInstance> attributes = document.Attributes;
                attributes.Add(@event.AttributeInstance);
                await UpdateChildren(@event.EntityInstanceId, currentCategoryPath, @event.PartitionKey, attributes);
            }
        );
    }

    public async Task On(AttributeInstanceRemoved @event)
    {
        await UpdateDocument(@event.EntityInstanceId,
            @event.PartitionKey,
            async document =>
            {
                List<AttributeInstance> attributes = document.Attributes;
                AttributeInstance? attributeToRemove = attributes.FirstOrDefault(x => x.ConfigurationAttributeMachineName == @event.AttributeMachineName);

                if (attributeToRemove != null)
                {
                    attributes.Remove(attributeToRemove);
                    await UpdateChildren(@event.EntityInstanceId, document.CategoryPath, @event.PartitionKey, attributes);
                }
            }
        );
    }
    public async Task On(AttributeInstanceUpdated @event)
    {
        await UpdateDocument(@event.EntityInstanceId,
            @event.PartitionKey,
            async document =>
            {
                List<AttributeInstance>? attributes = document.Attributes;
                var attributeToUpdate = attributes?.FirstOrDefault(x => x.ConfigurationAttributeMachineName == @event.AttributeInstance.ConfigurationAttributeMachineName);

                if (attributeToUpdate != null)
                {
                    attributes.Remove(attributeToUpdate);
                    attributes.Add(@event.AttributeInstance);
                    var currentCategoryPath = document.CategoryPath;
                    await UpdateChildren(@event.EntityInstanceId, currentCategoryPath, @event.PartitionKey, attributes);
                }
            }
        );
    }

    public async Task On(EntityInstanceCategoryPathChanged @event)
    {
        EntityInstanceProjectionDocument? item = await Repository.Single(@event.Id, @event.PartitionKey, CancellationToken.None);
        var currentCategoryPath = item?.CategoryPath;
        await UpdateDocument(@event.Id,
            @event.PartitionKey,
            async document =>
            {
                document.CategoryPath = @event.NewCategoryPath;
                await UpdateChildren(@event.Id, currentCategoryPath, @event.PartitionKey, newCategoryPath: @event.NewCategoryPath);
            });
    }

    public async Task On(EntityInstanceCreated @event)
    {
        var newProjection = new EntityInstanceProjectionDocument
        {
            Id = @event.Id,
            EntityConfigurationId = @event.EntityConfigurationId,
            Attributes = @event.Attributes,
            CategoryPath = @event.CategoryPath,
            PartitionKey = @event.PartitionKey
        };
        var parentIds = @event.CategoryPath.Split(Path.DirectorySeparatorChar);
        // Or we can select only the last parent and user attribute + parentAttributes from it
        // As it already contains all the parents attributes
        //var parents = new List<EntityInstanceProjectionDocument>();

        var lastParentId = parentIds.LastOrDefault();
        if (lastParentId != null)
        {
            EntityInstanceProjectionDocument? lastParent = await Repository.Single(new Guid(lastParentId), @event.PartitionKey, CancellationToken.None);
            if (lastParent != null)
            {
                List<KeyValuePair<string, List<AttributeInstance>>> parentalAttributes = lastParent.ParentalAttributes
                                                                                         ?? new List<KeyValuePair<string, List<AttributeInstance>>>();
                parentalAttributes.Add(new KeyValuePair<string, List<AttributeInstance>>(lastParentId, lastParent.Attributes));
                newProjection.ParentalAttributes = parentalAttributes;
            }
        }
        await UpsertDocument(newProjection, @event.PartitionKey);
    }

    private async Task UpdateChildren(Guid instanceId, string currentCategoryPath, string partitionKey, List<AttributeInstance>? newAttributes = null, string newCategoryPath = "")
    {

        List<string> idsToRemove = null;
        List<string>? idsToAdd = null;
        if (!string.IsNullOrEmpty(newCategoryPath) && newCategoryPath != currentCategoryPath)
        {
            var currentCategoryIds = currentCategoryPath.Split(Path.DirectorySeparatorChar).ToList();
            var newCategoryIds = newCategoryPath.Split(Path.DirectorySeparatorChar).ToList();
            idsToRemove = currentCategoryIds.Except(newCategoryIds).ToList();
            idsToAdd = newCategoryIds.Except(currentCategoryIds).ToList();
        }
        var children = await Repository.Query(new ProjectionQuery
        {
            Filters = new List<Filter>
            {
                new Filter
                {
                    PropertyName = nameof(EntityInstanceProjectionDocument.CategoryPath),
                    Operator = FilterOperator.StartsWith,
                    Value = currentCategoryPath
                }
            }
        }); // REFACTOR

        if (children.Any())
        {
            foreach (EntityInstanceProjectionDocument child in children)
            {

                List<KeyValuePair<string, List<AttributeInstance>>> parentalAttributes = child.ParentalAttributes ?? new List<KeyValuePair<string, List<AttributeInstance>>>();
                if (idsToAdd != null)
                {
                    foreach (var id in idsToAdd)
                    {
                        EntityInstanceProjectionDocument? parent = await Repository.Single(new Guid(id), partitionKey, CancellationToken.None);
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

                await UpdateDocument((Guid)child.Id!,
                    child.PartitionKey!,
                    document =>
                    {
                        document.CategoryPath = string.IsNullOrEmpty(newCategoryPath) ? currentCategoryPath : newCategoryPath;
                        document.ParentalAttributes = parentalAttributes;
                    });
            }
        }
    }
}