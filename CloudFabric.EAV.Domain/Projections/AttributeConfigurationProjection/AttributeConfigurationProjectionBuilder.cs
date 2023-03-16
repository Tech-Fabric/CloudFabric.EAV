using CloudFabric.EAV.Domain.Events.Configuration.Attribute;
using CloudFabric.EAV.Domain.Events.Configuration.Entity;
using CloudFabric.EAV.Domain.Events.Instance.Attribute;
using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

namespace CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;

public class AttributeConfigurationProjectionBuilder : ProjectionBuilder<AttributeConfigurationProjectionDocument>,
    IHandleEvent<AttributeConfigurationCreated>,
    IHandleEvent<AttributeConfigurationNameUpdated>,
    IHandleEvent<AttributeConfigurationDescriptionUpdated>,
    IHandleEvent<AttributeConfigurationIsRequiredFlagUpdated>,
    IHandleEvent<AttributeConfigurationUpdated>,
    IHandleEvent<AttributeConfigurationDeleted>,
    IHandleEvent<EntityInstanceCreated>,
    IHandleEvent<AttributeInstanceAdded>,
    IHandleEvent<AttributeInstanceRemoved>,
    IHandleEvent<AggregateUpdatedEvent<AttributeConfiguration>>,
    IHandleEvent<EntityConfigurationCreated>,
    IHandleEvent<EntityConfigurationAttributeAdded>,
    IHandleEvent<EntityConfigurationAttributeRemoved>
{
    public AttributeConfigurationProjectionBuilder(
        ProjectionRepositoryFactory projectionRepositoryFactory
    ) : base(projectionRepositoryFactory)
    {
    }

    public async Task On(AggregateUpdatedEvent<AttributeConfiguration> @event)
    {
        await SetDocumentUpdatedAt(@event.AggregateId, @event.PartitionKey, @event.UpdatedAt);
    }

    public async Task On(AttributeConfigurationCreated @event)
    {
        await UpsertDocument(
            new AttributeConfigurationProjectionDocument
            {
                Id = @event.AggregateId,
                IsRequired = @event.IsRequired,
                Name = @event.Name.Select(x =>
                    new SearchableLocalizedString { String = x.String, CultureInfoId = x.CultureInfoId }
                ).ToList(),
                MachineName = @event.MachineName,
                PartitionKey = @event.PartitionKey,
                TenantId = @event.TenantId,
                Description = @event.Description,
                UpdatedAt = @event.Timestamp,
                ValueType = @event.ValueType,
                Metadata = @event.Metadata
            },
            @event.PartitionKey,
            @event.Timestamp
        );
    }

    public async Task On(AttributeConfigurationDeleted @event)
    {
        await DeleteDocument(@event.AggregateId, @event.PartitionKey);
    }

    public async Task On(AttributeConfigurationDescriptionUpdated @event)
    {
        await UpdateDocument(@event.AggregateId,
            @event.PartitionKey,
            @event.Timestamp,
            document =>
            {
                LocalizedString? description =
                    document.Description.FirstOrDefault(n => n.CultureInfoId == @event.CultureInfoId);

                if (description == null)
                {
                    document.Description.Add(
                        new LocalizedString { CultureInfoId = @event.CultureInfoId, String = @event.NewDescription }
                    );
                }
                else
                {
                    description.String = @event.NewDescription;
                }
            }
        );
    }

    public async Task On(AttributeConfigurationIsRequiredFlagUpdated @event)
    {
        await UpdateDocument(@event.AggregateId,
            @event.PartitionKey,
            @event.Timestamp,
            document =>
            {
                document.IsRequired = @event.NewIsRequired;
            }
        );
    }

    public async Task On(AttributeConfigurationNameUpdated @event)
    {
        await UpdateDocument(@event.AggregateId,
            @event.PartitionKey,
            @event.Timestamp,
            document =>
            {
                SearchableLocalizedString? name =
                    document.Name.FirstOrDefault(n => n.CultureInfoId == @event.CultureInfoId);

                if (name == null)
                {
                    document.Name.Add(
                        new SearchableLocalizedString { CultureInfoId = @event.CultureInfoId, String = @event.NewName }
                    );
                }
                else
                {
                    name.String = @event.NewName;
                }
            }
        );
    }

    public async Task On(AttributeConfigurationUpdated @event)
    {
        await UpdateDocument(@event.AggregateId,
            @event.PartitionKey,
            @event.Timestamp,
            document =>
            {
                document.Name = @event.Name.ConvertAll(x =>
                    new SearchableLocalizedString { CultureInfoId = x.CultureInfoId, String = x.String }
                );

                document.Description = @event.Description;
                document.IsRequired = @event.IsRequired;
                document.TenantId = @event.TenantId;
                document.Metadata = @event.Metadata;
            }
        );
    }

    public async Task On(AttributeInstanceAdded @event)
    {
        AttributeConfigurationProjectionDocument? attributeConfig = (await ProjectionRepositoryFactory
                .GetProjectionRepository<AttributeConfigurationProjectionDocument>().Query(
                    new ProjectionQuery
                    {
                        Filters = new List<Filter>
                        {
                            new()
                            {
                                PropertyName =
                                    nameof(AttributeConfigurationProjectionDocument.MachineName),
                                Operator = FilterOperator.Equal,
                                Value = @event.AttributeInstance.ConfigurationAttributeMachineName
                            }
                        },
                        Limit = 1
                    }
                ))
            .Records
            .FirstOrDefault()
            ?.Document;

        if (attributeConfig != null)
        {
            await UpdateDocument(
                attributeConfig.Id!.Value,
                attributeConfig.PartitionKey!,
                @event.Timestamp,
                document =>
                {
                    document.NumberOfEntityInstancesWithAttribute++;
                }
            );
        }
    }

    public async Task On(AttributeInstanceRemoved @event)
    {
        AttributeConfigurationProjectionDocument? attributeConfig = (await ProjectionRepositoryFactory
                .GetProjectionRepository<AttributeConfigurationProjectionDocument>().Query(
                    new ProjectionQuery
                    {
                        Filters = new List<Filter>
                        {
                            new()
                            {
                                PropertyName =
                                    nameof(AttributeConfigurationProjectionDocument.MachineName),
                                Operator = FilterOperator.Equal,
                                Value = @event.AttributeMachineName
                            }
                        },
                        Limit = 1
                    }
                ))
            .Records
            .FirstOrDefault()
            ?.Document;

        if (attributeConfig != null)
        {
            await UpdateDocument(
                attributeConfig.Id!.Value,
                attributeConfig.PartitionKey!,
                @event.Timestamp,
                document =>
                {
                    document.NumberOfEntityInstancesWithAttribute--;
                }
            );
        }
    }

    public async Task On(EntityInstanceCreated @event)
    {
        IEnumerable<string> attributesMachineNames =
            @event.Attributes.Select(x => x.ConfigurationAttributeMachineName);

        foreach (var machineName in attributesMachineNames)
        {
            AttributeConfigurationProjectionDocument? attributeConfig = (await ProjectionRepositoryFactory
                    .GetProjectionRepository<AttributeConfigurationProjectionDocument>().Query(
                        new ProjectionQuery
                        {
                            Filters = new List<Filter>
                            {
                                new()
                                {
                                    PropertyName =
                                        nameof(AttributeConfigurationProjectionDocument.MachineName),
                                    Operator = FilterOperator.Equal,
                                    Value = machineName
                                }
                            },
                            Limit = 1
                        }
                    ))
                .Records
                .FirstOrDefault()
                ?.Document;

            if (attributeConfig != null)
            {
                await UpdateDocument(
                    attributeConfig.Id!.Value,
                    attributeConfig.PartitionKey!,
                    @event.Timestamp,
                    document =>
                    {
                        document.NumberOfEntityInstancesWithAttribute++;
                    }
                );
            }
        }
    }

    public async Task On(EntityConfigurationCreated @event)
    {
        foreach (var attribute in @event.Attributes)
        {
            await UpdateDocument(attribute.AttributeConfigurationId,
            attribute.AttributeConfigurationId.ToString(),
            @event.Timestamp,
            document =>
                {
                    document.UsedByEntityConfigurationIds.Add(@event.AggregateId.ToString());
                }
            );
        }
    }
    public async Task On(EntityConfigurationAttributeAdded @event)
    {
        await UpdateDocument(@event.AttributeReference.AttributeConfigurationId,
            @event.AttributeReference.AttributeConfigurationId.ToString(),
            @event.Timestamp,
            document =>
            {
                document.UsedByEntityConfigurationIds.Add(@event.AggregateId.ToString());
            }
        );
    }

    public async Task On(EntityConfigurationAttributeRemoved @event)
    {
        await UpdateDocument(@event.AttributeConfigurationId,
            @event.AttributeConfigurationId.ToString(),
            @event.Timestamp,
            document =>
            {
                document.UsedByEntityConfigurationIds.Remove(@event.AggregateId.ToString());
            }
        );
    }
}
