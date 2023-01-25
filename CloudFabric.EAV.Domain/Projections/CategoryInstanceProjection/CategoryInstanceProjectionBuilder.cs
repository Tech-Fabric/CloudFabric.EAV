using System.Collections.ObjectModel;

using CloudFabric.EAV.Domain.LocalEventSourcingPackages.Events.Category;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

namespace CloudFabric.EAV.Domain.LocalEventSourcingPackages.Projections.CategoryInstanceProjection
{
    public class CategoryInstanceProjectionBuilder : InstanceProjectionBuilder,
        IHandleEvent<CategoryCreated>,
        IHandleEvent<CategoryPathChanged>
    {

        public CategoryInstanceProjectionBuilder(AggregateRepositoryFactory aggregateRepositoryFactory, ProjectionRepositoryFactory projectionRepositoryFactory) : base(projectionRepositoryFactory, aggregateRepositoryFactory)
        {
        }

        private async Task<(List<AttributeConfiguration> allParentalAttributesConfigurations, Dictionary<string, object?> allParentalAttributes)> BuildBranchAttributesAsync(string categoryPath)
        {
            var parentIds = categoryPath.Split("/").ToList();
        
            // Parent attributes configurations for schema
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
                        var parentEntityInstance = await repo.LoadAsync(Guid.Parse(parentIdString), parentIdString).ConfigureAwait(false);
                        if (parentEntityInstance != null)
                        {
                            (List<AttributeConfiguration> parentAttributesConfigurations, ReadOnlyCollection<AttributeInstance> parentAttributes) = await ExtractAttributesAndAttributeConfigurationsAsync(parentIdString).ConfigureAwait(false);
                            allParentalAttributesConfigurations.AddRange(parentAttributesConfigurations);
                            //TODO: rewrite on linq
                            foreach (var attribute in parentAttributes)
                            {
                                allParentalAttributes[attribute.ConfigurationAttributeMachineName] = attribute.GetValue();
                            }
                        }
                    }
                }
            }
            return (allParentalAttributesConfigurations, allParentalAttributes);
        }

        private async Task<(List<AttributeConfiguration> attributesConfigurations, ReadOnlyCollection<AttributeInstance> Attributes)> ExtractAttributesAndAttributeConfigurationsAsync(string id)
        {
            var repo = _aggregateRepositoryFactory
                .GetAggregateRepository<EntityInstance>();
            var parentEntityInstance = await repo.LoadAsync(Guid.Parse(id), id).ConfigureAwait(false);
            List<AttributeConfiguration> attributesConfigurations = new List<AttributeConfiguration>();

            if (parentEntityInstance == null)
            {
                return (attributesConfigurations, new ReadOnlyCollection<AttributeInstance>(new List<AttributeInstance>()));
            }
            var entityConfiguration = await _aggregateRepositoryFactory
                .GetAggregateRepository<EntityConfiguration>()
                .LoadAsyncOrThrowNotFound(parentEntityInstance.EntityConfigurationId, parentEntityInstance.EntityConfigurationId.ToString()).ConfigureAwait(false);
            attributesConfigurations = await BuildAttributesListFromAttributesReferences(entityConfiguration.Attributes.ToList()).ConfigureAwait(false);
            return (attributesConfigurations, parentEntityInstance.Attributes);
        }
        
        
        public async Task On(CategoryCreated @event)
        {
            (List<AttributeConfiguration> allParentalAttributesConfigurations, Dictionary<string, object> allParentalAttributes) = await BuildBranchAttributesAsync( @event.CategoryPath).ConfigureAwait(false);

            // Build schema for entity instance considering all parent attributes and their configurations
            var projectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
                @event.EntityConfigurationId,
                allParentalAttributesConfigurations
            ).ConfigureAwait(false);

            var document = new Dictionary<string, object?>()
            {
                {
                    "Id",  @event.AggregateId
                },
                {
                    "EntityConfigurationId",  @event.EntityConfigurationId
                },
                {
                    "TenantId",  @event.TenantId
                },
                {
                    "CategoryPath",  @event.CategoryPath
                },
                {
                    "ParentalAttributes", allParentalAttributes
                },
                {
                    "Attributes", new Dictionary<string, object?>()
                }
            };

            // fill attributes
            foreach (var attribute in  @event.Attributes)
            {
                document.Add(attribute.ConfigurationAttributeMachineName, attribute.GetValue());
            }

            // Add document
            await UpsertDocument(
                projectionDocumentSchema,
                document,
                @event.PartitionKey,
                @event.Timestamp
            ).ConfigureAwait(false);
        }
        
        
        private async Task UpdateChildrenAsync(Guid instanceId,
            ProjectionDocumentSchema categorySchema,
            string currentCategoryPath,
            List<AttributeInstance>? newAttributes = null,
            List<EntityConfigurationAttributeReference>? newAttributesReferences = null,
            string newCategoryPath = "")
        {
            // For children it's category path + category Id
            var childrenCategoryPath = currentCategoryPath + $"/{instanceId}";

            // Manage changes in the CategoryPath
            List<string>? idsToRemove = null;
            List<string>? idsToAdd = null;
            if (!string.IsNullOrEmpty(newCategoryPath) && newCategoryPath != currentCategoryPath)
            {
                var currentCategoryIds = currentCategoryPath.Split(Path.DirectorySeparatorChar).ToList();
                var newCategoryIds = newCategoryPath.Split(Path.DirectorySeparatorChar).ToList();
                idsToRemove = currentCategoryIds.Except(newCategoryIds).ToList();
                idsToAdd = newCategoryIds.Except(currentCategoryIds).ToList();
            }

            // Get all children entities using simplified schema
            var childrenRepo = ProjectionRepositoryFactory.GetProjectionRepository(categorySchema);
            var children = await childrenRepo.Query(new ProjectionQuery
            {
                Filters = new List<Filter>
                {
                    new Filter
                    {
                        PropertyName = "CategoryPath",
                        Operator = FilterOperator.StartsWith,
                        Value = childrenCategoryPath
                    }
                }
            }).ConfigureAwait(false);

            if (children.TotalRecordsFound > 0)
            {
                
                // Update children one by one
                foreach (var resultDocument in children.Records)
                {                    
                    var childDocument = resultDocument.Document;
                    
                    if (childDocument == null)
                    {
                        //TODO: Log error
                    }
                    
                    // Get all branch attributes and their configurations
                    (List<AttributeConfiguration> allBranchAttributesConfigurations, Dictionary<string, object?> allBranchAttributes) = await BuildBranchAttributesAsync(childDocument["CategoryPath"] as string).ConfigureAwait(false);

                    var childProjectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
                        new Guid(childDocument["ConfigurationId"] as string ?? string.Empty),
                        allBranchAttributesConfigurations
                    ).ConfigureAwait(false);      
                    
                    // Get expanded child document
                    var childRepo = ProjectionRepositoryFactory.GetProjectionRepository(childProjectionDocumentSchema);
                    var child = await childRepo.Single(new Guid(childDocument["AggregateId"] as string ?? string.Empty), childDocument["PartitionKey"] as string ?? string.Empty, CancellationToken.None).ConfigureAwait(false);
                    
                    // Get current child parental attributes
                    Dictionary<string, object> parentalAttributes = child?["ParentalAttributes"] as Dictionary<string, object> ?? new Dictionary<string, object>();
                    
                    // Add to it all attributes from the new branch levels (e.g. if there is a new category in the the middle of the path)
                    if (idsToAdd != null)
                    {
                        // for each new level
                        foreach (var id in idsToAdd)
                        {
                            // get all attributes and configurations from the new level
                            (List<AttributeConfiguration> parentAttributesConfigurations, ReadOnlyCollection<AttributeInstance> parentAttributes) = await ExtractAttributesAndAttributeConfigurationsAsync(id).ConfigureAwait(false);
                            
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
                            (List<AttributeConfiguration> parentAttributesConfigurations, ReadOnlyCollection<AttributeInstance> parentAttributes) = await ExtractAttributesAndAttributeConfigurationsAsync(id).ConfigureAwait(false);
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
                        var newAttributeConfigs = await BuildAttributesListFromAttributesReferences(newAttributesReferences).ConfigureAwait(false);
                        allBranchAttributesConfigurations.AddRange(newAttributeConfigs);
                        foreach (var attribute in newAttributes)
                        {
                            allBranchAttributes[attribute.ConfigurationAttributeMachineName] = attribute.GetValue();
                        }
                    }
                    
                    // Build a new schema for the child considering updated list of branch attributes and configs
                    childProjectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
                        new Guid(childDocument["ConfigurationId"] as string ?? string.Empty),
                        allBranchAttributesConfigurations
                    ).ConfigureAwait(false);

                    await UpdateDocument(childProjectionDocumentSchema,
                        new Guid(child["Id"] as string) ,
                        child["PartitionKey"] as string,
                        updatedAt: DateTime.Now,
                        dict =>
                        {
                            dict["ParentalAttributes"] = parentalAttributes;
                            dict["CategoryPath"] = string.IsNullOrEmpty(newCategoryPath) ? childrenCategoryPath : newCategoryPath + $"/{instanceId}";
                        }).ConfigureAwait(false);
                }
            }
        }

        // Move category with all children to a new category
        public async Task On(CategoryPathChanged @event)
        {
            (List<AttributeConfiguration> parentAttributesConfigurations, _) = await ExtractAttributesAndAttributeConfigurationsAsync(@event.AggregateId.ToString()).ConfigureAwait(false);   
            var categorySchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
                @event.entityConfigurationId,
                parentAttributesConfigurations
            ).ConfigureAwait(false);
            await UpdateDocument(categorySchema,
                @event.AggregateId ,
                @event.PartitionKey,
                updatedAt: @event.Timestamp,
                dict =>
                {
                    dict["CategoryPath"] = @event.newCategoryPath;
                    // Update subcategories
                    return UpdateChildrenAsync(@event.AggregateId,
                        categorySchema,
                        @event.currentCategoryPath,
                        null,
                        null,
                        @event.newCategoryPath);
                }).ConfigureAwait(false);        
        }
        
        //TODO: Manage attribute changes
    }
}