using System.Globalization;
using System.Reflection;
using System.Text.Json;

using AutoMapper;

using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.Attributes;
using CloudFabric.EAV.Models.ViewModels.EAV;
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

namespace CloudFabric.EAV.Tests;

[TestClass]
public class Tests
{
    private AggregateRepositoryFactory _aggregateRepositoryFactory;
    private PostgresqlProjectionRepositoryFactory _projectionRepositoryFactory;

    private EAVService _eavService;

    private IEventStore _eventStore;
    private ILogger<EAVService> _logger;

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
        var mapper = configuration.CreateMapper();

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
            mapper,
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
            var entityConfigurationProjectionRepository = _projectionRepositoryFactory
                .GetProjectionRepository<EntityConfigurationProjectionDocument>();

            var attributeConfigurationProjectionRepository = _projectionRepositoryFactory
                .GetProjectionRepository<AttributeConfigurationProjectionDocument>();

            await entityConfigurationProjectionRepository.DeleteAll();
            await attributeConfigurationProjectionRepository.DeleteAll();

            var rebuildStateRepository = GetProjectionRebuildStateRepository();
            await rebuildStateRepository.DeleteAll();
        }
        catch
        {
        }
    }

    [TestMethod]
    public async Task CreateInstance_Success()
    {
        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        var configuration =
            await _eavService.GetEntityConfiguration(createdConfiguration.Id, createdConfiguration.PartitionKey);

        var entityInstanceCreateRequest =
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
        var entityInstanceCreateRequest =
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
        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        var configuration =
            await _eavService.GetEntityConfiguration(createdConfiguration.Id, createdConfiguration.PartitionKey);
        var requiredAttributeMachineName = "players_min";
        var entityInstanceCreateRequest =
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
    public async Task CreateEntityConfiguration_Success()
    {
        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );
        createdConfiguration.Should().NotBeNull();
        createdConfiguration.Id.Should().NotBeEmpty();
        foreach (var name in createdConfiguration.Name)
        {
            name.String.Should().Be(configurationCreateRequest.Name.First(n => n.CultureInfoId == name.CultureInfoId)
                .String
            );
        }

        createdConfiguration.MachineName.Should().Be(configurationCreateRequest.MachineName);
        createdConfiguration.Attributes.Count.Should().Be(configurationCreateRequest.Attributes.Count);
    }

    [TestMethod]
    public async Task CreateEntityConfiguration_AttributesMachineNamesAreNotUnique()
    {
        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        configurationCreateRequest.Attributes.Add(
            new TextAttributeConfigurationCreateUpdateRequest
            {
                Name = new List<LocalizedStringCreateRequest>
                {
                    new LocalizedStringCreateRequest
                    {
                        CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                        String = "test"
                    }
                },
                MachineName = (configurationCreateRequest.Attributes[0] as AttributeConfigurationCreateUpdateRequest)!.MachineName,
                DefaultValue = "test"
            }
        );

        (EntityConfigurationViewModel? entityConfig, ProblemDetails? error) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        entityConfig.Should().BeNull();
        error.Should().NotBeNull();
        error.Should().BeOfType<ValidationErrorResponse>();
        error.As<ValidationErrorResponse>().Errors.Should().Contain(x => x.Value.Contains("Attributes machine name must be unique"));
    }

    [TestMethod]
    public async Task GetEntityConfiguration_Success()
    {
        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        var configuration = await _eavService.GetEntityConfiguration(
            createdConfiguration.Id,
            createdConfiguration.PartitionKey
        );

        configuration.Should().BeEquivalentTo(createdConfiguration);
    }

    [TestMethod]
    public async Task UpdateAttribute_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var numberAttribute = new NumberAttributeConfigurationCreateUpdateRequest()
        {
            MachineName = "number_attribute",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new LocalizedStringCreateRequest
                    {
                        CultureInfoId = cultureInfoId,
                        String = "Number attribute description"
                    }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "New Number Attribute"
                }
            },
            DefaultValue = 15,
            IsRequired = true,
            MaximumValue = 100,
            MinimumValue = -100
        };

        var configCreateRequest = new EntityConfigurationCreateRequest()
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "test"
                }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>
            {
                numberAttribute
            }
        };

        (EntityConfigurationViewModel? created, _) = await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);
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
            created.Attributes[0].AttributeConfigurationId.ToString(),
            CancellationToken.None
        );

        updatedAttribute.Name[0].String.Should().Be(numberAttribute.Name[0].String);
        updatedAttribute.IsRequired.Should().Be(numberAttribute.IsRequired);
        updatedAttribute.As<NumberAttributeConfigurationViewModel>().MaximumValue.Should().Be(numberAttribute.MaximumValue);
        updatedAttribute.As<NumberAttributeConfigurationViewModel>().MinimumValue.Should().Be(numberAttribute.MinimumValue);
    }

    [TestMethod]
    public async Task DeleteAttribute_Success()
    {
        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel entityConfig, ProblemDetails? _) = await _eavService.CreateEntityConfiguration(configurationCreateRequest, CancellationToken.None);

        Guid attributeToDelete = entityConfig.Attributes.Select(x => x.AttributeConfigurationId).FirstOrDefault();

        await _eavService.DeleteAttributes(new List<Guid> { attributeToDelete }, CancellationToken.None);

        var entityConfAfterAttributeDeleted = await _eavService.GetEntityConfiguration(entityConfig.Id, entityConfig.Id.ToString());
        entityConfAfterAttributeDeleted.Attributes.Count().Should().Be(entityConfig.Attributes.Count() - 1);

        Func<Task> act = async () => await _eavService.GetAttribute(attributeToDelete, attributeToDelete.ToString());
        await act.Should().ThrowAsync<NotFoundException>();

        ProjectionQueryResult<AttributeConfigurationListItemViewModel> attributesProjections = await _eavService.ListAttributes(new ProjectionQuery
        {
            Filters = new List<Filter>
                {
                    new Filter
                    {
                        PropertyName = nameof(AttributeConfigurationProjectionDocument.Id),
                        Operator = FilterOperator.Equal,
                        Value = attributeToDelete
                    }
                }
        });
        attributesProjections.Records.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task DeleteEntityAttributeFromEntity_EntityNotFound()
    {
        Func<Task> act = async () => await _eavService.DeleteAttributesFromEntityConfiguration(new List<Guid> { Guid.NewGuid() }, Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task DeleteEntityAttributeFromEntity_DeleteNotExistingAttribute()
    {
        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel entityConfig, ProblemDetails? _) = await _eavService.CreateEntityConfiguration(configurationCreateRequest, CancellationToken.None);

        await _eavService.DeleteAttributesFromEntityConfiguration(new List<Guid> { Guid.NewGuid() }, entityConfig.Id, CancellationToken.None);
        var entityConfigAfterDeletingNotExistingAttribute = await _eavService.GetEntityConfiguration(entityConfig.Id, entityConfig.Id.ToString());
        entityConfigAfterDeletingNotExistingAttribute.Attributes.Count.Should().Be(entityConfig.Attributes.Count);
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

        var configRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        string newAttributeMachineName = (configRequest.Attributes[0] as AttributeConfigurationCreateUpdateRequest)!.MachineName!;

        (EntityConfigurationViewModel? createdConfig, _) = await _eavService.CreateEntityConfiguration(configRequest, CancellationToken.None);

        var newAttributeRequest = new NumberAttributeConfigurationCreateUpdateRequest()
        {
            DefaultValue = 4,
            IsRequired = true,
            Name = new List<LocalizedStringCreateRequest>()
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureId,
                    String = "Average Time"
                }
            },
            MachineName = newAttributeMachineName,
            MinimumValue = 1,
            Description = new List<LocalizedStringCreateRequest>()
        };

        configRequest.Attributes.Add(newAttributeRequest);

        var updateRequest = new EntityConfigurationUpdateRequest()
        {
            Attributes = configRequest.Attributes,
            Id = createdConfig.Id,
            Name = configRequest.Name
        };

        (EntityConfigurationViewModel? entityConfig, ProblemDetails? error) = await _eavService.UpdateEntityConfiguration(updateRequest, CancellationToken.None);
        entityConfig.Should().BeNull();
        error.Should().NotBeNull();
        error.Should().BeOfType<ValidationErrorResponse>();
        error.As<ValidationErrorResponse>().Errors.Should().Contain(x => x.Value.Contains("Attributes machine name must be unique"));
    }

    [TestMethod]
    public async Task UpdateEntityConfiguration_AddedNewAttribute_Success()
    {
        var cultureId = CultureInfo.GetCultureInfo("EN-us").LCID;

        var configRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfig, _) = await _eavService.CreateEntityConfiguration(configRequest, CancellationToken.None);
        const string newAttributeMachineName = "avg_time_mins";

        var newAttributeRequest = new NumberAttributeConfigurationCreateUpdateRequest()
        {
            DefaultValue = 4,
            IsRequired = true,
            Name = new List<LocalizedStringCreateRequest>()
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureId,
                    String = "Average Time"
                }
            },
            MachineName = newAttributeMachineName,
            MinimumValue = 1,
            Description = new List<LocalizedStringCreateRequest>()
        };

        configRequest.Attributes.Add(newAttributeRequest);

        var updateRequest = new EntityConfigurationUpdateRequest()
        {
            Attributes = configRequest.Attributes,
            Id = createdConfig.Id,
            Name = configRequest.Name
        };
        var updatedConfig = await _eavService.UpdateEntityConfiguration(updateRequest, CancellationToken.None);
        //var newAttrIndex = updatedConfig.Attributes.FindIndex(a => a.MachineName == newAttributeMachineName);
        //newAttrIndex.Should().BePositive();
        //var newAttribute = updatedConfig.Attributes[newAttrIndex];
        //newAttribute.Should().NotBeNull();
        //newAttribute.Should().BeEquivalentTo(newAttributeRequest, opt => opt.ComparingRecordsByValue());
    }

    [TestMethod]
    public async Task UpdateEntityConfiguration_ChangeName_Success()
    {
        var cultureId = CultureInfo.GetCultureInfo("EN-us").LCID;
        const string newName = "newName";
        var configRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfig, _) = await _eavService.CreateEntityConfiguration(configRequest, CancellationToken.None);
        var newNameRequest = new List<LocalizedStringCreateRequest>()
        {
            new LocalizedStringCreateRequest
            {
                CultureInfoId = cultureId,
                String = newName
            }
        };
        configRequest.Name = newNameRequest;
        var updateRequest = new EntityConfigurationUpdateRequest()
        {
            Attributes = configRequest.Attributes,
            Id = createdConfig.Id,
            Name = configRequest.Name
        };
        (EntityConfigurationViewModel? updatedConfig, _) = await _eavService.UpdateEntityConfiguration(updateRequest, CancellationToken.None);
        updatedConfig.Name.First(n => n.CultureInfoId == cultureId).String.Should().Be(newName);
        updatedConfig.Should().BeEquivalentTo(createdConfig, opt => opt.Excluding(o => o.Name));
    }

    [TestMethod]
    public async Task TestEntityConfigurationProjectionCreated()
    {
        var configurationItemsStart = await _eavService.ListEntityConfigurations(
            ProjectionQuery.Where<EntityConfigurationProjectionDocument>(x => x.MachineName == "BoardGame"),
            null,
            CancellationToken.None
        );

        configurationItemsStart.Records.Count.Should().Be(0);

        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        var createdConfiguration = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        // verify projection is created
        var configurationItems = await _eavService.ListEntityConfigurations(
            ProjectionQuery.Where<EntityConfigurationProjectionDocument>(x => x.MachineName == "BoardGame")
        );

        configurationItems.Records.Count.Should().Be(1);
    }

    [TestMethod]
    public async Task GetEntityConfigurationProjectionByTenantId_Success()
    {
        var configurationCreateRequest1 = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        var configurationCreateRequest2 = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdConfiguration1, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest1,
            CancellationToken.None
        );

        (EntityConfigurationViewModel? createdConfiguration2, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest2,
            CancellationToken.None
        );

        // verify projection is created
        var configurationItems = await _eavService.ListEntityConfigurations(
            ProjectionQuery.Where<EntityConfigurationProjectionDocument>(x => x.TenantId == createdConfiguration2.TenantId)
        );

        configurationItems.Records.Count.Should().Be(1);
        configurationItems.Records[0].Document?.TenantId.Should().Be(createdConfiguration2.TenantId);
    }

    [TestMethod]
    public async Task TestCreateNumberAttribute_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var numberAttribute = new NumberAttributeConfigurationCreateUpdateRequest()
        {
            MachineName = "testAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new LocalizedStringCreateRequest
                    {
                        CultureInfoId = cultureInfoId,
                        String = "testAttrDesc"
                    }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "testAttrName"
                }
            },
            DefaultValue = 15,
            IsRequired = true,
            MaximumValue = 100,
            MinimumValue = -100
        };

        var configCreateRequest = new EntityConfigurationCreateRequest()
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "test"
                }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>
            {
                numberAttribute
            }
        };

        (EntityConfigurationViewModel? created, _) = await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);
        created.Attributes.Count.Should().Be(1);

        var allAttributes = await _eavService.ListAttributes(new ProjectionQuery()
        {
            Limit = 100
        });

        allAttributes.Records.First().As<QueryResultDocument<AttributeConfigurationListItemViewModel>>()
            .Document?.Name.Should().BeEquivalentTo(numberAttribute.Name);
    }

    [TestMethod]
    public async Task TestGetNumberAttribute_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var numberAttribute = new NumberAttributeConfigurationCreateUpdateRequest()
        {
            MachineName = "testAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new LocalizedStringCreateRequest
                    {
                        CultureInfoId = cultureInfoId,
                        String = "testAttrDesc"
                    }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "testAttrName"
                }
            },
            DefaultValue = 15,
            IsRequired = true,
            MaximumValue = 100,
            MinimumValue = -100
        };

        var configCreateRequest = new EntityConfigurationCreateRequest()
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "test"
                }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>
            {
                numberAttribute
            }
        };

        (EntityConfigurationViewModel? created, _) = await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);
        created.Attributes.Count.Should().Be(1);

        var createdAttribute = await _eavService.GetAttribute(created.Attributes[0].AttributeConfigurationId,
            created.Attributes[0].AttributeConfigurationId.ToString());

        createdAttribute.MachineName.Should().Be("testAttr");
    }

    [TestMethod]
    public async Task TestCreateValueFromListAttribute_Success()
    {
        var valueFromListAttributeRepository = _aggregateRepositoryFactory.GetAggregateRepository<ValueFromListAttributeConfiguration>();

        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var valueFromListAttribute = new ValueFromListAttributeConfigurationCreateUpdateRequest()
        {
            MachineName = "testValueAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new LocalizedStringCreateRequest
                    {
                        CultureInfoId = cultureInfoId,
                        String = "ValueAttributeDescription"
                    }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "testValueAttributeName"
                }
            },
            IsRequired = true,
            ValueFromListAttributeType = ValueFromListAttributeType.OneValueFromList,
            ValuesList = new List<ValueFromListOptionCreateUpdateRequest>
            {
                new ValueFromListOptionCreateUpdateRequest("firstTestOption", "Premium wrap", 30),
                new ValueFromListOptionCreateUpdateRequest("secondTestOption", "Card with wishes from shop", null)
            }
        };

        var entityConfigurationCreateRequest = new EntityConfigurationCreateRequest()
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "test"
                }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>
            {
                valueFromListAttribute
            }
        };

        (EntityConfigurationViewModel? created, _) = await _eavService.CreateEntityConfiguration(entityConfigurationCreateRequest, CancellationToken.None);
        created!.Attributes.Count.Should().Be(1);

        var allAttributes = await _eavService.ListAttributes(new ProjectionQuery()
        {
            Limit = 100
        });

        allAttributes.Records.First().As<QueryResultDocument<AttributeConfigurationListItemViewModel>>()
            .Document?.Name.Should().BeEquivalentTo(valueFromListAttribute.Name);

        var valuesAttribute = await valueFromListAttributeRepository.LoadAsync(
            allAttributes.Records.First().Document!.Id!.Value,
            allAttributes.Records.First().Document!.Id.ToString()!,
            CancellationToken.None
        );
        valuesAttribute!.ValuesList.Count.Should().Be(2);
    }

    [TestMethod]
    public async Task TestCreateValueFromListAttribute_OptionNamesNotUnique()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var valueFromListAttribute = new ValueFromListAttributeConfigurationCreateUpdateRequest()
        {
            MachineName = "testValueAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new LocalizedStringCreateRequest
                    {
                        CultureInfoId = cultureInfoId,
                        String = "ValueAttributeDescription"
                    }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "testValueAttributeName"
                }
            },
            IsRequired = true,
            ValueFromListAttributeType = ValueFromListAttributeType.OneValueFromList,
            ValuesList = new List<ValueFromListOptionCreateUpdateRequest>
            {
                new ValueFromListOptionCreateUpdateRequest("repeatedMachineName", "Premium wrap", 30),
                new ValueFromListOptionCreateUpdateRequest("repeatedMachineName", "Card with wishes from shop", null)
            }
        };

        var entityConfigurationCreateRequest = new EntityConfigurationCreateRequest()
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "test"
                }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>
            {
                valueFromListAttribute
            }
        };

        Func<Task> action = async () => await _eavService.CreateEntityConfiguration(entityConfigurationCreateRequest, CancellationToken.None);
        await action.Should().ThrowAsync<Exception>();

        valueFromListAttribute.ValuesList = new List<ValueFromListOptionCreateUpdateRequest>
        {
                new ValueFromListOptionCreateUpdateRequest(name: "Repeated Name", machineName: "firstTestOption", valueToAppend: 30),
                new ValueFromListOptionCreateUpdateRequest(name: "Repeated Name", machineName: "secondTestOption", valueToAppend: null)
        };

        await action.Should().ThrowAsync<Exception>();
    }

    [TestMethod]
    public async Task TestUpdateValueFromListAttribute_Success()
    {
        var valueFromListRepository = _aggregateRepositoryFactory.GetAggregateRepository<ValueFromListAttributeConfiguration>();

        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var valueFromListAttributeCreateRequest = new ValueFromListAttributeConfigurationCreateUpdateRequest()
        {
            MachineName = "testValueAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new LocalizedStringCreateRequest
                    {
                        CultureInfoId = cultureInfoId,
                        String = "ValueAttributeDescription"
                    }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "testValueAttributeName"
                }
            },
            IsRequired = true,
            ValueFromListAttributeType = ValueFromListAttributeType.OneValueFromList,
            ValuesList = new List<ValueFromListOptionCreateUpdateRequest>
            {
                new ValueFromListOptionCreateUpdateRequest("firstTestOption", "Premium wrap", 30),
                new ValueFromListOptionCreateUpdateRequest("secondTestOption", "Card with wishes from shop", null)
            }
        };

        var valueFromListAttribute = await _eavService.CreateAttribute(valueFromListAttributeCreateRequest, CancellationToken.None);

        // create request with changed properties and update attribute
        string affectedMachineName = Guid.NewGuid().ToString();
        valueFromListAttributeCreateRequest.AttributeMachineNameToAffect = affectedMachineName;
        valueFromListAttributeCreateRequest.ValuesList = new()
        {
            new ValueFromListOptionCreateUpdateRequest("changedAttribute", "Card with wishes from shop", null)
        };
        valueFromListAttributeCreateRequest.ValueFromListAttributeType = ValueFromListAttributeType.MultipleValuesFromList;

        (AttributeConfigurationViewModel? changedAttribute, _) = await _eavService.UpdateAttribute(valueFromListAttribute.Id, valueFromListAttributeCreateRequest!, CancellationToken.None);

        var changedValueFromListAttribute = await valueFromListRepository.LoadAsync(changedAttribute!.Id, changedAttribute.Id.ToString(), CancellationToken.None);
        changedValueFromListAttribute!.AttributeMachineNameToAffect.Should().Be(affectedMachineName);
        changedValueFromListAttribute.ValueFromListAttributeType.Should().Be(ValueFromListAttributeType.MultipleValuesFromList);
        changedValueFromListAttribute.ValuesList.Count.Should().Be(1);
        changedValueFromListAttribute.ValuesList.FirstOrDefault()!.MachineName.Should().Be("changedAttribute");
    }

    [TestMethod]
    public async Task TestCreateEntityInstanceWithValueFromListAttribute_ValidationError()
    {
        // create entity configuration with value from list attribute
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var valueFromListAttribute = new ValueFromListAttributeConfigurationCreateUpdateRequest()
        {
            MachineName = "testValueAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new LocalizedStringCreateRequest
                    {
                        CultureInfoId = cultureInfoId,
                        String = "ValueAttributeDescription"
                    }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "testValueAttributeName"
                }
            },
            IsRequired = true,
            ValueFromListAttributeType = ValueFromListAttributeType.OneValueFromList,
            ValuesList = new List<ValueFromListOptionCreateUpdateRequest>
            {
                new ValueFromListOptionCreateUpdateRequest("firstTestOption", "Premium wrap", 30)
            }
        };

        var entityConfigurationCreateRequest = new EntityConfigurationCreateRequest()
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "test"
                }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>
            {
                valueFromListAttribute
            }
        };

        (EntityConfigurationViewModel? entityConfiguration, _) = await _eavService.CreateEntityConfiguration(entityConfigurationCreateRequest, CancellationToken.None);

        // create entity instance using wrong type of attribute
        (EntityInstanceViewModel result, ProblemDetails validationErrors) = await _eavService.CreateEntityInstance(new EntityInstanceCreateRequest()
        {
            EntityConfigurationId = entityConfiguration.Id,
            Attributes = new List<AttributeInstanceCreateUpdateRequest>()
            {
                new NumberAttributeInstanceCreateUpdateRequest()
                {
                    ConfigurationAttributeMachineName = "testValueAttr",
                    Value = int.MaxValue
                }
            }
        });

        result.Should().BeNull();
        validationErrors.Should().BeOfType<ValidationErrorResponse>();
        validationErrors.As<ValidationErrorResponse>().Errors["testValueAttr"].First().Should()
            .Be("Cannot validate attribute. Expected attribute type: Value from list");
    }

    [TestMethod]
    public async Task AddAttributeToEntityConfiguration_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;

        var configCreateRequest = new EntityConfigurationCreateRequest()
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "test"
                }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>()
        };

        (EntityConfigurationViewModel? createdEntityConfiguration, _) = await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);

        var numberAttribute = new NumberAttributeConfigurationCreateUpdateRequest()
        {
            MachineName = "testAttr",
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new LocalizedStringCreateRequest
                    {
                        CultureInfoId = cultureInfoId,
                        String = "testAttrDesc"
                    }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "testAttrName"
                }
            },
            DefaultValue = 15,
            IsRequired = true,
            MaximumValue = 100,
            MinimumValue = -100
        };

        var createdAttribute = await _eavService.CreateAttribute(numberAttribute, CancellationToken.None);
        createdAttribute.Should().NotBeNull();

        await _eavService.AddAttributeToEntityConfiguration(
            createdAttribute.Id,
            createdEntityConfiguration.Id,
            CancellationToken.None
        );

        // check that attribute is added
        var updatedEntityConfiguration = await _eavService.GetEntityConfiguration(
            createdEntityConfiguration.Id,
            createdEntityConfiguration.Id.ToString()
        );

        updatedEntityConfiguration.Attributes.Any(x => x.AttributeConfigurationId == createdAttribute.Id)
            .Should()
            .BeTrue();
    }

    [TestMethod]
    public async Task AddAttributeToEntityConfiguration_MachineNamesAreNotUnique()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;

        var configCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        string newAttributeMachineName = (configCreateRequest.Attributes[0] as AttributeConfigurationCreateUpdateRequest)!.MachineName!;

        (EntityConfigurationViewModel? createdEntityConfiguration, _) = await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);

        var numberAttribute = new NumberAttributeConfigurationCreateUpdateRequest()
        {
            MachineName = newAttributeMachineName,
            Description =
                new List<LocalizedStringCreateRequest>
                {
                    new LocalizedStringCreateRequest
                    {
                        CultureInfoId = cultureInfoId,
                        String = "testAttrDesc"
                    }
                },
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "testAttrName"
                }
            },
            DefaultValue = 15,
            IsRequired = true,
            MaximumValue = 100,
            MinimumValue = -100
        };

        var createdAttribute = await _eavService.CreateAttribute(numberAttribute, CancellationToken.None);
        createdAttribute.Should().NotBeNull();

        (EntityConfigurationViewModel? entityConfig, ProblemDetails? error) = await _eavService.AddAttributeToEntityConfiguration(
            createdAttribute.Id,
            createdEntityConfiguration.Id,
            CancellationToken.None
        );

        entityConfig.Should().BeNull();
        error.Should().NotBeNull();
        error.Should().BeOfType<ValidationErrorResponse>();
        error.As<ValidationErrorResponse>().Errors.Should().Contain(x => x.Value.Contains("Attributes machine name must be unique"));
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
            ConfigurationAttributeMachineName = changedAttributeName,
            Value = 10
        };
        var updateRequest = new EntityInstanceUpdateRequest
        {
            EntityConfigurationId = createdConfiguration.Id,
            Attributes = attributesRequest,
            Id = createdInstance.Id
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

        var playerMaxIndex = attributesRequest.FindIndex(a => a.ConfigurationAttributeMachineName == changedAttributeName);
        attributesRequest[playerMaxIndex] = new NumberAttributeInstanceCreateUpdateRequest
        {
            ConfigurationAttributeMachineName = changedAttributeName,
            Value = 20
        };
        var updateRequest = new EntityInstanceUpdateRequest
        {
            EntityConfigurationId = createdConfiguration.Id,
            Attributes = attributesRequest,
            Id = createdInstance.Id
        };

        (EntityInstanceViewModel updatedInstance, ProblemDetails validationErrors) = await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(), updateRequest, CancellationToken.None);
        updatedInstance.Should().BeNull();
        validationErrors.As<ValidationErrorResponse>().Errors.Should().ContainKey(changedAttributeName);
    }

    [TestMethod]
    public async Task UpdateInstance_AddAttribute_Success()
    {
        const string changedAttributeName = "avg_time_mins";

        EntityConfigurationCreateRequest configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest = EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        List<AttributeInstanceCreateUpdateRequest> attributesRequest = entityInstanceCreateRequest.Attributes;
        (EntityInstanceViewModel createdInstance, _) = await _eavService.CreateEntityInstance(entityInstanceCreateRequest);

        attributesRequest.Add(new NumberAttributeInstanceCreateUpdateRequest
        {
            ConfigurationAttributeMachineName = changedAttributeName,
            Value = 30
        });

        var updateRequest = new EntityInstanceUpdateRequest
        {
            EntityConfigurationId = createdConfiguration.Id,
            Attributes = attributesRequest,
            Id = createdInstance.Id
        };

        (EntityInstanceViewModel updatedInstance, _) = await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(), updateRequest, CancellationToken.None);
        updatedInstance.Attributes.First(a => a.ConfigurationAttributeMachineName == changedAttributeName).As<NumberAttributeInstanceViewModel>().Value.Should().Be(30);
    }

    [TestMethod]
    public async Task CreateInstance_NumberOfItemsWithAttributeUpdated_Success()
    {
        const string changedAttributeName = "avg_time_mins";

        EntityConfigurationCreateRequest configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest = EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        List<AttributeInstanceCreateUpdateRequest> attributesRequest = entityInstanceCreateRequest.Attributes;
        (EntityInstanceViewModel createdInstance, _) = await _eavService.CreateEntityInstance(entityInstanceCreateRequest);

        attributesRequest.Add(new NumberAttributeInstanceCreateUpdateRequest
        {
            ConfigurationAttributeMachineName = changedAttributeName,
            Value = 30
        });

        var updateRequest = new EntityInstanceUpdateRequest
        {
            EntityConfigurationId = createdConfiguration.Id,
            Attributes = attributesRequest,
            Id = createdInstance.Id
        };

        (EntityInstanceViewModel updatedInstance, _) = await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(), updateRequest, CancellationToken.None);

        ProjectionQueryResult<AttributeConfigurationListItemViewModel> attributeConfigurations = await _eavService.ListAttributes(
            new ProjectionQuery
            {
                Filters = new List<Filter>
                {
                    new Filter
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

        EntityConfigurationCreateRequest configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        var numberAttributeConfig = configurationCreateRequest.Attributes
            .First(x => (x is NumberAttributeConfigurationCreateUpdateRequest) && ((NumberAttributeConfigurationCreateUpdateRequest)x).MachineName == changedAttributeName);

        (numberAttributeConfig as NumberAttributeConfigurationCreateUpdateRequest)!.NumberType = NumberAttributeType.Integer;

        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest = EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);
        entityInstanceCreateRequest.Attributes.RemoveAll(a =>
            a.ConfigurationAttributeMachineName == changedAttributeName
        );
        entityInstanceCreateRequest.Attributes.Add(new NumberAttributeInstanceCreateUpdateRequest
        {
            ConfigurationAttributeMachineName = changedAttributeName,
            Value = 30.55M
        });

        (EntityInstanceViewModel instance, ProblemDetails error) = await _eavService.CreateEntityInstance(entityInstanceCreateRequest);
        instance.Should().BeNull();
        error.As<ValidationErrorResponse>().Errors.Should().Contain(x => x.Value.Contains("Value is not an integer value"));
    }

    [TestMethod]
    public async Task UpdateInstance_AddAttribute_IgnoreAttributeNotInConfig()
    {
        const string changedAttributeName = "min_time_mins";

        EntityConfigurationCreateRequest configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest = EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        List<AttributeInstanceCreateUpdateRequest> attributesRequest = entityInstanceCreateRequest.Attributes;
        (EntityInstanceViewModel createdInstance, _) = await _eavService.CreateEntityInstance(entityInstanceCreateRequest);

        attributesRequest.Add(new NumberAttributeInstanceCreateUpdateRequest
        {
            ConfigurationAttributeMachineName = changedAttributeName,
            Value = 30
        });

        var updateRequest = new EntityInstanceUpdateRequest
        {
            EntityConfigurationId = createdConfiguration.Id,
            Attributes = attributesRequest,
            Id = createdInstance.Id
        };

        (EntityInstanceViewModel updatedInstance, _) = await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(), updateRequest, CancellationToken.None);
        updatedInstance.Attributes.FirstOrDefault(a => a.ConfigurationAttributeMachineName == changedAttributeName).Should().BeNull();
    }

    [TestMethod]
    public async Task UpdateInstance_RemoveAttribute_Success()
    {
        const string changedAttributeName = "description";

        EntityConfigurationCreateRequest configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest = EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        List<AttributeInstanceCreateUpdateRequest> attributesRequest = entityInstanceCreateRequest.Attributes;
        (EntityInstanceViewModel createdInstance, _) = await _eavService.CreateEntityInstance(entityInstanceCreateRequest);

        attributesRequest = attributesRequest.Where(a => a.ConfigurationAttributeMachineName != changedAttributeName).ToList();
        var updateRequest = new EntityInstanceUpdateRequest
        {
            EntityConfigurationId = createdConfiguration.Id,
            Attributes = attributesRequest,
            Id = createdInstance.Id
        };

        (EntityInstanceViewModel updatedInstance, _) = await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(), updateRequest, CancellationToken.None);
        updatedInstance.Attributes.FirstOrDefault(a => a.ConfigurationAttributeMachineName == changedAttributeName).Should().BeNull();
    }

    [TestMethod]
    public async Task UpdateInstance_RemoveAttribute_FailValidation()
    {
        const string changedAttributeName = "players_max";

        EntityConfigurationCreateRequest configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest = EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        List<AttributeInstanceCreateUpdateRequest> attributesRequest = entityInstanceCreateRequest.Attributes;
        (EntityInstanceViewModel createdInstance, _) = await _eavService.CreateEntityInstance(entityInstanceCreateRequest);

        attributesRequest = attributesRequest.Where(a => a.ConfigurationAttributeMachineName != changedAttributeName).ToList();
        var updateRequest = new EntityInstanceUpdateRequest
        {
            EntityConfigurationId = createdConfiguration.Id,
            Attributes = attributesRequest,
            Id = createdInstance.Id
        };

        (EntityInstanceViewModel updatedInstance, ProblemDetails errors) = await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(), updateRequest, CancellationToken.None);
        updatedInstance.Should().BeNull();
        errors.As<ValidationErrorResponse>().Errors.Should().ContainKey(changedAttributeName);
    }

    [TestMethod]
    public async Task TestCreateNumberAttributeAsReference_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;
        var priceAttribute = new NumberAttributeConfigurationCreateUpdateRequest()
        {
            MachineName = "price",
            Description = new List<LocalizedStringCreateRequest>()
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "Product Price"
                }
            },
            Name = new List<LocalizedStringCreateRequest>()
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "Price"
                }
            },
            DefaultValue = 0,
            IsRequired = true,
            MaximumValue = -1,
            MinimumValue = 0
        };

        var priceAttributeCreated = await _eavService.CreateAttribute(priceAttribute, CancellationToken.None);

        var entityConfigurationCreateRequest = new EntityConfigurationCreateRequest()
        {
            MachineName = "product",
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "Product"
                }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>()
            {
                new EntityAttributeConfigurationCreateUpdateReferenceRequest()
                {
                    AttributeConfigurationId = priceAttributeCreated.Id
                },
                new TextAttributeConfigurationCreateUpdateRequest()
                {
                    MachineName = "additional_notes",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest
                        {
                            CultureInfoId = cultureInfoId,
                            String = "Additional Notes"
                        }
                    },
                    IsRequired = false,
                    DefaultValue = ""
                }
            }
        };

        var entityConfigurationCreated = await _eavService.CreateEntityConfiguration(
            entityConfigurationCreateRequest,
            CancellationToken.None
        );

        var allAttributes = await _eavService.ListAttributes(new ProjectionQuery()
        {
            Limit = 1000
        });
        allAttributes.Records.Count.Should().Be(2);
    }

    [TestMethod]
    public async Task TestCreateInstanceAndQuery()
    {
        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        var (createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        var configuration = await _eavService.GetEntityConfiguration(
            createdConfiguration.Id,
            createdConfiguration.PartitionKey
        );

        //configuration.Should().BeEquivalentTo(createdConfiguration);

        var instanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        var (createdInstance, createProblemDetails) = await _eavService.CreateEntityInstance(instanceCreateRequest);

        createdInstance.Should().BeEquivalentTo(instanceCreateRequest);

        var query = new ProjectionQuery()
        {
            Filters = new List<Filter>() { { new Filter("Id", FilterOperator.Equal, createdInstance.Id) } }
        };

        await _eavService
            .QueryInstances(createdConfiguration.Id, query);
    }

    [TestMethod]
    public async Task TestSimpleJsonConverter()
    {
        var jsonString =
            "{\"valueType\" : 2, \"machineName\" : \"test\", \"name\" : [{\"string\" : \"Test\", \"cultureInfoId\" : 1033}], \"description\" : [{\"string\" : \"Test\", \"cultureInfoId\" : 1033}], \"defaultValue\" : 0, \"isRequired\" : true, \"maximumValue\" : 10, \"minimumValue\" : 0 }";
        var deserializeOptions = new JsonSerializerOptions();
        deserializeOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        AttributeConfigurationCreateUpdateRequest attribute = JsonSerializer.Deserialize<AttributeConfigurationCreateUpdateRequest>(jsonString, deserializeOptions)!;
        attribute.As<NumberAttributeConfigurationCreateUpdateRequest>().ValueType.Should().Be(EavAttributeType.Number);
        attribute.MachineName.Should().Be("test");
    }

    [TestMethod]
    public async Task TestSimpleToPolymorphJsonConverter()
    {
        var jsonString =
            "{\"typeName\": \"CloudFabric.EAV.Models.RequestModels.Attributes.NumberAttributeConfigurationCreateUpdateRequest\", \"typeValue\": { \"valueType\" : 2, \"machineName\" : \"test\", \"name\" : [{\"string\" : \"Test\", \"cultureInfoId\" : 1033}], \"description\" : [{\"string\" : \"Test\", \"cultureInfoId\" : 1033}], \"defaultValue\" : 0, \"isRequired\" : true, \"maximumValue\" : -1, \"minimumValue\" : 0 }}";
        var deserializeOptions = new JsonSerializerOptions();
        AttributeConfigurationCreateUpdateRequest attribute = JsonSerializer.Deserialize<AttributeConfigurationCreateUpdateRequest>(jsonString, deserializeOptions)!;
        attribute.As<NumberAttributeConfigurationCreateUpdateRequest>().ValueType.Should().Be(EavAttributeType.Number);
        attribute.MachineName.Should().Be("test");
    }

    [TestMethod]
    public async Task CreateReadonlyAttributeConfiguration_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;

        var configCreateRequest = new EntityConfigurationCreateRequest()
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "test"
                }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>()
            {
                new NumberAttributeConfigurationCreateUpdateRequest()
                {
                    MachineName = "testAttr",
                    Description =
                        new List<LocalizedStringCreateRequest>
                        {
                            new LocalizedStringCreateRequest
                            {
                                CultureInfoId = cultureInfoId,
                                String = "testAttrDesc"
                            }
                        },
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new LocalizedStringCreateRequest
                        {
                            CultureInfoId = cultureInfoId,
                            String = "testAttrName"
                        }
                    },
                    DefaultValue = 15,
                    IsRequired = true,
                    IsReadOnly = true
                }
            }
        };

        (EntityConfigurationViewModel? createdEntityConfiguration, _) = await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);

        AttributeConfigurationViewModel attribute = await _eavService.GetAttribute(
            createdEntityConfiguration.Attributes[0].AttributeConfigurationId,
            createdEntityConfiguration.Attributes[0].AttributeConfigurationId.ToString(),
            CancellationToken.None
        );

        attribute.IsReadOnly.Should().BeTrue();

        // check projections
        var attributes = await _eavService.ListAttributes(
            new ProjectionQuery
            {
                Filters = new List<Filter>
                {
                    new Filter(nameof(AttributeConfigurationProjectionDocument.Id), FilterOperator.Equal, attribute.Id)
                },
                Limit = 1
            }
        );

        attributes.Records.First().Document.IsReadOnly.Should().BeTrue();
    }

    [TestMethod]
    public async Task UpdateReadonlyAttributeConfiguration_AttributeCannotBeUpdated()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;

        var configCreateRequest = new EntityConfigurationCreateRequest()
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "test"
                }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>()
            {
                new NumberAttributeConfigurationCreateUpdateRequest()
                {
                    MachineName = "testAttr",
                    Description =
                        new List<LocalizedStringCreateRequest>
                        {
                            new LocalizedStringCreateRequest
                            {
                                CultureInfoId = cultureInfoId,
                                String = "testAttrDesc"
                            }
                        },
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new LocalizedStringCreateRequest
                        {
                            CultureInfoId = cultureInfoId,
                            String = "testAttrName"
                        }
                    },
                    DefaultValue = 15,
                    MinimumValue = 0,
                    MaximumValue = 100,
                    IsRequired = true,
                    IsReadOnly = true
                }
            }
        };

        (EntityConfigurationViewModel? createdEntityConfiguration, _) = await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);

        (AttributeConfigurationViewModel? updatedAttribute, ProblemDetails? error) = await _eavService.UpdateAttribute(
            createdEntityConfiguration.Attributes[0].AttributeConfigurationId,
            new NumberAttributeConfigurationCreateUpdateRequest
            {
                Description =
                    new List<LocalizedStringCreateRequest>
                    {
                        new LocalizedStringCreateRequest
                        {
                            CultureInfoId = cultureInfoId,
                            String = "updated"
                        }
                    },
                Name = new List<LocalizedStringCreateRequest>
                {
                    new LocalizedStringCreateRequest
                    {
                        CultureInfoId = cultureInfoId,
                        String = "updated"
                    }
                },
                DefaultValue = 999,
                MinimumValue = -1,
                MaximumValue = 10000,
                IsRequired = true,
                IsReadOnly = true
            },
            CancellationToken.None
        );

        updatedAttribute.Should().BeNull();
        error.As<ValidationErrorResponse>().Errors.First().Value.First().Should().Be("Read only attributes cannot be updated");
    }

    [TestMethod]
    public async Task UpdateReadonlyAttributeInstance_AttributeIsNotUpdated()
    {
        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        var readonlyAttributeConfig = configurationCreateRequest.Attributes
            .First(x => ((AttributeConfigurationCreateUpdateRequest)x).MachineName == "name") as AttributeConfigurationCreateUpdateRequest;
        readonlyAttributeConfig.IsReadOnly = true;

        var (createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        var instanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        (EntityInstanceViewModel createdInstance, ProblemDetails _) = await _eavService.CreateEntityInstance(instanceCreateRequest);

        // try to update readonly attribute
        var readonlyAttributeInstance = (LocalizedTextAttributeInstanceCreateUpdateRequest)instanceCreateRequest.Attributes.First(x => x.ConfigurationAttributeMachineName == readonlyAttributeConfig.MachineName);
        var initialValue = readonlyAttributeInstance.Value.First().String;
        readonlyAttributeInstance.Value = new List<LocalizedStringCreateRequest>
        {
            new LocalizedStringCreateRequest
            {
                CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                String = "Updated readonly attribute"
            }
        };

        (EntityInstanceViewModel updatedInstance, ProblemDetails error) = await _eavService.UpdateEntityInstance(
            createdInstance.PartitionKey,
            new EntityInstanceUpdateRequest
            {
                Id = createdInstance.Id,
                EntityConfigurationId = createdInstance.EntityConfigurationId,
                Attributes = new List<AttributeInstanceCreateUpdateRequest>(instanceCreateRequest.Attributes)
            },
            CancellationToken.None
        );

        error.Should().BeNull();
        var instance = await _eavService.GetEntityInstance(createdInstance.Id, createdConfiguration.PartitionKey);
        var updatedAttribute = instance.Attributes.First(x => x.ConfigurationAttributeMachineName == readonlyAttributeConfig.MachineName);
        ((LocalizedTextAttributeInstanceViewModel)updatedAttribute).Value.First().String.Should().Be(initialValue);
    }

    [TestMethod]
    public async Task DeleteReadonlyAttributeConfiguration_Success()
    {
        var cultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID;

        var configCreateRequest = new EntityConfigurationCreateRequest()
        {
            MachineName = "test",
            Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest
                {
                    CultureInfoId = cultureInfoId,
                    String = "test"
                }
            },
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>()
            {
                new NumberAttributeConfigurationCreateUpdateRequest()
                {
                    MachineName = "testAttr",
                    Description =
                        new List<LocalizedStringCreateRequest>
                        {
                            new LocalizedStringCreateRequest
                            {
                                CultureInfoId = cultureInfoId,
                                String = "testAttrDesc"
                            }
                        },
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new LocalizedStringCreateRequest
                        {
                            CultureInfoId = cultureInfoId,
                            String = "testAttrName"
                        }
                    },
                    DefaultValue = 15,
                    IsRequired = true,
                    IsReadOnly = true
                }
            }
        };

        (EntityConfigurationViewModel? createdEntityConfiguration, _) = await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);

        await _eavService.DeleteAttributes(
            new List<Guid> { createdEntityConfiguration.Attributes[0].AttributeConfigurationId },
            CancellationToken.None
        );

        Func<Task> action = async () => await _eavService.GetAttribute(
            createdEntityConfiguration.Attributes[0].AttributeConfigurationId,
            createdEntityConfiguration.Attributes[0].AttributeConfigurationId.ToString(),
            CancellationToken.None
        );

        await action.Should().ThrowAsync<EventSourcing.EventStore.NotFoundException>();
    }

    private IProjectionRepository GetProjectionRepository(ProjectionDocumentSchema schema)
    {
        return new InMemoryProjectionRepository(schema);
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
