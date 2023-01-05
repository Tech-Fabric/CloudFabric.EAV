using System.Collections.ObjectModel;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.Projections;

namespace CloudFabric.EAV.Domain.LocalEventSourcingPackages.Projections
{
    public class InstanceProjectionBuilder: ProjectionBuilder
    {
        protected AggregateRepositoryFactory _aggregateRepositoryFactory;

        protected InstanceProjectionBuilder(ProjectionRepositoryFactory projectionRepositoryFactory, AggregateRepositoryFactory aggregateRepositoryFactory) : base(projectionRepositoryFactory)
        {
            _aggregateRepositoryFactory = aggregateRepositoryFactory;
        }
        
        protected async Task<(List<AttributeConfiguration>, Dictionary<string, object>)> BuildBranchAttributes(string categoryPath)
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
                        var parentEntityInstance = await repo.LoadAsync(Guid.Parse(parentIdString), parentIdString);
                        if (parentEntityInstance != null)
                        {
                            (List<AttributeConfiguration> parentAttributesConfigurations, ReadOnlyCollection<AttributeInstance> parentAttributes) = await ExtractAttributesAndAttributeConfigurations(parentIdString);
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

        protected async Task<(List<AttributeConfiguration> attributesConfigurations, ReadOnlyCollection<AttributeInstance> Attributes)> ExtractAttributesAndAttributeConfigurations(string id)
        {
            var repo = _aggregateRepositoryFactory
                .GetAggregateRepository<EntityInstance>();
            var parentEntityInstance = await repo.LoadAsync(Guid.Parse(id), id);
            List<AttributeConfiguration> attributesConfigurations = null;

            if (parentEntityInstance != null)
            {
                var entityConfiguration = await _aggregateRepositoryFactory
                    .GetAggregateRepository<EntityConfiguration>()
                    .LoadAsyncOrThrowNotFound(parentEntityInstance.EntityConfigurationId, parentEntityInstance.EntityConfigurationId.ToString());
                attributesConfigurations = await BuildAttributesListFromAttributesReferences(entityConfiguration.Attributes.ToList());
            }
            return (attributesConfigurations, parentEntityInstance.Attributes);
        }
        
        protected async Task<ProjectionDocumentSchema> BuildProjectionDocumentSchemaForEntityConfigurationId(
            Guid entityConfigurationId,
            List<AttributeConfiguration>? parentAttributeConfigurations)
        {
            var entityConfiguration = await _aggregateRepositoryFactory
                .GetAggregateRepository<EntityConfiguration>()
                .LoadAsyncOrThrowNotFound(entityConfigurationId, entityConfigurationId.ToString());

            List<AttributeConfiguration> attributes = await BuildAttributesListFromAttributesReferences(entityConfiguration.Attributes.ToList());

            return ProjectionDocumentSchemaFactory.FromEntityConfiguration(entityConfiguration, attributes, parentAttributeConfigurations);
        }

        protected async Task<List<AttributeConfiguration>> BuildAttributesListFromAttributesReferences(List<EntityConfigurationAttributeReference> references)
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

        protected async Task CreateInstanceProjection(Guid aggregateId,
            Guid entityConfigurationId,
            Guid? tenantId,
            string categoryPath,
            ReadOnlyCollection<AttributeInstance> attributes,
            string partitionKey,
            DateTime timestamp
            )
        {
            (List<AttributeConfiguration> allParentalAttributesConfigurations, Dictionary<string, object> allParentalAttributes) = await BuildBranchAttributes(categoryPath);

            // Build schema for entity instance considering all parent attributes and their configurations
            var projectionDocumentSchema = await BuildProjectionDocumentSchemaForEntityConfigurationId(
                entityConfigurationId,
                allParentalAttributesConfigurations
            );

            var document = new Dictionary<string, object?>()
            {
                {
                    "Id", aggregateId
                },
                {
                    "EntityConfigurationId", entityConfigurationId
                },
                {
                    "TenantId", tenantId
                },
                {
                    "CategoryPath", categoryPath
                },
                {
                    "ParentalAttributes", allParentalAttributes
                },
                {
                    "Attributes", new Dictionary<string, object?>()
                }
            };

            // fill attributes
            foreach (var attribute in attributes)
            {
                document.Add(attribute.ConfigurationAttributeMachineName, attribute.GetValue());
            }

            // Add document
            await UpsertDocument(
                projectionDocumentSchema,
                document,
                partitionKey,
                timestamp
            );
        }
    }
}