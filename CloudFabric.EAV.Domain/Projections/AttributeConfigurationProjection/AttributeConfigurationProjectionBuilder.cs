using CloudFabric.EAV.Domain.Events.Configuration.Attribute;
using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

namespace CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;

public class AttributeConfigurationProjectionBuilder : ProjectionBuilder<AttributeConfigurationProjectionDocument>,
    IHandleEvent<AttributeConfigurationCreated>,
    IHandleEvent<AttributeConfigurationNameUpdated>,
    IHandleEvent<AttributeConfigurationDescriptionUpdated>,
    IHandleEvent<AttributeConfigurationIsRequiredFlagUpdated>,
    IHandleEvent<EntityInstanceCreated>,
    IHandleEvent<AttributeInstanceAdded>,
    IHandleEvent<AttributeInstanceRemoved>
{
    public AttributeConfigurationProjectionBuilder(
        ProjectionRepositoryFactory repositoryFactory
    ) : base(repositoryFactory)
    {
    }

    public async Task On(AttributeConfigurationCreated @event)
    {
        await UpsertDocument(
            new AttributeConfigurationProjectionDocument()
            {
                Id = @event.Id,
                IsRequired = @event.IsRequired,
                Name = @event.Name.Select(x =>
                    new SearchableLocalizedString
                    {
                        String = x.String,
                        CultureInfoId = x.CultureInfoId
                    }
                ).ToList(),
                MachineName = @event.MachineName,
                PartitionKey = @event.PartitionKey,
                TenantId = @event.TenantId,
                Description = @event.Description,
                UpdatedAt = @event.Timestamp,
                AttributeType = @event.ValueType
            },
            @event.PartitionKey
        );
    }

    public async Task On(AttributeConfigurationNameUpdated @event)
    {
        await UpdateDocument(@event.Id,
            @event.PartitionKey,
            (document) =>
            {
                SearchableLocalizedString? name = document.Name.FirstOrDefault(n => n.CultureInfoId == @event.CultureInfoId);

                if (name == null)
                {
                    document.Name.Add(
                        new SearchableLocalizedString
                        {
                            CultureInfoId = @event.CultureInfoId,
                            String = @event.NewName
                        }
                    );
                }
                else
                {
                    name.String = @event.NewName;
                }
            }
        );
    }

    public async Task On(AttributeConfigurationDescriptionUpdated @event)
    {
        await UpdateDocument(@event.Id,
            @event.PartitionKey,
            (document) =>
            {
                LocalizedString? description = document.Description.FirstOrDefault(n => n.CultureInfoId == @event.CultureInfoId);

                if (description == null)
                {
                    document.Description.Add(
                        new LocalizedString
                        {
                            CultureInfoId = @event.CultureInfoId,
                            String = @event.NewDescription
                        }
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
        await UpdateDocument(@event.Id,
            @event.PartitionKey,
            (document) =>
            {
                document.IsRequired = @event.NewIsRequired;
            }
        );
    }

    public async Task On(EntityInstanceCreated @event)
    {
        var attributesMachineNames = @event.Attributes.Select(x => x.ConfigurationAttributeMachineName);

        foreach (var machineName in attributesMachineNames)
        {
            var attributeConfig = (await ProjectionRepositoryFactory.GetProjectionRepository<AttributeConfigurationProjectionDocument>().Query(
                new ProjectionQuery
                {
                    Filters = new List<Filter>
                    {
                        new()
                        {
                            PropertyName = nameof(AttributeConfigurationProjectionDocument.MachineName),
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
                    (document) =>
                    {
                        document.NumberOfEntityInstancesWithAttribute++;
                    }
                );
            }
        }
    }

    public async Task On(AttributeInstanceAdded @event)
    {
        var attributeConfig = (await ProjectionRepositoryFactory.GetProjectionRepository<AttributeConfigurationProjectionDocument>().Query(
            new ProjectionQuery
            {
                Filters = new List<Filter>
                {
                    new()
                    {
                        PropertyName = nameof(AttributeConfigurationProjectionDocument.MachineName),
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
                (document) =>
                {
                    document.NumberOfEntityInstancesWithAttribute++;
                }
            );
        }
    }

    public async Task On(AttributeInstanceRemoved @event)
    {
        var attributeConfig = (await ProjectionRepositoryFactory.GetProjectionRepository<AttributeConfigurationProjectionDocument>().Query(
            new ProjectionQuery
            {
                Filters = new List<Filter>
                {
                    new()
                    {
                        PropertyName = nameof(AttributeConfigurationProjectionDocument.MachineName),
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
                (document) =>
                {
                    document.NumberOfEntityInstancesWithAttribute--;
                }
            );
        }
    }
}