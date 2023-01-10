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
            List<EntityConfigurationAttributeReference>? newAttributesReferences = null,

            string newCategoryPath = "")
        {
            // For children it's category path + category Id
            var childrenCategoryPath = currentCategoryPath + $"/{instanceId}";
            
            // Get all branch attributes and their configurations
            (List<AttributeConfiguration> allBranchAttributesConfigurations, Dictionary<string, object> allBranchAttributes) = await BuildBranchAttributes(childrenCategoryPath);

            // Build children document schema considering all branch attributes as parent attributes
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

            // Get all children entities on this level of tree
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
                
                // Update children one by one
                foreach (var resultDocument in children.Records)
                {
                    var child = resultDocument.Document;
                    if (child == null)
                    {
                        //TODO: Log error
                    }
                    
                    // Get current child parental attributes
                    Dictionary<string, object> parentalAttributes = child["ParentalAttributes"] as Dictionary<string, object> ?? new Dictionary<string, object>();
                    
                    // Add to it all attributes from the new branch levels (e.g. if there is a new category in the the middle of the path)
                    if (idsToAdd != null)
                    {
                        // for each new level
                        foreach (var id in idsToAdd)
                        {
                            // get all attributes and configurations from the new level
                            (List<AttributeConfiguration> parentAttributesConfigurations, ReadOnlyCollection<AttributeInstance> parentAttributes) = await ExtractAttributesAndAttributeConfigurations(id);
                            
                            // add them to the global pull of configurations and attributes
                            allBranchAttributesConfigurations.AddRange(parentAttributesConfigurations);
                            foreach (var attribute in parentAttributes)
                            {
                                allBranchAttributes[attribute.ConfigurationAttributeMachineName] = attribute.GetValue();
                            }
                        }
                    }
                    
                    // Remove all attributes and configs that are no longer in the new branch
                    
                    if (idsToRemove != null)
                    {
                        
                        foreach (var id in idsToRemove)
                        {
                            (List<AttributeConfiguration> parentAttributesConfigurations, ReadOnlyCollection<AttributeInstance> parentAttributes) = await ExtractAttributesAndAttributeConfigurations(id);
                            allBranchAttributesConfigurations.RemoveAll(ac => parentAttributesConfigurations.Any(pac => pac.MachineName == ac.MachineName));
                            foreach (AttributeInstance ai in parentAttributes)
                            {
                                parentalAttributes.Remove(ai.ConfigurationAttributeMachineName);
                            }
                        }
                    }
                    
                    // If there are new attributes in the CURRENT category itself add them to the global pull as well 
                    if (newAttributes != null && newAttributesReferences != null)
                    {
                        var newAttributeConfigs = await BuildAttributesListFromAttributesReferences(newAttributesReferences);
                        allBranchAttributesConfigurations.AddRange(newAttributeConfigs);
                        foreach (var attribute in newAttributes)
                        {
                            allBranchAttributes[attribute.ConfigurationAttributeMachineName] = attribute.GetValue();
                        }
                    }
                    
                    // Build a new schema for the child considering updated list of branch attributes and configs
                    childProjectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
                        childrenConfigurationId,
                        allBranchAttributesConfigurations
                    );

                    await UpdateDocument(childProjectionDocumentSchema,
                        new Guid(child["Id"] as string) ,
                        child["PartitionKey"] as string,
                        updatedAt: DateTime.Now,
                        dict =>
                        {
                            dict["ParentalAttributes"] = parentalAttributes;
                            dict["CategoryPath"] = string.IsNullOrEmpty(newCategoryPath) ? childrenCategoryPath : newCategoryPath + $"/{instanceId}";
                        });
                }
            }
        }

    }
}