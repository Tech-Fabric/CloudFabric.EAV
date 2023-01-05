using System.Collections.ObjectModel;

using CloudFabric.EAV.Domain.LocalEventSourcingPackages.Events.Category;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

namespace CloudFabric.EAV.Domain.LocalEventSourcingPackages.Projections.CategoryInstanceProjection
{
    public class CategoryInstanceProjectionBuilder : InstanceProjectionBuilder,
        IHandleEvent<CategoryCreated>
    {

        protected CategoryInstanceProjectionBuilder(AggregateRepositoryFactory aggregateRepositoryFactory, ProjectionRepositoryFactory projectionRepositoryFactory) : base(projectionRepositoryFactory, aggregateRepositoryFactory)
        {
        }

        public async Task On(CategoryCreated @event)
        {
            await CreateInstanceProjection(@event.AggregateId,
                @event.EntityConfigurationId,
                @event.TenantId,
                @event.CategoryPath,
                @event.Attributes,
                @event.PartitionKey,
                @event.Timestamp);
        }


        private async Task UpdateChildrenEntities(Guid instanceId,
            ProjectionDocumentSchema categorySchema,
            Guid childrenConfigurationId,
            string partitionKey,
            string currentCategoryPath,
            List<AttributeInstance>? newAttributes = null,
            string newCategoryPath = "")
        {
            var childrenCategoryPath = currentCategoryPath + $"/{instanceId}";
            
            (List<AttributeConfiguration> allBranchAttributesConfigurations, Dictionary<string, object> allBranchAttributes) = await BuildBranchAttributes(childrenCategoryPath);

            var childProjectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
                childrenConfigurationId,
                allBranchAttributesConfigurations
            );

            // Manage changes in the CategoryPath
            List<string> idsToRemove = null;
            List<string>? idsToAdd = null;
            if (!string.IsNullOrEmpty(newCategoryPath) && newCategoryPath != currentCategoryPath)
            {
                var currentCategoryIds = currentCategoryPath.Split(Path.DirectorySeparatorChar).ToList();
                var newCategoryIds = newCategoryPath.Split(Path.DirectorySeparatorChar).ToList();
                idsToRemove = currentCategoryIds.Except(newCategoryIds).ToList();
                idsToAdd = newCategoryIds.Except(currentCategoryIds).ToList();
            }

            var childrenRepo = ProjectionRepositoryFactory.GetProjectionRepository(childProjectionDocumentSchema);
            var parenRepo = _aggregateRepositoryFactory.GetAggregateRepository<EntityCategory>();
            
            var children = await childrenRepo.Query(new ProjectionQuery
            {
                Filters = new List<Filter>
                {
                    new Filter
                    {
                        PropertyName = "CategoryPath",
                        Operator = FilterOperator.Equal,
                        Value = childrenCategoryPath
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
                    Dictionary<string, object> parentalAttributes = child["ParentalAttributes"] as Dictionary<string, object> ?? new Dictionary<string, object>();
                    if (idsToAdd != null)
                    {
                        foreach (var id in idsToAdd)
                        {
                            (List<AttributeConfiguration> parentAttributesConfigurations, ReadOnlyCollection<AttributeInstance> parentAttributes) = await ExtractAttributesAndAttributeConfigurations(id);
                            allBranchAttributesConfigurations.AddRange(parentAttributesConfigurations);
                            foreach (var attribute in parentAttributes)
                            {
                                allBranchAttributes[attribute.ConfigurationAttributeMachineName] = attribute.GetValue();
                            }
                        }
                    }
    
                    if (idsToRemove != null)
                    {
                        
                        foreach (var id in idsToRemove)
                        {
                            (List<AttributeConfiguration> parentAttributesConfigurations, ReadOnlyCollection<AttributeInstance> parentAttributes) = await ExtractAttributesAndAttributeConfigurations(id);
                            allBranchAttributesConfigurations.RemoveAll(ac => parentAttributesConfigurations.Contains(ac.MachineName))               
                            parentalAttributes.Remove(parentalAttributes.FirstOrDefault(x => x.Key == id));
                        }
                    }
                    if (newAttributes != null)
                    {
                        parentalAttributes.Remove(parentalAttributes.FirstOrDefault(x => x.Key == instanceId.ToString()));
                        parentalAttributes.Add(new KeyValuePair<string, List<AttributeInstance>>(instanceId.ToString(), newAttributes));
                    }
    
                    // TODO: cache 
                    childProjectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
                        child.EntityConfigurationId,
                        null
                    );
                    await UpdateDocument(childProjectionDocumentSchema,
                        child.Id.Value,
                        child.PartitionKey!,
                        updatedAt: DateTime.Now, 
                        dict =>
                        {
                            dict["ParentalAttributes"] = parentalAttributes;
                            dict["CategoryPath"] = string.IsNullOrEmpty(newCategoryPath) ? currentCategoryPath : newCategoryPath;
                        });
                }
            }
            */
        }

    }
}