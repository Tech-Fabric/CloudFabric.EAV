using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

namespace CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;

public class EntityInstanceProjectionBuilder : ProjectionBuilder,
    IHandleEvent<EntityInstanceCreated>,
    IHandleEvent<EntityInstanceCategoryPathChanged>,
    IHandleEvent<AttributeInstanceAdded>,
    IHandleEvent<AttributeInstanceUpdated>,
    IHandleEvent<AttributeInstanceRemoved>
{
    public EntityInstanceProjectionBuilder(IProjectionRepository repository) : base(repository)
    {
    }

    public async Task On(AttributeInstanceAdded @event)
    {
        await UpdateDocument(@event.EntityInstanceId,
            @event.PartitionKey,
            async document =>
            {
                var attributes = document[nameof(EntityInstanceProjectionDocument.Attributes)] as List<AttributeInstance>;
                var currentCategoryPath = document[nameof(EntityInstanceProjectionDocument.CategoryPath)] as string;
                attributes ??= new();

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
                var attributes = document[nameof(EntityInstanceProjectionDocument.Attributes)] as List<AttributeInstance>;
                AttributeInstance? attributeToRemove = attributes?.FirstOrDefault(x => x.ConfigurationAttributeMachineName == @event.AttributeMachineName);

                if (attributeToRemove != null)
                {
                    attributes.Remove(attributeToRemove);
                    var currentCategoryPath = document[nameof(EntityInstanceProjectionDocument.CategoryPath)] as string;
                    await UpdateChildren(@event.EntityInstanceId, currentCategoryPath, @event.PartitionKey, attributes);
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
                var attributes = document[nameof(EntityInstanceProjectionDocument.Attributes)] as List<AttributeInstance>;
                var attributeToUpdate = attributes?.FirstOrDefault(x => x.ConfigurationAttributeMachineName == @event.AttributeInstance.ConfigurationAttributeMachineName);

                if (attributeToUpdate != null)
                {
                    attributes.Remove(attributeToUpdate);
                    attributes.Add(@event.AttributeInstance);
                    var currentCategoryPath = document[nameof(EntityInstanceProjectionDocument.CategoryPath)] as string;
                    await UpdateChildren(@event.EntityInstanceId, currentCategoryPath, @event.PartitionKey, attributes);
                }
            }
        );
    }

    public async Task On(EntityInstanceCategoryPathChanged @event)
    {
        Dictionary<string, object?>? item = await Repository.Single(@event.Id, @event.PartitionKey, CancellationToken.None);
        var currentCategoryPath = item?[nameof(EntityInstanceProjectionDocument.CategoryPath)] as string;
        await UpdateDocument(@event.Id,
            @event.PartitionKey,
            document =>
            {
                document[nameof(EntityInstanceProjectionDocument.CategoryPath)] = @event.NewCategoryPath;
            });
        await UpdateChildren(@event.Id, currentCategoryPath, @event.PartitionKey, newCategoryPath: @event.NewCategoryPath);
    }

    public async Task On(EntityInstanceCreated @event)
    {
        var newProjectionDictionary = new Dictionary<string, object?>
        {
            {
                nameof(EntityInstanceProjectionDocument.Id), @event.Id
            },
            {
                nameof(EntityInstanceProjectionDocument.EntityConfigurationId), @event.EntityConfigurationId
            },
            {
                nameof(EntityInstanceProjectionDocument.Attributes), @event.Attributes
            },
            {
                nameof(EntityInstanceProjectionDocument.CategoryPath), @event.CategoryPath
            }
        };
        var parentIds = @event.CategoryPath.Split(Path.DirectorySeparatorChar);
        // Or we can select only the last parent and user attribute + parentAttributes from it
        // As it already contains all the parents attributes
        //var parents = new List<EntityInstanceProjectionDocument>();

        var lastParentId = parentIds.LastOrDefault();
        if (lastParentId != null)
        {
            Dictionary<string, object?>? lastParent = await Repository.Single(new Guid(lastParentId), @event.PartitionKey, CancellationToken.None);
            if (lastParent != null)
            {
                Dictionary<string, List<AttributeInstance>?> parentalAttributes = lastParent[nameof(EntityInstanceProjectionDocument.ParentalAttributes)] as Dictionary<string, List<AttributeInstance>?>
                                                                                  ?? new Dictionary<string, List<AttributeInstance>?>();
                parentalAttributes[lastParentId] = lastParent[nameof(EntityInstanceProjectionDocument.ParentalAttributes)] as List<AttributeInstance>;
                newProjectionDictionary[nameof(EntityInstanceProjectionDocument.ParentalAttributes)] = parentalAttributes;
            }
        }
        await UpsertDocument(newProjectionDictionary, @event.PartitionKey);
    }

    private async Task UpdateChildren(Guid instanceId, string currentCategoryPath, string partitionKey, List<AttributeInstance>? newAttributes = null, string newCategoryPath = "")
    {

        List<string> idsToRemove = null;
        List<string>? idsToAdd = null;
        if (newCategoryPath != currentCategoryPath)
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
        }) as List<EntityInstanceProjectionDocument>;

        if (children != null && children.Any())
        {
            foreach (EntityInstanceProjectionDocument child in children)
            {

                Dictionary<string, List<AttributeInstance>?> parentalAttributes = child.ParentalAttributes;
                if (idsToAdd != null)
                {
                    foreach (var id in idsToAdd)
                    {
                        Dictionary<string, object?>? parent = await Repository.Single(new Guid(id), partitionKey, CancellationToken.None);
                        parentalAttributes[id] = parent?[nameof(EntityInstanceProjectionDocument.Attributes)] as List<AttributeInstance>;
                    }
                }

                if (idsToRemove != null)
                {
                    foreach (var id in idsToRemove)
                    {
                        parentalAttributes.Remove(id);
                    }
                }
                if (newAttributes != null)
                {
                    parentalAttributes[instanceId.ToString()] = newAttributes;
                }

                await UpdateDocument((Guid)child.Id,
                    child.PartitionKey,
                    document =>
                    {
                        document[nameof(EntityInstanceProjectionDocument.CategoryPath)] = string.IsNullOrEmpty(newCategoryPath) ? currentCategoryPath : newCategoryPath;
                        document[nameof(EntityInstanceProjectionDocument.ParentalAttributes)] = parentalAttributes;
                    });
            }
        }
    }
}