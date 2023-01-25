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

        protected async Task<ProjectionDocumentSchema> BuildProjectionDocumentSchemaForEntityConfigurationId(
            Guid entityConfigurationId,
            List<AttributeConfiguration>? parentAttributeConfigurations)
        {
            var entityConfiguration = await _aggregateRepositoryFactory
                .GetAggregateRepository<EntityConfiguration>()
                .LoadAsyncOrThrowNotFound(entityConfigurationId, entityConfigurationId.ToString()).ConfigureAwait(false);

            List<AttributeConfiguration> attributes = await BuildAttributesListFromAttributesReferences(entityConfiguration.Attributes.ToList()).ConfigureAwait(false);

            return ProjectionDocumentSchemaFactory.FromEntityConfiguration(entityConfiguration, attributes, parentAttributeConfigurations);
        }

        protected async Task<ProjectionDocumentSchema> BuildEmptyProjectionDocumentSchemaForEntityConfigurationId(
            Guid entityConfigurationId)
        {
            var entityConfiguration = await _aggregateRepositoryFactory
                .GetAggregateRepository<EntityConfiguration>()
                .LoadAsyncOrThrowNotFound(entityConfigurationId, entityConfigurationId.ToString());
            
            return ProjectionDocumentSchemaFactory.FromEntityConfiguration(entityConfiguration, new List<AttributeConfiguration>(), null);
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
           
        }
    }
}