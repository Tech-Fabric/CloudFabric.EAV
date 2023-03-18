using System;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

using AutoMapper;

using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.Attributes;
using CloudFabric.EAV.Service;
using CloudFabric.EAV.Tests.Factories;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Persistence;
using CloudFabric.EventSourcing.EventStore.Postgresql;
using CloudFabric.Projections;
using CloudFabric.Projections.InMemory;
using CloudFabric.Projections.Postgresql;
using CloudFabric.Projections.Queries;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace CloudFabric.EAV.Tests;

[TestClass]
public class Tests
{
    private AggregateRepositoryFactory _aggregateRepositoryFactory;

    private EAVService _eavService;

    private IEventStore _eventStore;
    private ILogger<EAVService> _logger;
    private PostgresqlProjectionRepositoryFactory _projectionRepositoryFactory;
    private IMapper _mapper;

    [TestInitialize]
    public async Task SetUp()
    {
        var loggerFactory = new LoggerFactory();
        _logger = loggerFactory.CreateLogger<EAVService>();

        var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(Assembly.GetAssembly(typeof(EAVService)));
            }
        );
        _mapper = configuration.CreateMapper();

        var connectionString = "Host=localhost;"
                               + "Username=cloudfabric_eventsourcing_test;"
                               + "Password=cloudfabric_eventsourcing_test;"
                               + "Database=cloudfabric_eventsourcing_test;"
                               + "Maximum Pool Size=1000";

        _eventStore = new PostgresqlEventStore(
            connectionString,
            "eav_tests_event_store"
        );

        await _eventStore.Initialize();

        _aggregateRepositoryFactory = new AggregateRepositoryFactory(_eventStore);
        _projectionRepositoryFactory = new PostgresqlProjectionRepositoryFactory(connectionString);

        // Projections engine - takes events from events observer and passes them to multiple projection builders
        var projectionsEngine = new ProjectionsEngine(
            _projectionRepositoryFactory.GetProjectionRepository<ProjectionRebuildState>()
        );
        projectionsEngine.SetEventsObserver(GetEventStoreEventsObserver());

        var attributeConfigurationProjectionBuilder = new AttributeConfigurationProjectionBuilder(
            _projectionRepositoryFactory
        );
        var ordersListProjectionBuilder = new EntityConfigurationProjectionBuilder(_projectionRepositoryFactory);

        projectionsEngine.AddProjectionBuilder(attributeConfigurationProjectionBuilder);
        projectionsEngine.AddProjectionBuilder(ordersListProjectionBuilder);


        await projectionsEngine.StartAsync("TestInstance");


        _eavService = new EAVService(
            _logger,
            _mapper,
            new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            },
            _aggregateRepositoryFactory,
            _projectionRepositoryFactory,
            new EventUserInfo(Guid.NewGuid())
        );
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _eventStore.DeleteAll();

        try
        {
            IProjectionRepository<EntityConfigurationProjectionDocument> entityConfigurationProjectionRepository =
                _projectionRepositoryFactory
                    .GetProjectionRepository<EntityConfigurationProjectionDocument>();

            IProjectionRepository<AttributeConfigurationProjectionDocument>
                attributeConfigurationProjectionRepository = _projectionRepositoryFactory
                    .GetProjectionRepository<AttributeConfigurationProjectionDocument>();

            await entityConfigurationProjectionRepository.DeleteAll();
            await attributeConfigurationProjectionRepository.DeleteAll();

            IProjectionRepository<ProjectionRebuildState> rebuildStateRepository =
                GetProjectionRebuildStateRepository();
            await rebuildStateRepository.DeleteAll();
        }
        catch
        {
        }
    }


    [TestMethod]
    public async Task CreateInstance_Success()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityConfigurationViewModel configuration =
            await _eavService.GetEntityConfiguration(createdConfiguration.Id);

        EntityInstanceCreateRequest entityInstanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        (EntityInstanceViewModel createdInstance, ProblemDetails validationErrors) =
            await _eavService.CreateEntityInstance(entityInstanceCreateRequest);

        validationErrors.Should().BeNull();
        createdInstance.Id.Should().NotBeEmpty();
        createdInstance.EntityConfigurationId.Should().Be(configuration.Id);
    }

    [TestMethod]
    public async Task CreateInstance_InvalidConfigurationId()
    {
        EntityInstanceCreateRequest entityInstanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(Guid.NewGuid());
        (EntityInstanceViewModel result, ProblemDetails validationErrors) =
            await _eavService.CreateEntityInstance(entityInstanceCreateRequest);
        result.Should().BeNull();
        validationErrors.Should().BeOfType<ValidationErrorResponse>();
        validationErrors.As<ValidationErrorResponse>().Errors.Should().ContainKey("EntityConfigurationId");
        validationErrors.As<ValidationErrorResponse>().Errors["EntityConfigurationId"].First().Should()
            .Be("Configuration not found");
    }

    [TestMethod]
    public async Task CreateInstance_MissingRequiredAttribute()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityConfigurationViewModel configuration =
            await _eavService.GetEntityConfiguration(createdConfiguration.Id);
        var requiredAttributeMachineName = "players_min";
        EntityInstanceCreateRequest entityInstanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);
        entityInstanceCreateRequest.Attributes = entityInstanceCreateRequest.Attributes
            .Where(a => a.ConfigurationAttributeMachineName != requiredAttributeMachineName).ToList();
        (EntityInstanceViewModel createdInstance, ProblemDetails validationErrors) =
            await _eavService.CreateEntityInstance(entityInstanceCreateRequest);
        createdInstance.Should().BeNull();

        validationErrors.As<ValidationErrorResponse>().Errors.Should().ContainKey(requiredAttributeMachineName);
        validationErrors.As<ValidationErrorResponse>().Errors[requiredAttributeMachineName].First().Should()
            .Be("Attribute is Required");
    }

    [TestMethod]
    public async Task CreateInstance_MissingRequiredAttributeValue()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        var requiredAttributeMachineName = "test_date";

        configurationCreateRequest.Attributes.Add(new DateRangeAttributeConfigurationUpdateRequest()
        {
            DateRangeAttributeType = DateRangeAttributeType.SingleDate,
            IsRequired = true,
            MachineName = requiredAttributeMachineName,
            Name = new List<LocalizedStringCreateRequest>()
            {
                new LocalizedStringCreateRequest()
                {
                    CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                    String = "Test Date"
                }
            }
        });
        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityConfigurationViewModel configuration =
            await _eavService.GetEntityConfiguration(createdConfiguration.Id);
        EntityInstanceCreateRequest entityInstanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);
        entityInstanceCreateRequest.Attributes.Add(
            new DateRangeAttributeInstanceCreateUpdateRequest()
            {
                ConfigurationAttributeMachineName = requiredAttributeMachineName,
                Value = null
            });

        (EntityInstanceViewModel createdInstance, ProblemDetails validationErrors) =
            await _eavService.CreateEntityInstance(entityInstanceCreateRequest);
        createdInstance.Should().BeNull();
    }

    [TestMethod]
    public async Task CreateEntityConfiguration_Success()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfiguration, ProblemDetails? errors) =
            await _eavService.CreateEntityConfiguration(
                configurationCreateRequest,
                CancellationToken.None
            );
        createdConfiguration.Should().NotBeNull();
        createdConfiguration.Id.Should().NotBeEmpty();
        foreach (LocalizedStringViewModel name in createdConfiguration.Name)
        {
            name.String.Should().Be(configurationCreateRequest.Name
                .First(n => n.CultureInfoId == name.CultureInfoId)
                .String
            );
        }

        createdConfiguration.MachineName.Should().Be(configurationCreateRequest.MachineName);
        createdConfiguration.Attributes.Count.Should().Be(configurationCreateRequest.Attributes.Count);

        var configurationWithAttributes =
            await _eavService.GetEntityConfigurationWithAttributes(createdConfiguration.Id);

        configurationWithAttributes.Should().BeEquivalentTo(configurationCreateRequest);
    }

    [TestMethod]
    public async Task CreateEntityConfiguration_ValidationError()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        configurationCreateRequest.Name = new List<LocalizedStringCreateRequest>();
        (EntityConfigurationViewModel? createdConfiguration, ProblemDetails? errors) =
            await _eavService.CreateEntityConfiguration(
                configurationCreateRequest,
                CancellationToken.None
            );
        createdConfiguration.Should().BeNull();
        errors.Should().NotBeNull();
        errors.As<ValidationErrorResponse>().Errors.Should().ContainKey(configurationCreateRequest.MachineName);
        errors.As<ValidationErrorResponse>().Errors[configurationCreateRequest.MachineName].Should()
            .Contain("Name cannot be empty");
    }

    [TestMethod]
    public async Task CreateEntityConfiguration_AttributesMachineNamesAreNotUnique()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        configurationCreateRequest.Attributes.Add(
            new TextAttributeConfigurationCreateUpdateRequest
            {
                Name = new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID, String = "test" }
                },
                MachineName =
                    (configurationCreateRequest.Attributes[0] as AttributeConfigurationCreateUpdateRequest)!
                    .MachineName,
                DefaultValue = "test"
            }
        );

        (EntityConfigurationViewModel? entityConfig, ProblemDetails? error) =
            await _eavService.CreateEntityConfiguration(
                configurationCreateRequest,
                CancellationToken.None
            );

        entityConfig.Should().BeNull();
        error.Should().NotBeNull();
        error.Should().BeOfType<ValidationErrorResponse>();
        error.As<ValidationErrorResponse>().Errors.Should()
            .Contain(x => x.Value.Contains("Attributes machine name must be unique"));
    }

    [TestMethod]
    public async Task GetEntityConfiguration_Success()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityConfigurationViewModel configuration = await _eavService.GetEntityConfiguration(
            createdConfiguration.Id
        );

        configuration.Should().BeEquivalentTo(createdConfiguration);
    }

    [TestMethod]
    public async Task UpdateAttribute_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var numberAttribute = new NumberAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "number_attribute",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "Number attribute description" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "New Number Attribute" }
            },
            DefaultValue = 15,
            IsRequired = true,
            MaximumValue = 100,
            MinimumValue = -100
        };

        var configCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { numberAttribute }
        };

        (EntityConfigurationViewModel? created, _) =
            await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);
        created.Attributes.Count.Should().Be(1);

        // update added attribute
        numberAttribute.Name[0].String = "Another number name";
        numberAttribute.IsRequired = false;
        numberAttribute.MinimumValue = 0;
        numberAttribute.MaximumValue = 50;

        (AttributeConfigurationViewModel? _, ProblemDetails? error) = await _eavService.UpdateAttribute(
            created.Attributes[0].AttributeConfigurationId,
            numberAttribute,
            CancellationToken.None
        );

        error.Should().BeNull();

        AttributeConfigurationViewModel updatedAttribute = await _eavService.GetAttribute(
            created.Attributes[0].AttributeConfigurationId,
            CancellationToken.None
        );

        updatedAttribute.Name[0].String.Should().Be(numberAttribute.Name[0].String);
        updatedAttribute.IsRequired.Should().Be(numberAttribute.IsRequired);
        updatedAttribute.As<NumberAttributeConfigurationViewModel>().MaximumValue.Should()
            .Be(numberAttribute.MaximumValue);
        updatedAttribute.As<NumberAttributeConfigurationViewModel>().MinimumValue.Should()
            .Be(numberAttribute.MinimumValue);
    }

    [TestMethod]
    public async Task UpdateAttribute_ValidationError()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var numberAttribute = new NumberAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "number_attribute",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "Number attribute description" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "New Number Attribute" }
            },
            DefaultValue = 15,
            IsRequired = true,
            MaximumValue = 100,
            MinimumValue = -100
        };

        var configCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { numberAttribute }
        };

        (EntityConfigurationViewModel? created, _) =
            await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);
        created.Attributes.Count.Should().Be(1);

        // update added attribute
        numberAttribute.Name = new List<LocalizedStringCreateRequest>();

        (AttributeConfigurationViewModel? updatedResult, ProblemDetails? errors) =
            await _eavService.UpdateAttribute(
                created.Attributes[0].AttributeConfigurationId,
                numberAttribute,
                CancellationToken.None
            );

        updatedResult.Should().BeNull();
        errors.Should().NotBeNull();
        errors.As<ValidationErrorResponse>().Errors.Should().ContainKey(numberAttribute.MachineName);
        errors.As<ValidationErrorResponse>().Errors[numberAttribute.MachineName].Should()
            .Contain("Name cannot be empty");
    }

    [TestMethod]
    public async Task DeleteAttribute_Success()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel entityConfig, ProblemDetails? _) =
            await _eavService.CreateEntityConfiguration(configurationCreateRequest, CancellationToken.None);

        Guid attributeToDelete = entityConfig.Attributes.Select(x => x.AttributeConfigurationId).FirstOrDefault();

        await _eavService.DeleteAttributes(new List<Guid> { attributeToDelete }, CancellationToken.None);

        EntityConfigurationViewModel entityConfAfterAttributeDeleted =
            await _eavService.GetEntityConfiguration(entityConfig.Id);
        entityConfAfterAttributeDeleted.Attributes.Count().Should().Be(entityConfig.Attributes.Count() - 1);

        Func<Task> act = async () => await _eavService.GetAttribute(attributeToDelete);
        await act.Should().ThrowAsync<NotFoundException>();

        ProjectionQueryResult<AttributeConfigurationListItemViewModel> attributesProjections =
            await _eavService.ListAttributes(new ProjectionQuery
                {
                    Filters = new List<Filter>
                    {
                        new()
                        {
                            PropertyName = nameof(AttributeConfigurationProjectionDocument.Id),
                            Operator = FilterOperator.Equal,
                            Value = attributeToDelete
                        }
                    }
                }
            );
        attributesProjections.Records.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task DeleteEntityAttributeFromEntity_EntityNotFound()
    {
        Func<Task> act = async () =>
            await _eavService.DeleteAttributesFromEntityConfiguration(new List<Guid> { Guid.NewGuid() },
                Guid.NewGuid(),
                CancellationToken.None
            );
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task DeleteEntityAttributeFromEntity_DeleteNotExistingAttribute()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel entityConfig, ProblemDetails? _) =
            await _eavService.CreateEntityConfiguration(configurationCreateRequest, CancellationToken.None);

        await _eavService.DeleteAttributesFromEntityConfiguration(new List<Guid> { Guid.NewGuid() },
            entityConfig.Id,
            CancellationToken.None
        );
        EntityConfigurationViewModel entityConfigAfterDeletingNotExistingAttribute = await _eavService.GetEntityConfiguration(entityConfig.Id);
        entityConfigAfterDeletingNotExistingAttribute.Attributes.Count.Should().Be(entityConfig.Attributes.Count);
    }

    [TestMethod]
    public async Task GetAttributeByUsedEntities_Success()
    {
        var projectionRepository = _projectionRepositoryFactory.GetProjectionRepository<AttributeConfigurationProjectionDocument>();

        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;

        // Create attributes
        var numberAttributeRequest = new NumberAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "number_attribute",
            Name = new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "Number Attribute" }
                },
            DefaultValue = 15,
            IsRequired = false,
            MaximumValue = 100,
            MinimumValue = -100
        };
        (AttributeConfigurationViewModel numberAttribute, _) = await _eavService.CreateAttribute(numberAttributeRequest);

        var textAttributeRequest = new TextAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "text_attribute",
            Name = new List<LocalizedStringCreateRequest>
                    {
                        new() { CultureInfoId = cultureInfoId, String = "Text Attribute" }
                    },
            IsRequired = true,
            IsSearchable = true,
            MaxLength = 100,
            DefaultValue = "-"
        };
        (AttributeConfigurationViewModel textAttribute, _) = await _eavService.CreateAttribute(textAttributeRequest);

        // Create entity and add attributes
        var configurationCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            }
        };
        (EntityConfigurationViewModel? createdFirstEntity, _) =
            await _eavService.CreateEntityConfiguration(configurationCreateRequest, CancellationToken.None);

        await _eavService.AddAttributeToEntityConfiguration(numberAttribute.Id, createdFirstEntity.Id);
        await _eavService.AddAttributeToEntityConfiguration(textAttribute.Id, createdFirstEntity.Id);

        // Get attributes by UsedByEntityConfigurationIds
        ProjectionQuery query = new ProjectionQuery()
        {
            Filters = new()
            {
                new Filter
                {
                    PropertyName = nameof(AttributeConfigurationProjectionDocument.UsedByEntityConfigurationIds).ToLowerInvariant(),
                    Operator = FilterOperator.ArrayContains,
                    Value = createdFirstEntity.Id.ToString()
                }
            }
        };

        //var result = await _eavService.ListAttributes(query);
        //result.TotalRecordsFound.Should().Be(2);
        var result = await projectionRepository.Query(query);
        result.TotalRecordsFound.Should().Be(2);
        result.Records.First().Document.UsedByEntityConfigurationIds.FirstOrDefault().Should().Be(createdFirstEntity.Id.ToString());

        // Create another entity with attribute
        configurationCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "another_test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            }
        };
        (EntityConfigurationViewModel? createdSecondEntity, _) =
            await _eavService.CreateEntityConfiguration(configurationCreateRequest, CancellationToken.None);
        await _eavService.AddAttributeToEntityConfiguration(numberAttribute.Id, createdSecondEntity.Id);

        // Get attribute by one of UsedByEntityConfigurationIds
        query.Filters = new()
        {
            new Filter
            {
                PropertyName = nameof(AttributeConfigurationProjectionDocument.UsedByEntityConfigurationIds).ToLowerInvariant(),
                Operator = FilterOperator.ArrayContains,
                Value = createdSecondEntity.Id.ToString()
            }
        };

        result = await projectionRepository.Query(query);
        result.TotalRecordsFound.Should().Be(1);
        result.Records.FirstOrDefault().Document.UsedByEntityConfigurationIds.Count.Should().Be(2);

        // Get after delete
        await _eavService.DeleteAttributes(new List<Guid>() { numberAttribute.Id });
        result = await projectionRepository.Query(query);
        result.TotalRecordsFound.Should().Be(0);

        // Get attributes when created in a flow of entity creation
        List<EntityAttributeConfigurationCreateUpdateRequest> createAttrList = new();
        for (int i = 0; i < 20; i++)
        {
            createAttrList.Add(new NumberAttributeConfigurationCreateUpdateRequest
            {
                MachineName = $"number_attribute{i}",
                Name = new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "Number Attribute" }
                },
                DefaultValue = 15,
                IsRequired = false,
                MaximumValue = 100,
                MinimumValue = -100
            });
        }

        configurationCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test2",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = createAttrList
        };
        (EntityConfigurationViewModel? createdThirdEntity, _) =
            await _eavService.CreateEntityConfiguration(configurationCreateRequest, CancellationToken.None);

        query.Filters = new()
        {
            new Filter
            {
                PropertyName = nameof(AttributeConfigurationProjectionDocument.UsedByEntityConfigurationIds).ToLowerInvariant(),
                Operator = FilterOperator.ArrayContains,
                Value = createdThirdEntity.Id.ToString()
            }
        };

        result = await projectionRepository.Query(query);
        result.TotalRecordsFound.Should().Be(20);
    }

    //
    // [TestMethod]
    //  public async Task UpdateEntityConfiguration_ChangeLocalizedStringAttribute_Success()
    //  {
    //      var cultureId = CultureInfo.GetCultureInfo("EN-us").LCID;
    //      const string newName = "newName";
    //      var configRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
    //
    //      var createdConfig = await _eavService.CreateEntityConfiguration(configRequest, CancellationToken.None);
    //
    //      var allAttributes = await _eavService.ListAttributes(100);
    //
    //      var nameAttrIndex = configRequest.Attributes.FindIndex(a => a.MachineName == "name");
    //      configRequest.Attributes[nameAttrIndex] = new LocalizedTextAttributeConfigurationCreateUpdateRequest()
    //      {
    //          MachineName = "name",
    //          Name = new List<LocalizedStringCreateRequest>()
    //          {
    //              new LocalizedStringCreateRequest()
    //              {
    //                  CultureInfoId = cultureId,
    //                  String = newName
    //              },
    //          },
    //      };
    //      var updateRequest = new EntityConfigurationUpdateRequest()
    //      {
    //          Attributes = configRequest.Attributes,
    //          Id = createdConfig.Id,
    //          MachineName = configRequest.MachineName,
    //          Name = configRequest.Name,
    //          PartitionKey = createdConfig.PartitionKey
    //      };
    //      var updatedConfig = await _eavService.UpdateEntityConfiguration(updateRequest, CancellationToken.None);
    //      updatedConfig.Attributes.First(a => a.MachineName == "name").As<LocalizedTextAttributeConfigurationViewModel>().Name.First().String.Should().Be(newName);
    //      updatedConfig.Attributes.First(a => a.MachineName == "name").As<LocalizedTextAttributeConfigurationViewModel>().Name.First().CultureInfoId.Should().Be(cultureId);
    //      updatedConfig.Id.Should().Be(createdConfig.Id);
    //      updatedConfig.MachineName.Should().Be(createdConfig.MachineName);
    //      updatedConfig.PartitionKey.Should().Be(createdConfig.PartitionKey);
    //  }
    //
    // [TestMethod]
    // public async Task UpdateEntityConfiguration_RemoveAttribute_Success()
    // {
    //     var configRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
    //     var createdConfig = await _eavService.CreateEntityConfiguration(configRequest, CancellationToken.None);
    //     const string playersMinMachineName = "players_min";
    //     var updateRequest = new EntityConfigurationUpdateRequest()
    //     {
    //         Attributes = configRequest.Attributes.Where(a => a.MachineName != playersMinMachineName).ToList(),
    //         Id = createdConfig.Id,
    //         MachineName = configRequest.MachineName,
    //         Name = configRequest.Name,
    //         PartitionKey = createdConfig.PartitionKey
    //     };
    //     var updatedConfig = await _eavService.UpdateEntityConfiguration(updateRequest, CancellationToken.None);
    //     updatedConfig.Attributes.FindIndex(a => a.MachineName == playersMinMachineName).Should().BeNegative();
    // }

    [TestMethod]
    public async Task UpdateEntityConfiguration_AddedNewAttribute_MachineNamesAreNotUnique()
    {
        var cultureId = CultureInfo.GetCultureInfo("EN-us").LCID;

        EntityConfigurationCreateRequest configRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        var newAttributeMachineName =
            (configRequest.Attributes[0] as AttributeConfigurationCreateUpdateRequest)!.MachineName!;

        (EntityConfigurationViewModel? createdConfig, _) =
            await _eavService.CreateEntityConfiguration(configRequest, CancellationToken.None);

        var newAttributeRequest = new NumberAttributeConfigurationCreateUpdateRequest
        {
            DefaultValue = 4,
            IsRequired = true,
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureId, String = "Average Time" }
            },
            MachineName = newAttributeMachineName,
            MinimumValue = 1,
            Description = new List<LocalizedStringCreateRequest>()
        };

        configRequest.Attributes.Add(newAttributeRequest);

        var updateRequest = new EntityConfigurationUpdateRequest
        {
            Attributes = configRequest.Attributes, Id = createdConfig.Id, Name = configRequest.Name
        };

        (EntityConfigurationViewModel? entityConfig, ProblemDetails? error) =
            await _eavService.UpdateEntityConfiguration(updateRequest, CancellationToken.None);
        entityConfig.Should().BeNull();
        error.Should().NotBeNull();
        error.Should().BeOfType<ValidationErrorResponse>();
        error.As<ValidationErrorResponse>().Errors.Should()
            .Contain(x => x.Value.Contains("Attributes machine name must be unique"));
    }

    [TestMethod]
    public async Task UpdateEntityConfiguration_AddedNewAttribute_Success()
    {
        var cultureId = CultureInfo.GetCultureInfo("EN-us").LCID;

        EntityConfigurationCreateRequest configRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfig, _) =
            await _eavService.CreateEntityConfiguration(configRequest, CancellationToken.None);
        const string newAttributeMachineName = "avg_time_mins";

        var newAttributeRequest = new NumberAttributeConfigurationCreateUpdateRequest
        {
            DefaultValue = 4,
            IsRequired = true,
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureId, String = "Average Time" }
            },
            MachineName = newAttributeMachineName,
            MinimumValue = 1,
            Description = new List<LocalizedStringCreateRequest>()
        };

        configRequest.Attributes.Add(newAttributeRequest);

        var updateRequest = new EntityConfigurationUpdateRequest
        {
            Attributes = configRequest.Attributes, Id = createdConfig.Id, Name = configRequest.Name
        };

        _ = await _eavService.UpdateEntityConfiguration(updateRequest, CancellationToken.None);
        //var newAttrIndex = updatedConfig.Attributes.FindIndex(a => a.MachineName == newAttributeMachineName);
        //newAttrIndex.Should().BePositive();
        //var newAttribute = updatedConfig.Attributes[newAttrIndex];
        //newAttribute.Should().NotBeNull();
        //newAttribute.Should().BeEquivalentTo(newAttributeRequest, opt => opt.ComparingRecordsByValue());
    }

    [TestMethod]
    public async Task UpdateEntityConfiguration_AddedNewAttribute_ValidationError()
    {
        _ = CultureInfo.GetCultureInfo("EN-us").LCID;

        EntityConfigurationCreateRequest configRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfig, _) =
            await _eavService.CreateEntityConfiguration(configRequest, CancellationToken.None);
        const string newAttributeMachineName = "some_new_attribute_name";

        var newAttributeRequest = new NumberAttributeConfigurationCreateUpdateRequest
        {
            DefaultValue = 4,
            IsRequired = true,
            MachineName = newAttributeMachineName,
            MinimumValue = 1,
            Description = new List<LocalizedStringCreateRequest>()
        };

        List<EntityAttributeConfigurationCreateUpdateRequest> newAttributesUpdateRequest =
            createdConfig.Attributes.Select(a => new EntityAttributeConfigurationCreateUpdateReferenceRequest()
            {
                AttributeConfigurationId = a.AttributeConfigurationId
            }).ToList<EntityAttributeConfigurationCreateUpdateRequest>();

        newAttributesUpdateRequest.Add(newAttributeRequest);

        var updateRequest = new EntityConfigurationUpdateRequest
        {
            Attributes = newAttributesUpdateRequest, Id = createdConfig.Id, Name = configRequest.Name
        };

        (_, ProblemDetails? errors) =
            await _eavService.UpdateEntityConfiguration(updateRequest, CancellationToken.None);
        errors.Should().NotBeNull();
        errors.As<ValidationErrorResponse>().Errors.Should().ContainKey(newAttributeMachineName);
        errors.As<ValidationErrorResponse>().Errors[newAttributeMachineName].Should()
            .Contain("Name cannot be empty");
    }

    [TestMethod]
    public async Task UpdateEntityConfiguration_ChangeAttributeName_Fail()
    {
        var cultureId = CultureInfo.GetCultureInfo("EN-us").LCID;
        const string newName = "newName";
        EntityConfigurationCreateRequest configRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfig, _) =
            await _eavService.CreateEntityConfiguration(configRequest, CancellationToken.None);
        var newNameRequest = new List<LocalizedStringCreateRequest>
        {
            new() { CultureInfoId = cultureId, String = newName }
        };
        configRequest.Name = newNameRequest;
        var updateRequest = new EntityConfigurationUpdateRequest
        {
            Attributes = configRequest.Attributes, Id = createdConfig.Id, Name = configRequest.Name
        };
        (EntityConfigurationViewModel? updatedConfig, ProblemDetails? updateError) =
            await _eavService.UpdateEntityConfiguration(updateRequest, CancellationToken.None);

        updateError.Should().NotBeNull();
        updateError.As<ValidationErrorResponse>().Errors.First().Key.Should().Be("Attributes[0]");
        updateError.As<ValidationErrorResponse>().Errors.First().Value.First().Should().Contain("should not be used");
    }

    [TestMethod]
    public async Task EntityConfigurationProjectionCreated()
    {
        ProjectionQueryResult<EntityConfigurationViewModel> configurationItemsStart =
            await _eavService.ListEntityConfigurations(
                ProjectionQueryExpressionExtensions.Where<EntityConfigurationProjectionDocument>(x =>
                    x.MachineName == "BoardGame"
                ),
                CancellationToken.None
            );

        configurationItemsStart.Records.Count.Should().Be(0);

        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel?, ProblemDetails?) createdConfiguration =
            await _eavService.CreateEntityConfiguration(
                configurationCreateRequest,
                CancellationToken.None
            );

        // verify projection is created
        ProjectionQueryResult<EntityConfigurationViewModel> configurationItems =
            await _eavService.ListEntityConfigurations(
                ProjectionQueryExpressionExtensions.Where<EntityConfigurationProjectionDocument>(x =>
                    x.MachineName == "BoardGame"
                )
            );

        configurationItems.Records.Count.Should().Be(1);
    }

    [TestMethod]
    public async Task GetEntityConfigurationProjectionByTenantId_Success()
    {
        EntityConfigurationCreateRequest configurationCreateRequest1 =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        EntityConfigurationCreateRequest configurationCreateRequest2 =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdConfiguration1, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest1,
            CancellationToken.None
        );

        (EntityConfigurationViewModel? createdConfiguration2, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest2,
            CancellationToken.None
        );

        // verify projection is created
        ProjectionQueryResult<EntityConfigurationViewModel> configurationItems =
            await _eavService.ListEntityConfigurations(
                ProjectionQueryExpressionExtensions.Where<EntityConfigurationProjectionDocument>(x =>
                    x.TenantId == createdConfiguration2.TenantId
                )
            );

        configurationItems.Records.Count.Should().Be(1);
        configurationItems.Records[0].Document?.TenantId.Should().Be(createdConfiguration2.TenantId);
    }

    [TestMethod]
    public async Task CreateNumberAttribute_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var numberAttribute = new NumberAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "testAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "testAttrDesc" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "testAttrName" }
            },
            DefaultValue = 15,
            IsRequired = true,
            MaximumValue = 100,
            MinimumValue = -100
        };

        var configCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { numberAttribute }
        };

        (EntityConfigurationViewModel? created, _) =
            await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);
        created.Attributes.Count.Should().Be(1);

        ProjectionQueryResult<AttributeConfigurationListItemViewModel> allAttributes =
            await _eavService.ListAttributes(new ProjectionQuery { Limit = 100 });

        allAttributes.Records.First().As<QueryResultDocument<AttributeConfigurationListItemViewModel>>()
            .Document?.Name.Should().BeEquivalentTo(numberAttribute.Name);
    }

    [TestMethod]
    public async Task CreateNumberAttribute_ValidationError()
    {
        var request = new NumberAttributeConfigurationCreateUpdateRequest { MachineName = "avg_time_mins" };

        (AttributeConfigurationViewModel? result, ValidationErrorResponse? errors) =
            await _eavService.CreateAttribute(request);
        result.Should().BeNull();
        errors.Should().NotBeNull();
        errors.As<ValidationErrorResponse>().Errors.Should().ContainKey(request.MachineName);
        errors.As<ValidationErrorResponse>().Errors[request.MachineName].Should().Contain("Name cannot be empty");
    }

    [TestMethod]
    public async Task CreateFileAttribute_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var fileAttribute = new FileAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "testAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "testAttrDesc" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "testAttrName" }
            },
            IsRequired = true,
            IsDownloadable = true
        };

        var configCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { fileAttribute }
        };

        (EntityConfigurationViewModel? created, _) =
            await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);
        created.Attributes.Count.Should().Be(1);

        AttributeConfigurationViewModel createdAttribute = await _eavService.GetAttribute(
            created.Attributes[0].AttributeConfigurationId,
            CancellationToken.None
        );

        createdAttribute.Name.Should().BeEquivalentTo(fileAttribute.Name);
        createdAttribute.As<FileAttributeConfigurationViewModel>().IsDownloadable.Should()
            .Be(fileAttribute.IsDownloadable);
    }

    [TestMethod]
    public async Task UpdateFileAttribute_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var fileAttribute = new FileAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "testAttr",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "testAttrName" }
            },
            IsRequired = true,
            IsDownloadable = true
        };

        var configCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { fileAttribute }
        };

        (EntityConfigurationViewModel? created, _) =
            await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);
        created!.Attributes.Count.Should().Be(1);

        fileAttribute.IsDownloadable = false;
        fileAttribute.IsRequired = false;

        (AttributeConfigurationViewModel? updated, _) = await _eavService.UpdateAttribute(
            created.Attributes[0].AttributeConfigurationId,
            fileAttribute,
            CancellationToken.None
        );

        AttributeConfigurationViewModel createdAttribute = await _eavService
            .GetAttribute(updated!.Id, CancellationToken.None);

        createdAttribute.IsRequired.Should().Be(fileAttribute.IsRequired);
        createdAttribute.As<FileAttributeConfigurationViewModel>().IsDownloadable.Should()
            .Be(fileAttribute.IsDownloadable);
    }

    [TestMethod]
    public async Task CreateFileAttributeInstance_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var fileAttribute = new FileAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "testAttr",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "testAttrName" }
            },
            IsRequired = true,
            IsDownloadable = true
        };

        var configCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { fileAttribute }
        };

        (EntityConfigurationViewModel? createdConfig, _) =
            await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);
        createdConfig!.Attributes.Count.Should().Be(1);

        var instanceRequest = new EntityInstanceCreateRequest
        {
            EntityConfigurationId = createdConfig!.Id,
            TenantId = createdConfig.TenantId,
            Attributes = new List<AttributeInstanceCreateUpdateRequest>
            {
                new FileAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = fileAttribute.MachineName,
                    Value = new FileAttributeValueCreateUpdateRequest
                    {
                        Filename = "test.pdf", Url = "/test.pdf"
                    }
                }
            }
        };

        (EntityInstanceViewModel createdInstance, ProblemDetails _) =
            await _eavService.CreateEntityInstance(instanceRequest, CancellationToken.None);

        createdInstance.Should().NotBeNull();

        createdInstance = await _eavService.GetEntityInstance(createdInstance.Id, createdConfig.Id.ToString());
        createdInstance.Attributes.Count.Should().Be(1);
        createdInstance.Attributes[0]
            .As<FileAttributeInstanceViewModel>()
            .Value
            .Url
            .Should()
            .Be("/test.pdf");
    }

    [TestMethod]
    public async Task GetNumberAttribute_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var numberAttribute = new NumberAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "testAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "testAttrDesc" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "testAttrName" }
            },
            DefaultValue = 15,
            IsRequired = true,
            MaximumValue = 100,
            MinimumValue = -100
        };

        var configCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { numberAttribute }
        };

        (EntityConfigurationViewModel? created, _) =
            await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);
        created.Attributes.Count.Should().Be(1);

        AttributeConfigurationViewModel createdAttribute = await _eavService.GetAttribute(
            created.Attributes[0].AttributeConfigurationId
        );

        createdAttribute.MachineName.Should().Be("testAttr");
    }

    [TestMethod]
    public async Task CreateValueFromListAttribute_Success()
    {
        AggregateRepository<ValueFromListAttributeConfiguration> valueFromListAttributeRepository =
            _aggregateRepositoryFactory.GetAggregateRepository<ValueFromListAttributeConfiguration>();

        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var valueFromListAttribute = new ValueFromListAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "testValueAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "ValueAttributeDescription" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "testValueAttributeName" }
            },
            IsRequired = true,
            ValuesList = new List<ValueFromListOptionCreateUpdateRequest>
            {
                new("First Option", null),
                new("Second 65 : Option! --!", null),
                new("Third option", "custom_machine_name")
            }
        };

        var entityConfigurationCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { valueFromListAttribute }
        };

        (EntityConfigurationViewModel? created, _) =
            await _eavService.CreateEntityConfiguration(entityConfigurationCreateRequest, CancellationToken.None);
        created!.Attributes.Count.Should().Be(1);

        ProjectionQueryResult<AttributeConfigurationListItemViewModel> allAttributes =
            await _eavService.ListAttributes(new ProjectionQuery { Limit = 100 });

        allAttributes.Records.First().As<QueryResultDocument<AttributeConfigurationListItemViewModel>>()
            .Document?.Name.Should().BeEquivalentTo(valueFromListAttribute.Name);

        ValueFromListAttributeConfiguration? valuesAttribute = await valueFromListAttributeRepository.LoadAsync(
            allAttributes.Records.First().Document!.Id!.Value,
            allAttributes.Records.First().Document!.Id.ToString()!,
            CancellationToken.None
        );
        valuesAttribute!.ValuesList.Count.Should().Be(3);
    }

    [TestMethod]
    public async Task CreateValueFromListAttribute_ValidationError()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var valueFromListAttribute = new ValueFromListAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "testValueAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "ValueAttributeDescription" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "testValueAttributeName" }
            },
            IsRequired = true,
            ValuesList = new List<ValueFromListOptionCreateUpdateRequest>
            {
                new("Repeated Name", "firstTestOption"), new("Repeated Name", "secondTestOption")
            }
        };

        var entityConfigurationCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { valueFromListAttribute }
        };

        // case check repeated name
        (EntityConfigurationViewModel entity, ProblemDetails errors) =
            await _eavService.CreateEntityConfiguration(entityConfigurationCreateRequest, CancellationToken.None);

        entity.Should().BeNull();
        errors.Should().BeOfType<ValidationErrorResponse>();
        errors.As<ValidationErrorResponse>().Errors.Should()
            .Contain(x => x.Value.Contains("Identical options not allowed"));

        // case check repeated machine name
        valueFromListAttribute.ValuesList = new List<ValueFromListOptionCreateUpdateRequest>
        {
            new("First Option Name", "repeatedMachineName"), new("Second Oprion Name", "repeatedMachineName")
        };

        (entity, errors) =
            await _eavService.CreateEntityConfiguration(entityConfigurationCreateRequest, CancellationToken.None);

        entity.Should().BeNull();
        errors.Should().BeOfType<ValidationErrorResponse>();
        errors.As<ValidationErrorResponse>().Errors.Should()
            .Contain(x => x.Value.Contains("Identical options not allowed"));

        // case check empty options list
        valueFromListAttribute.ValuesList = new List<ValueFromListOptionCreateUpdateRequest>();

        (entity, errors) =
            await _eavService.CreateEntityConfiguration(entityConfigurationCreateRequest, CancellationToken.None);

        entity.Should().BeNull();
        errors.Should().BeOfType<ValidationErrorResponse>();
        errors.As<ValidationErrorResponse>().Errors.Should()
            .Contain(x => x.Value.Contains("Cannot create attribute without options"));
    }

    [TestMethod]
    public async Task UpdateValueFromListAttribute_Success()
    {
        AggregateRepository<ValueFromListAttributeConfiguration> valueFromListRepository =
            _aggregateRepositoryFactory.GetAggregateRepository<ValueFromListAttributeConfiguration>();

        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var valueFromListAttributeCreateRequest = new ValueFromListAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "testValueAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "ValueAttributeDescription" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "testValueAttributeName" }
            },
            IsRequired = true,
            ValuesList = new List<ValueFromListOptionCreateUpdateRequest>
            {
                new("Premium wrap", "firstTestOption"), new("Card with wishes from shop", "secondTestOption")
            }
        };

        (AttributeConfigurationViewModel? valueFromListAttribute, _) =
            await _eavService.CreateAttribute(valueFromListAttributeCreateRequest, CancellationToken.None);


        // create request with changed properties and update attribute
        _ = Guid.NewGuid().ToString();
        valueFromListAttributeCreateRequest.ValuesList = new List<ValueFromListOptionCreateUpdateRequest>
        {
            new("Card with wishes from shop", "changedAttribute")
        };

        (AttributeConfigurationViewModel? changedAttribute, _) =
            await _eavService.UpdateAttribute(valueFromListAttribute.Id,
                valueFromListAttributeCreateRequest!,
                CancellationToken.None
            );

        ValueFromListAttributeConfiguration? changedValueFromListAttribute =
            await valueFromListRepository.LoadAsync(changedAttribute!.Id,
                changedAttribute.Id.ToString(),
                CancellationToken.None
            );
        changedValueFromListAttribute.ValuesList.Count.Should().Be(1);
        changedValueFromListAttribute.ValuesList.FirstOrDefault()!.MachineName.Should().Be("changedAttribute");
    }

    [TestMethod]
    public async Task CreateEntityInstanceWithValueFromListAttribute_ValidationError()
    {
        // create entity configuration with value from list attribute
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var valueFromListAttribute = new ValueFromListAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "testValueAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "ValueAttributeDescription" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "testValueAttributeName" }
            },
            IsRequired = true,
            ValuesList = new List<ValueFromListOptionCreateUpdateRequest> { new("firstTestOption", "Premium wrap") }
        };

        var entityConfigurationCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { valueFromListAttribute }
        };

        (EntityConfigurationViewModel? entityConfiguration, _) =
            await _eavService.CreateEntityConfiguration(entityConfigurationCreateRequest, CancellationToken.None);

        // create entity instance using wrong type of attribute
        (EntityInstanceViewModel result, ProblemDetails validationErrors) = await _eavService.CreateEntityInstance(
            new EntityInstanceCreateRequest
            {
                EntityConfigurationId = entityConfiguration.Id,
                Attributes = new List<AttributeInstanceCreateUpdateRequest>
                {
                    new NumberAttributeInstanceCreateUpdateRequest
                    {
                        ConfigurationAttributeMachineName = "testValueAttr", Value = int.MaxValue
                    }
                }
            }
        );

        result.Should().BeNull();
        validationErrors.Should().BeOfType<ValidationErrorResponse>();
        validationErrors.As<ValidationErrorResponse>().Errors["testValueAttr"].First().Should()
            .Be("Cannot validate attribute. Expected attribute type: Value from list");

        (result, validationErrors) = await _eavService.CreateEntityInstance(new EntityInstanceCreateRequest
            {
                EntityConfigurationId = entityConfiguration.Id,
                Attributes = new List<AttributeInstanceCreateUpdateRequest>
                {
                    new ValueFromListAttributeInstanceCreateUpdateRequest
                    {
                        ConfigurationAttributeMachineName = "testValueAttr", Value = "notvalidmachineneme"
                    }
                }
            }
        );

        result.Should().BeNull();
        validationErrors.Should().BeOfType<ValidationErrorResponse>();
        validationErrors.As<ValidationErrorResponse>().Errors["testValueAttr"].First().Should()
            .Be("Cannot validate attribute. Wrong option");
    }

    [TestMethod]
    public async Task CreateSerialAttribute_Success()
    {
        AggregateRepository<SerialAttributeConfiguration> serialAttributeRepository =
            _aggregateRepositoryFactory.GetAggregateRepository<SerialAttributeConfiguration>();

        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var serialAttributeCreateRequest = new SerialAttributeConfigurationCreateRequest
        {
            MachineName = "serialAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "SerialAttributeDescription" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "serialAttributeName" }
            },
            IsRequired = true,
            StartingNumber = 1,
            Increment = 1
        };

        var entityConfigurationCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { serialAttributeCreateRequest }
        };

        (EntityConfigurationViewModel? created, _) =
            await _eavService.CreateEntityConfiguration(entityConfigurationCreateRequest, CancellationToken.None);
        created!.Attributes.Count.Should().Be(1);

        ProjectionQueryResult<AttributeConfigurationListItemViewModel> allAttributes =
            await _eavService.ListAttributes(new ProjectionQuery { Limit = 100 });

        allAttributes.Records.First().As<QueryResultDocument<AttributeConfigurationListItemViewModel>>()
            .Document?.Name.Should().BeEquivalentTo(serialAttributeCreateRequest.Name);

        allAttributes.Records.First().As<QueryResultDocument<AttributeConfigurationListItemViewModel>>()
            .Document?.MachineName.Should().Be(serialAttributeCreateRequest.MachineName);

        SerialAttributeConfiguration? serialAttribute = await serialAttributeRepository.LoadAsync(
            allAttributes.Records.First().Document!.Id!.Value,
            allAttributes.Records.First().Document!.Id.ToString()!,
            CancellationToken.None
        );

        serialAttribute!.As<SerialAttributeConfiguration>().StartingNumber.Should()
            .Be(serialAttributeCreateRequest.StartingNumber);
        serialAttribute!.As<SerialAttributeConfiguration>().Increment.Should()
            .Be(serialAttributeCreateRequest.Increment);
    }

    [TestMethod]
    public async Task CreateSerialAttribute_ValidationError()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var serialAttributeCreateRequest = new SerialAttributeConfigurationCreateRequest
        {
            MachineName = "serialAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "SerialAttributeDescription" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "serialAttributeName" }
            },
            IsRequired = true,
            StartingNumber = -1,
            Increment = 0
        };

        (AttributeConfigurationViewModel _, ValidationErrorResponse errors) =
            await _eavService.CreateAttribute(serialAttributeCreateRequest, CancellationToken.None);
        errors.Should().BeOfType<ValidationErrorResponse>();
        errors.Errors.Should().Contain(x => x.Value.Contains("Increment value must not be negative or 0"));
        errors.Errors.Should().Contain(x => x.Value.Contains("Statring number must not be negative"));

        serialAttributeCreateRequest.Increment = -1;
        (_, errors) = await _eavService.CreateAttribute(serialAttributeCreateRequest, CancellationToken.None);
        errors.Errors.Should().Contain(x => x.Value.Contains("Increment value must not be negative or 0"));
    }

    [TestMethod]
    public async Task UpdateSerialAttribute_Success()
    {
        AggregateRepository<SerialAttributeConfiguration> serialAttributeRepository =
            _aggregateRepositoryFactory.GetAggregateRepository<SerialAttributeConfiguration>();

        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var serialAttributeCreateRequest = new SerialAttributeConfigurationCreateRequest
        {
            MachineName = "serialAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "SerialAttributeDescription" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "serialAttributeName" }
            },
            IsRequired = true,
            StartingNumber = 100,
            Increment = 1
        };

        var entityConfigurationCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { serialAttributeCreateRequest }
        };

        (EntityConfigurationViewModel? created, _) =
            await _eavService.CreateEntityConfiguration(entityConfigurationCreateRequest, CancellationToken.None);

        // create update request and change attribute
        var serialAttributeUpdateRequest = new SerialAttributeConfigurationUpdateRequest
        {
            MachineName = "serialAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "SerialAttributeDescription" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "serialAttributeName" }
            },
            IsRequired = true,
            Increment = 5
        };

        (AttributeConfigurationViewModel? updatedAttribute, ProblemDetails _) = await _eavService.UpdateAttribute(
            created.Attributes.FirstOrDefault().AttributeConfigurationId,
            serialAttributeUpdateRequest,
            CancellationToken.None
        );

        SerialAttributeConfiguration? serialAttribute = await serialAttributeRepository.LoadAsync(
            updatedAttribute.Id!,
            updatedAttribute.Id.ToString(),
            CancellationToken.None
        );

        serialAttribute.As<SerialAttributeConfiguration>().Increment.Should()
            .Be(serialAttributeUpdateRequest.Increment);
        serialAttribute.As<SerialAttributeConfiguration>().StartingNumber.Should()
            .Be(serialAttributeCreateRequest.StartingNumber);
    }

    [TestMethod]
    public async Task CreateEntityInstanceWithSerialAttributes_Success()
    {
        AggregateRepository<EntityConfiguration> entityRepository =
            _aggregateRepositoryFactory.GetAggregateRepository<EntityConfiguration>();

        AggregateRepository<EntityInstance> entityInstanceRepository =
            _aggregateRepositoryFactory.GetAggregateRepository<EntityInstance>();

        // create entity configuration and instance with serial attribute
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var serialAttributeCreateRequest = new SerialAttributeConfigurationCreateRequest
        {
            MachineName = "serialAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "SerialAttributeDescription" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "serialAttributeName" }
            },
            IsRequired = true,
            StartingNumber = 100,
            Increment = 555
        };

        var entityConfigurationCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { serialAttributeCreateRequest }
        };

        (EntityConfigurationViewModel? entityConfig, _) =
            await _eavService.CreateEntityConfiguration(entityConfigurationCreateRequest, CancellationToken.None);

        (EntityInstanceViewModel result, ProblemDetails _) = await _eavService.CreateEntityInstance(
            new EntityInstanceCreateRequest
            {
                EntityConfigurationId = entityConfig.Id,
                Attributes = new List<AttributeInstanceCreateUpdateRequest>
                {
                    new SerialAttributeInstanceCreateUpdateRequest
                    {
                        ConfigurationAttributeMachineName = "serialAttr"
                    }
                }
            }
        );
        result.Attributes.FirstOrDefault().ConfigurationAttributeMachineName.Should()
            .Be(serialAttributeCreateRequest.MachineName);

        // check attribute override value in entity configuration
        EntityConfiguration entity = await entityRepository.LoadAsyncOrThrowNotFound(entityConfig.Id,
            entityConfig.PartitionKey,
            CancellationToken.None
        );
        var attributeExternalValue = JsonSerializer.Deserialize<long>(
            entity.Attributes.FirstOrDefault().AttributeConfigurationExternalValues.FirstOrDefault().ToString()
        );

        attributeExternalValue.Should()
            .Be(serialAttributeCreateRequest
                .As<SerialAttributeConfigurationCreateRequest>().StartingNumber
            );

        // check entity instance
        EntityInstance entityInstance =
            await entityInstanceRepository.LoadAsyncOrThrowNotFound(result.Id, result.PartitionKey);

        entityInstance.Attributes.FirstOrDefault(x =>
                x.ConfigurationAttributeMachineName == serialAttributeCreateRequest.MachineName
            )
            .As<SerialAttributeInstance>().Value.Should().Be(serialAttributeCreateRequest.StartingNumber);

        // create another entity instance
        (result, _) = await _eavService.CreateEntityInstance(new EntityInstanceCreateRequest
            {
                EntityConfigurationId = entityConfig.Id,
                Attributes = new List<AttributeInstanceCreateUpdateRequest>
                {
                    new SerialAttributeInstanceCreateUpdateRequest
                    {
                        ConfigurationAttributeMachineName = "serialAttr"
                    }
                }
            }
        );

        // check that override value in entity configuration was updated
        entity = await entityRepository.LoadAsyncOrThrowNotFound(entityConfig.Id,
            entityConfig.PartitionKey,
            CancellationToken.None
        );
        attributeExternalValue = JsonSerializer.Deserialize<long>(
            entity.Attributes.FirstOrDefault().AttributeConfigurationExternalValues.FirstOrDefault().ToString()
        );

        attributeExternalValue.Should()
            .Be(serialAttributeCreateRequest
                    .As<SerialAttributeConfigurationCreateRequest>().StartingNumber
                + serialAttributeCreateRequest.Increment
            );

        //check another entity instance
        entityInstance = await entityInstanceRepository.LoadAsyncOrThrowNotFound(result.Id, result.PartitionKey);
        entityInstance.Attributes.FirstOrDefault(x =>
                x.ConfigurationAttributeMachineName == serialAttributeCreateRequest.MachineName
            )
            .As<SerialAttributeInstance>().Value.Should().Be(serialAttributeCreateRequest.StartingNumber
                                                             + serialAttributeCreateRequest.Increment
            );
    }

    [TestMethod]
    public async Task AddAttributeToEntityConfiguration_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;

        var configCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "test" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>()
        };

        (EntityConfigurationViewModel? createdEntityConfiguration, _) =
            await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);

        var numberAttribute = new NumberAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "testAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "testAttrDesc" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "testAttrName" }
            },
            DefaultValue = 15,
            IsRequired = true,
            MaximumValue = 100,
            MinimumValue = -100
        };

        (AttributeConfigurationViewModel? createdAttribute, _) =
            await _eavService.CreateAttribute(numberAttribute, CancellationToken.None);
        createdAttribute.Should().NotBeNull();

        await _eavService.AddAttributeToEntityConfiguration(
            createdAttribute.Id,
            createdEntityConfiguration.Id,
            CancellationToken.None
        );

        // check that attribute is added
        EntityConfigurationViewModel updatedEntityConfiguration = await _eavService.GetEntityConfiguration(
            createdEntityConfiguration.Id
        );

        updatedEntityConfiguration.Attributes.Any(x => x.AttributeConfigurationId == createdAttribute.Id)
            .Should()
            .BeTrue();
    }

    [TestMethod]
    public async Task AddAttributeToEntityConfiguration_MachineNamesAreNotUnique()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;

        EntityConfigurationCreateRequest configCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        var newAttributeMachineName =
            (configCreateRequest.Attributes[0] as AttributeConfigurationCreateUpdateRequest)!.MachineName!;

        (EntityConfigurationViewModel? createdEntityConfiguration, _) =
            await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);

        var numberAttribute = new NumberAttributeConfigurationCreateUpdateRequest
        {
            MachineName = newAttributeMachineName,
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "testAttrDesc" }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "testAttrName" }
            },
            DefaultValue = 15,
            IsRequired = true,
            MaximumValue = 100,
            MinimumValue = -100
        };

        (AttributeConfigurationViewModel? createdAttribute, _) =
            await _eavService.CreateAttribute(numberAttribute, CancellationToken.None);
        createdAttribute.Should().NotBeNull();

        (EntityConfigurationViewModel? entityConfig, ProblemDetails? error) =
            await _eavService.AddAttributeToEntityConfiguration(
                createdAttribute.Id,
                createdEntityConfiguration.Id,
                CancellationToken.None
            );

        entityConfig.Should().BeNull();
        error.Should().NotBeNull();
        error.Should().BeOfType<ValidationErrorResponse>();
        error.As<ValidationErrorResponse>().Errors.Should()
            .Contain(x => x.Value.Contains("Attributes machine name must be unique"));
    }

    [TestMethod]
    public async Task UpdateInstance_UpdateAttribute_Success()
    {
        const string changedAttributeName = "players_max";

        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        List<AttributeInstanceCreateUpdateRequest> attributesRequest = entityInstanceCreateRequest.Attributes;
        (EntityInstanceViewModel createdInstance, _) =
            await _eavService.CreateEntityInstance(entityInstanceCreateRequest);

        var playerMaxIndex =
            attributesRequest.FindIndex(a => a.ConfigurationAttributeMachineName == changedAttributeName);
        attributesRequest[playerMaxIndex] = new NumberAttributeInstanceCreateUpdateRequest
        {
            ConfigurationAttributeMachineName = changedAttributeName, Value = 10
        };
        var updateRequest = new EntityInstanceUpdateRequest
        {
            EntityConfigurationId = createdConfiguration.Id, Attributes = attributesRequest, Id = createdInstance.Id
        };

        (EntityInstanceViewModel updatedInstance, _) =
            await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(),
                updateRequest,
                CancellationToken.None
            );
        updatedInstance.Attributes.First(a => a.ConfigurationAttributeMachineName == changedAttributeName)
            .As<NumberAttributeInstanceViewModel>().Value.Should().Be(10);
    }

    [TestMethod]
    public async Task UpdateInstance_UpdateAttribute_FailValidation()
    {
        const string changedAttributeName = "players_max";

        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        List<AttributeInstanceCreateUpdateRequest> attributesRequest = entityInstanceCreateRequest.Attributes;
        (EntityInstanceViewModel createdInstance, _) =
            await _eavService.CreateEntityInstance(entityInstanceCreateRequest);

        var playerMaxIndex =
            attributesRequest.FindIndex(a => a.ConfigurationAttributeMachineName == changedAttributeName);
        attributesRequest[playerMaxIndex] = new NumberAttributeInstanceCreateUpdateRequest
        {
            ConfigurationAttributeMachineName = changedAttributeName, Value = 20
        };
        var updateRequest = new EntityInstanceUpdateRequest
        {
            EntityConfigurationId = createdConfiguration.Id, Attributes = attributesRequest, Id = createdInstance.Id
        };

        (EntityInstanceViewModel updatedInstance, ProblemDetails validationErrors) =
            await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(),
                updateRequest,
                CancellationToken.None
            );
        updatedInstance.Should().BeNull();
        validationErrors.As<ValidationErrorResponse>().Errors.Should().ContainKey(changedAttributeName);
    }

    [TestMethod]
    public async Task UpdateInstance_AddAttribute_Success()
    {
        const string changedAttributeName = "avg_time_mins";

        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        List<AttributeInstanceCreateUpdateRequest> attributesRequest = entityInstanceCreateRequest.Attributes;
        (EntityInstanceViewModel createdInstance, _) =
            await _eavService.CreateEntityInstance(entityInstanceCreateRequest);

        attributesRequest.Add(new NumberAttributeInstanceCreateUpdateRequest
            {
                ConfigurationAttributeMachineName = changedAttributeName, Value = 30
            }
        );

        var updateRequest = new EntityInstanceUpdateRequest
        {
            EntityConfigurationId = createdConfiguration.Id, Attributes = attributesRequest, Id = createdInstance.Id
        };

        (EntityInstanceViewModel updatedInstance, _) =
            await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(),
                updateRequest,
                CancellationToken.None
            );
        updatedInstance.Attributes.First(a => a.ConfigurationAttributeMachineName == changedAttributeName)
            .As<NumberAttributeInstanceViewModel>().Value.Should().Be(30);
    }

    [TestMethod]
    public async Task CreateInstance_NumberOfItemsWithAttributeUpdated_Success()
    {
        const string changedAttributeName = "avg_time_mins";

        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        List<AttributeInstanceCreateUpdateRequest> attributesRequest = entityInstanceCreateRequest.Attributes;
        (EntityInstanceViewModel createdInstance, _) =
            await _eavService.CreateEntityInstance(entityInstanceCreateRequest);

        attributesRequest.Add(new NumberAttributeInstanceCreateUpdateRequest
            {
                ConfigurationAttributeMachineName = changedAttributeName, Value = 30
            }
        );

        var updateRequest = new EntityInstanceUpdateRequest
        {
            EntityConfigurationId = createdConfiguration.Id, Attributes = attributesRequest, Id = createdInstance.Id
        };

        await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(),
            updateRequest,
            CancellationToken.None
        );

        ProjectionQueryResult<AttributeConfigurationListItemViewModel> attributeConfigurations =
            await _eavService.ListAttributes(
                new ProjectionQuery
                {
                    Filters = new List<Filter>
                    {
                        new()
                        {
                            PropertyName = nameof(AttributeConfigurationProjectionDocument.MachineName),
                            Operator = FilterOperator.Equal,
                            Value = changedAttributeName
                        }
                    }
                }
            );

        attributeConfigurations.Records.First().Document?.NumberOfEntityInstancesWithAttribute.Should().Be(1);
    }

    [TestMethod]
    public async Task UpdateInstance_AddNumberAttribute_InvalidNumberType()
    {
        const string changedAttributeName = "avg_time_mins";

        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        EntityAttributeConfigurationCreateUpdateRequest numberAttributeConfig = configurationCreateRequest
            .Attributes
            .First(x => x is NumberAttributeConfigurationCreateUpdateRequest
                        && ((NumberAttributeConfigurationCreateUpdateRequest)x).MachineName == changedAttributeName
            );

        (numberAttributeConfig as NumberAttributeConfigurationCreateUpdateRequest)!.NumberType =
            NumberAttributeType.Integer;

        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);
        entityInstanceCreateRequest.Attributes.RemoveAll(a =>
            a.ConfigurationAttributeMachineName == changedAttributeName
        );
        entityInstanceCreateRequest.Attributes.Add(new NumberAttributeInstanceCreateUpdateRequest
            {
                ConfigurationAttributeMachineName = changedAttributeName, Value = 30.55M
            }
        );

        (EntityInstanceViewModel instance, ProblemDetails error) =
            await _eavService.CreateEntityInstance(entityInstanceCreateRequest);
        instance.Should().BeNull();
        error.As<ValidationErrorResponse>().Errors.Should()
            .Contain(x => x.Value.Contains("Value is not an integer value"));
    }

    [TestMethod]
    public async Task UpdateInstance_AddAttribute_IgnoreAttributeNotInConfig()
    {
        const string changedAttributeName = "min_time_mins";

        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        List<AttributeInstanceCreateUpdateRequest> attributesRequest = entityInstanceCreateRequest.Attributes;
        (EntityInstanceViewModel createdInstance, _) =
            await _eavService.CreateEntityInstance(entityInstanceCreateRequest);

        attributesRequest.Add(new NumberAttributeInstanceCreateUpdateRequest
            {
                ConfigurationAttributeMachineName = changedAttributeName, Value = 30
            }
        );

        var updateRequest = new EntityInstanceUpdateRequest
        {
            EntityConfigurationId = createdConfiguration.Id, Attributes = attributesRequest, Id = createdInstance.Id
        };

        (EntityInstanceViewModel updatedInstance, _) =
            await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(),
                updateRequest,
                CancellationToken.None
            );
        updatedInstance.Attributes.FirstOrDefault(a => a.ConfigurationAttributeMachineName == changedAttributeName)
            .Should().BeNull();
    }

    [TestMethod]
    public async Task UpdateInstance_RemoveAttribute_Success()
    {
        const string changedAttributeName = "description";

        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        List<AttributeInstanceCreateUpdateRequest> attributesRequest = entityInstanceCreateRequest.Attributes;
        (EntityInstanceViewModel createdInstance, _) =
            await _eavService.CreateEntityInstance(entityInstanceCreateRequest);

        attributesRequest = attributesRequest
            .Where(a => a.ConfigurationAttributeMachineName != changedAttributeName).ToList();
        var updateRequest = new EntityInstanceUpdateRequest
        {
            EntityConfigurationId = createdConfiguration.Id, Attributes = attributesRequest, Id = createdInstance.Id
        };

        (EntityInstanceViewModel updatedInstance, _) =
            await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(),
                updateRequest,
                CancellationToken.None
            );
        updatedInstance.Attributes.FirstOrDefault(a => a.ConfigurationAttributeMachineName == changedAttributeName)
            .Should().BeNull();
    }

    [TestMethod]
    public async Task UpdateInstance_RemoveAttribute_FailValidation()
    {
        const string changedAttributeName = "players_max";

        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        List<AttributeInstanceCreateUpdateRequest> attributesRequest = entityInstanceCreateRequest.Attributes;
        (EntityInstanceViewModel createdInstance, _) =
            await _eavService.CreateEntityInstance(entityInstanceCreateRequest);

        attributesRequest = attributesRequest
            .Where(a => a.ConfigurationAttributeMachineName != changedAttributeName).ToList();
        var updateRequest = new EntityInstanceUpdateRequest
        {
            EntityConfigurationId = createdConfiguration.Id, Attributes = attributesRequest, Id = createdInstance.Id
        };

        (EntityInstanceViewModel updatedInstance, ProblemDetails errors) =
            await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(),
                updateRequest,
                CancellationToken.None
            );
        updatedInstance.Should().BeNull();
        errors.As<ValidationErrorResponse>().Errors.Should().ContainKey(changedAttributeName);
    }

    [TestMethod]
    public async Task CreateNumberAttributeAsReference_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var priceAttribute = new NumberAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "price",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "Product Price" }
                },
            Name = new List<LocalizedStringCreateRequest> { new() { CultureInfoId = cultureInfoId, String = "Price" } },
            DefaultValue = 1,
            IsRequired = true,
            MaximumValue = 100,
            MinimumValue = 1
        };


        (AttributeConfigurationViewModel? priceAttributeCreated, _) =
            await _eavService.CreateAttribute(priceAttribute, CancellationToken.None);

        var entityConfigurationCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "product",
            Name =
                new List<LocalizedStringCreateRequest> { new() { CultureInfoId = cultureInfoId, String = "Product" } },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>
            {
                new EntityAttributeConfigurationCreateUpdateReferenceRequest
                {
                    AttributeConfigurationId = priceAttributeCreated.Id
                },
                new TextAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "additional_notes",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new() { CultureInfoId = cultureInfoId, String = "Additional Notes" }
                    },
                    IsRequired = false,
                    DefaultValue = ""
                }
            }
        };

        _ = await _eavService.CreateEntityConfiguration(
            entityConfigurationCreateRequest,
            CancellationToken.None
        );

        ProjectionQueryResult<AttributeConfigurationListItemViewModel> allAttributes =
            await _eavService.ListAttributes(new ProjectionQuery { Limit = 1000 });
        allAttributes.Records.Count.Should().Be(2);
    }

    [TestMethod]
    public async Task CreateInstanceAndQuery()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityConfigurationViewModel configuration = await _eavService.GetEntityConfiguration(
            createdConfiguration.Id
        );

        //configuration.Should().BeEquivalentTo(createdConfiguration);

        EntityInstanceCreateRequest instanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        (EntityInstanceViewModel createdInstance, ProblemDetails createProblemDetails) =
            await _eavService.CreateEntityInstance(instanceCreateRequest);

        createdInstance.EntityConfigurationId.Should().Be(instanceCreateRequest.EntityConfigurationId);
        createdInstance.TenantId.Should().Be(instanceCreateRequest.TenantId);
        createdInstance.Attributes.Should()
            .BeEquivalentTo(instanceCreateRequest.Attributes, x => x.Excluding(w => w.ValueType));

        var query = new ProjectionQuery
        {
            Filters = new List<Filter> { new("Id", FilterOperator.Equal, createdInstance.Id) }
        };

        await _eavService
            .QueryInstances(createdConfiguration.Id, query);
    }

    [TestMethod]
    public async Task SimpleJsonConverter()
    {
        var jsonString =
            "{\"valueType\" : 2, \"machineName\" : \"test\", \"name\" : [{\"string\" : \"Test\", \"cultureInfoId\" : 1033}], \"description\" : [{\"string\" : \"Test\", \"cultureInfoId\" : 1033}], \"defaultValue\" : 0, \"isRequired\" : true, \"maximumValue\" : 10, \"minimumValue\" : 0 }";
        var deserializeOptions = new JsonSerializerOptions();
        deserializeOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        var attribute =
            JsonSerializer.Deserialize<AttributeConfigurationCreateUpdateRequest>(jsonString, deserializeOptions)!;
        attribute.As<NumberAttributeConfigurationCreateUpdateRequest>().ValueType.Should()
            .Be(EavAttributeType.Number);
        attribute.MachineName.Should().Be("test");
    }

    [TestMethod]
    public async Task SimpleToPolymorphJsonConverter()
    {
        var jsonString =
            "{\"typeName\": \"CloudFabric.EAV.Models.RequestModels.Attributes.NumberAttributeConfigurationCreateUpdateRequest\", \"typeValue\": { \"valueType\" : 2, \"machineName\" : \"test\", \"name\" : [{\"string\" : \"Test\", \"cultureInfoId\" : 1033}], \"description\" : [{\"string\" : \"Test\", \"cultureInfoId\" : 1033}], \"defaultValue\" : 0, \"isRequired\" : true, \"maximumValue\" : -1, \"minimumValue\" : 0 }}";
        var deserializeOptions = new JsonSerializerOptions();
        var attribute =
            JsonSerializer.Deserialize<AttributeConfigurationCreateUpdateRequest>(jsonString, deserializeOptions)!;
        attribute.As<NumberAttributeConfigurationCreateUpdateRequest>().ValueType.Should()
            .Be(EavAttributeType.Number);
        attribute.MachineName.Should().Be("test");
    }

    [TestMethod]
    public async Task EntityInstanceJsonConverter()
    {
        var jsonString = @"
        {
            ""entityConfigurationId"": ""a786eaac-66c6-44e4-8a82-3b5cf87b43e1"",
            ""tenantId"": ""a786eaac-66c6-44e4-8a82-3b5cf87b43e1"",
            ""attributes"": [
                {
                    ""configurationAttributeMachineName"": ""test-number"",
                    ""valueType"": ""Number"",
                    ""value"": 5
                },
                {
                    ""configurationAttributeMachineName"": ""test-text"",
                    ""valueType"": ""Text"",
                    ""value"": ""Json deserialization test""
                },
                {
                    ""configurationAttributeMachineName"": ""test-date"",
                    ""valueType"": ""DateRange"",
                    ""value"": {
                        ""from"": ""2023-01-24"",
                        ""to"": ""2023-01-25""
                    }
                }
            ]
        }";

        var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var deserializedInstance =
            JsonSerializer.Deserialize<EntityInstanceCreateRequest>(jsonString, deserializeOptions)!;

        deserializedInstance.EntityConfigurationId.Should().Be(Guid.Parse("a786eaac-66c6-44e4-8a82-3b5cf87b43e1"));
        deserializedInstance.TenantId.Should().Be(Guid.Parse("a786eaac-66c6-44e4-8a82-3b5cf87b43e1"));

        deserializedInstance.Attributes[0].ConfigurationAttributeMachineName.Should().Be("test-number");
        deserializedInstance.Attributes[0].ValueType.Should().Be(EavAttributeType.Number);
        deserializedInstance.Attributes[0].As<NumberAttributeInstanceCreateUpdateRequest>().Value.Should().Be(5);

        deserializedInstance.Attributes[1].ConfigurationAttributeMachineName.Should().Be("test-text");
        deserializedInstance.Attributes[1].ValueType.Should().Be(EavAttributeType.Text);
        deserializedInstance.Attributes[1].As<TextAttributeInstanceCreateUpdateRequest>().Value.Should()
            .Be("Json deserialization test");

        deserializedInstance.Attributes[2].ConfigurationAttributeMachineName.Should().Be("test-date");
        deserializedInstance.Attributes[2].ValueType.Should().Be(EavAttributeType.DateRange);
        deserializedInstance.Attributes[2].As<DateRangeAttributeInstanceCreateUpdateRequest>().Value.From.Should()
            .Be(DateTime.Parse("2023-01-24"));
        deserializedInstance.Attributes[2].As<DateRangeAttributeInstanceCreateUpdateRequest>().Value.To.Should()
            .Be(DateTime.Parse("2023-01-25"));
    }

    [TestMethod]
    public async Task AddAttributeMetadata_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var priceAttribute = new NumberAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "price",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "Product Price" }
                },
            Name = new List<LocalizedStringCreateRequest> { new() { CultureInfoId = cultureInfoId, String = "Price" } },
            DefaultValue = 1,
            IsRequired = true,
            MaximumValue = 100,
            MinimumValue = 1,
            Metadata = JsonSerializer.Serialize(new LocalizedStringCreateRequest { String = "test-metadata" })
        };

        var entityConfigurationCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "product",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "Product" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { priceAttribute }
        };


        (EntityConfigurationViewModel? createdConfig, _) =
            await _eavService.CreateEntityConfiguration(entityConfigurationCreateRequest, CancellationToken.None);
        createdConfig.Should().NotBeNull();

        AttributeConfigurationViewModel attribute = await _eavService.GetAttribute(
            createdConfig!.Attributes[0].AttributeConfigurationId,
            CancellationToken.None
        );

        attribute.Metadata.Should().NotBeNull();
        var deserializedMetadata = JsonSerializer.Deserialize<LocalizedStringCreateRequest>(attribute.Metadata!);
        deserializedMetadata!.String.Should().Be("test-metadata");

        // check projections
        ProjectionQueryResult<AttributeConfigurationListItemViewModel> attributes =
            await _eavService.ListAttributes(new ProjectionQuery());
        attributes.Records.First().Document!.Metadata.Should().Be(attribute.Metadata);
    }

    [TestMethod]
    public async Task UpdateAttributeMetadata_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var priceAttribute = new NumberAttributeConfigurationCreateUpdateRequest
        {
            MachineName = "price",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = cultureInfoId, String = "Product Price" }
                },
            Name = new List<LocalizedStringCreateRequest> { new() { CultureInfoId = cultureInfoId, String = "Price" } },
            DefaultValue = 1,
            IsRequired = true,
            MaximumValue = 100,
            MinimumValue = 1,
            Metadata = JsonSerializer.Serialize(new LocalizedStringCreateRequest { String = "test-metadata" })
        };

        var entityConfigurationCreateRequest = new EntityConfigurationCreateRequest
        {
            MachineName = "product",
            Name = new List<LocalizedStringCreateRequest>
            {
                new() { CultureInfoId = cultureInfoId, String = "Product" }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest> { priceAttribute }
        };


        (EntityConfigurationViewModel? createdConfig, _) =
            await _eavService.CreateEntityConfiguration(entityConfigurationCreateRequest, CancellationToken.None);
        createdConfig.Should().NotBeNull();

        // update attribute metadata
        priceAttribute.Metadata = "updated metadata";
        (AttributeConfigurationViewModel? updatedAttribute, _) = await _eavService.UpdateAttribute(
            createdConfig.Attributes[0].AttributeConfigurationId,
            priceAttribute,
            CancellationToken.None
        );

        updatedAttribute.Should().NotBeNull();

        AttributeConfigurationViewModel attribute = await _eavService.GetAttribute(
            updatedAttribute.Id,
            CancellationToken.None
        );

        attribute.Metadata.Should().Be(priceAttribute.Metadata);

        // check projections
        ProjectionQueryResult<AttributeConfigurationListItemViewModel> attributesList =
            await _eavService.ListAttributes(new ProjectionQuery());
        attributesList.Records.First().Document!.Metadata.Should().Be(priceAttribute.Metadata);
    }

    private IProjectionRepository<ProjectionRebuildState> GetProjectionRebuildStateRepository()
    {
        return new InMemoryProjectionRepository<ProjectionRebuildState>();
    }

    private IEventsObserver GetEventStoreEventsObserver()
    {
        return new PostgresqlEventStoreEventObserver((PostgresqlEventStore)_eventStore);
    }
}
