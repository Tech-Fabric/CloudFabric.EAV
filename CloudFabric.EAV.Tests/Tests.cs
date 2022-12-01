using System.Globalization;
using System.Reflection;

using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels;
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

    private PostgresqlProjectionRepository<AttributeConfigurationProjectionDocument>
        _attributeConfigurationProjectionRepository;
    private EAVService _eavService;

    private PostgresqlProjectionRepository<EntityConfigurationProjectionDocument>
        _entityConfigurationProjectionRepository;

    private PostgresqlProjectionRepository<EntityInstanceProjectionDocument> _entityInstanceProjectionRepository;
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

        var attributeConfigurationRepository = new AggregateRepository<AttributeConfiguration>(_eventStore);
        var entityConfigurationRepository = new AggregateRepository<EntityConfiguration>(_eventStore);
        var entityInstanceRepository = new AggregateRepository<EntityInstance>(_eventStore);

        // Projections engine - takes events from events observer and passes them to multiple projection builders
        var projectionsEngine = new ProjectionsEngine(GetProjectionRebuildStateRepository());
        projectionsEngine.SetEventsObserver(GetEventStoreEventsObserver());

        _attributeConfigurationProjectionRepository =
            new PostgresqlProjectionRepository<AttributeConfigurationProjectionDocument>(connectionString);
        _entityConfigurationProjectionRepository =
            new PostgresqlProjectionRepository<EntityConfigurationProjectionDocument>(connectionString);
        _entityInstanceProjectionRepository = new PostgresqlProjectionRepository<EntityInstanceProjectionDocument>(connectionString);

        var attributeConfigurationProjectionBuilder = new AttributeConfigurationProjectionBuilder(
            _attributeConfigurationProjectionRepository
        );

        var entityConfigurationProjectionBuilder = new EntityConfigurationProjectionBuilder(
            _entityConfigurationProjectionRepository
        );

        var entityInstanceProjectionBuilder = new EntityInstanceProjectionBuilder(
            _entityInstanceProjectionRepository
        );

        projectionsEngine.AddProjectionBuilder(attributeConfigurationProjectionBuilder);
        projectionsEngine.AddProjectionBuilder(entityConfigurationProjectionBuilder);
        projectionsEngine.AddProjectionBuilder(entityInstanceProjectionBuilder);

        await projectionsEngine.StartAsync("TestInstance");


        _eavService = new EAVService(
            _logger,
            mapper,
            attributeConfigurationRepository,
            entityConfigurationRepository,
            entityInstanceRepository,
            _attributeConfigurationProjectionRepository,
            _entityConfigurationProjectionRepository,
            new EventUserInfo(Guid.NewGuid())
        );
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _eventStore.DeleteAll();

        try
        {
            await _entityConfigurationProjectionRepository.DeleteAll();
            await _attributeConfigurationProjectionRepository.DeleteAll();

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

        EntityConfigurationViewModel createdConfiguration = await _eavService.CreateEntityConfiguration(
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

    public async Task CreateInstance_MissingRequiredAttribute()
    {
        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        EntityConfigurationViewModel createdConfiguration = await _eavService.CreateEntityConfiguration(
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
        EntityConfigurationViewModel createdConfiguration = await _eavService.CreateEntityConfiguration(
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
    public async Task GetEntityConfiguration_Success()
    {

        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        EntityConfigurationViewModel createdConfiguration = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        var configuration = await _eavService.GetEntityConfiguration(
            createdConfiguration.Id,
            createdConfiguration.PartitionKey
        );

        configuration.Should().BeEquivalentTo(createdConfiguration);
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
    public async Task UpdateEntityConfiguration_AddedNewAttribute_Success()
    {
        var cultureId = CultureInfo.GetCultureInfo("EN-us").LCID;

        var configRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        var createdConfig = await _eavService.CreateEntityConfiguration(configRequest, CancellationToken.None);
        const string newAttributeMachineName = "avg_time_mins";

        var newAttributeRequest = new NumberAttributeConfigurationCreateUpdateRequest()
        {
            DefaultValue = 4,
            IsRequired = true,
            Name = new List<LocalizedStringCreateRequest>()
            {
                new LocalizedStringCreateRequest()
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
        var createdConfig = await _eavService.CreateEntityConfiguration(configRequest, CancellationToken.None);
        var newNameRequest = new List<LocalizedStringCreateRequest>()
        {
            new LocalizedStringCreateRequest()
            {
                CultureInfoId = cultureId,
                String = newName
            },
        };
        configRequest.Name = newNameRequest;
        var updateRequest = new EntityConfigurationUpdateRequest()
        {
            Attributes = configRequest.Attributes,
            Id = createdConfig.Id,
            Name = configRequest.Name
        };
        var updatedConfig = await _eavService.UpdateEntityConfiguration(updateRequest, CancellationToken.None);
        updatedConfig.Name.First(n => n.CultureInfoId == cultureId).String.Should().Be(newName);
        updatedConfig.Should().BeEquivalentTo(createdConfig, opt => opt.Excluding(o => o.Name));
    }

    [TestMethod]
    public async Task TestEntityConfigurationProjectionCreated()
    {
        // configure projections
        var entityConfigurationEventsObserver = GetEventStoreEventsObserver();

        // Projections engine - takes events from events observer and passes them to multiple projection builders
        var projectionsEngine = new ProjectionsEngine(GetProjectionRebuildStateRepository());
        projectionsEngine.SetEventsObserver(entityConfigurationEventsObserver);

        var ordersListProjectionBuilder =
            new EntityConfigurationProjectionBuilder(_entityConfigurationProjectionRepository);
        projectionsEngine.AddProjectionBuilder(ordersListProjectionBuilder);

        await projectionsEngine.StartAsync("TestInstance");

        var configurationItemsStart = await _eavService.ListEntityConfigurations(
            ProjectionQuery.Where<EntityConfigurationProjectionDocument>(x => x.MachineName == "BoardGame"),
            null,
            CancellationToken.None
        );

        configurationItemsStart.Count.Should().Be(0);

        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        var createdConfiguration = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        // verify projection is created
        var configurationItems = await _eavService.ListEntityConfigurations(
            ProjectionQuery.Where<EntityConfigurationProjectionDocument>(x => x.MachineName == "BoardGame"),
            null,
            CancellationToken.None
        );

        configurationItems.Count.Should().Be(1);

        await projectionsEngine.StopAsync();
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
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>()
            {
                numberAttribute
            }
        };

        var created = await _eavService.CreateEntityConfiguration(configCreateRequest, CancellationToken.None);
        created.Attributes.Count.Should().Be(1);

        var allAttributes = await _eavService.ListAttributes(100);
        allAttributes.First().As<AttributeConfigurationListItemViewModel>()
            .Name.Should().BeEquivalentTo(numberAttribute.Name);
    }

    [Ignore]
    [TestMethod]
    public async Task UpdateInstance_UpdateAttribute_Success()
    {
        const string changedAttributeName = "players_max";

        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        EntityConfigurationViewModel createdConfiguration = await _eavService.CreateEntityConfiguration(
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
            await _eavService.UpdateEntityInstance(createdInstance.Id.ToString(),
                updateRequest,
                CancellationToken.None
            );
        updatedInstance.Attributes.First(a => a.ConfigurationAttributeMachineName == changedAttributeName)
            .As<NumberAttributeInstanceViewModel>().Value.Should().Be(10);
    }

    [Ignore]
    [TestMethod]
    public async Task UpdateInstance_UpdateAttribute_FailValidation()
    {
        const string changedAttributeName = "players_max";

        EntityConfigurationCreateRequest configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        EntityConfigurationViewModel createdConfiguration = await _eavService.CreateEntityConfiguration(configurationCreateRequest,
            CancellationToken.None
        );

        EntityInstanceCreateRequest entityInstanceCreateRequest = EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        List<AttributeInstanceCreateUpdateRequest> attributesRequest = entityInstanceCreateRequest.Attributes;
        (EntityInstanceViewModel createdInstance, _) = await _eavService.CreateEntityInstance(entityInstanceCreateRequest);

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

        (EntityInstanceViewModel updatedInstance, ProblemDetails validationErrors) = await _eavService.UpdateEntityInstance(createdInstance.Id.ToString(), updateRequest, CancellationToken.None);
        updatedInstance.Should().BeNull();
        validationErrors.As<ValidationErrorResponse>().Errors.Should().ContainKey(changedAttributeName);
    }

    [TestMethod]
    public async Task UpdateInstance_AddAttribute_Success()
    {
        const string changedAttributeName = "avg_time_mins";

        EntityConfigurationCreateRequest configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        EntityConfigurationViewModel createdConfiguration = await _eavService.CreateEntityConfiguration(configurationCreateRequest,
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

        (EntityInstanceViewModel updatedInstance, _) = await _eavService.UpdateEntityInstance(createdInstance.Id.ToString(), updateRequest, CancellationToken.None);
        updatedInstance.Attributes.First(a => a.ConfigurationAttributeMachineName == changedAttributeName).As<NumberAttributeInstanceViewModel>().Value.Should().Be(30);
    }

    [TestMethod]
    public async Task UpdateInstance_AddAttribute_IgnoreAttributeNotInConfig()
    {
        const string changedAttributeName = "min_time_mins";

        EntityConfigurationCreateRequest configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        EntityConfigurationViewModel createdConfiguration = await _eavService.CreateEntityConfiguration(configurationCreateRequest,
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

        (EntityInstanceViewModel updatedInstance, _) = await _eavService.UpdateEntityInstance(createdInstance.Id.ToString(), updateRequest, CancellationToken.None);
        updatedInstance.Attributes.FirstOrDefault(a => a.ConfigurationAttributeMachineName == changedAttributeName).Should().BeNull();
    }

    [TestMethod]
    public async Task UpdateInstance_RemoveAttribute_Success()
    {
        const string changedAttributeName = "description";

        EntityConfigurationCreateRequest configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        EntityConfigurationViewModel createdConfiguration = await _eavService.CreateEntityConfiguration(configurationCreateRequest,
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

        (EntityInstanceViewModel updatedInstance, _) = await _eavService.UpdateEntityInstance(createdInstance.Id.ToString(), updateRequest, CancellationToken.None);
        updatedInstance.Attributes.FirstOrDefault(a => a.ConfigurationAttributeMachineName == changedAttributeName).Should().BeNull();
    }

    [TestMethod]
    public async Task UpdateInstance_RemoveAttribute_FailValidation()
    {
        const string changedAttributeName = "players_max";

        EntityConfigurationCreateRequest configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        EntityConfigurationViewModel createdConfiguration = await _eavService.CreateEntityConfiguration(configurationCreateRequest,
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

        (EntityInstanceViewModel updatedInstance, ProblemDetails errors) = await _eavService.UpdateEntityInstance(createdInstance.Id.ToString(), updateRequest, CancellationToken.None);
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
                new()
                {
                    CultureInfoId = cultureInfoId,
                    String = "Product Price"
                }
            },
            Name = new List<LocalizedStringCreateRequest>()
            {
                new()
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
            Name = new List<LocalizedStringCreateRequest>()
            {
                new()
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
                        new()
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

        var allAttributes = await _eavService.ListAttributes(10000);
        allAttributes.Count.Should().Be(2);
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