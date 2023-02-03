using System.Diagnostics;
using System.Globalization;
using System.Reflection;

using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Service;
using CloudFabric.EAV.Tests.Factories;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Persistence;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudFabric.EAV.Tests;

public abstract class EntityInstanceQueryingTests
{
    private EAVService _eavService;
    private IEventStore _eventStore;
    private ILogger<EAVService> _logger;

    /// <summary>
    /// Some projection engines take time to catch events and update projection records
    /// (like cosmosdb with change feed event observer).
    /// For example, elasticsearch test overrides this with a value of 1000ms
    /// </summary>
    protected virtual TimeSpan ProjectionsUpdateDelay { get; set; } = TimeSpan.FromMilliseconds(0);

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

        _eventStore = GetEventStore();
        await _eventStore.Initialize();

        var aggregateRepositoryFactory = new AggregateRepositoryFactory(_eventStore);
        var attributeConfigurationRepository = aggregateRepositoryFactory
            .GetAggregateRepository<AttributeConfiguration>();
        var entityConfigurationRepository = aggregateRepositoryFactory
            .GetAggregateRepository<EntityConfiguration>();
        var entityInstanceRepository = aggregateRepositoryFactory
            .GetAggregateRepository<EntityInstance>();

        var projectionRepositoryFactory = GetProjectionRepositoryFactory();

        // Projections engine - takes events from events observer and passes them to multiple projection builders
        var projectionsEngine = new ProjectionsEngine(
            projectionRepositoryFactory.GetProjectionRepository<ProjectionRebuildState>()
        );
        projectionsEngine.SetEventsObserver(GetEventStoreEventsObserver());

        var attributeConfigurationProjectionBuilder = new AttributeConfigurationProjectionBuilder(
            projectionRepositoryFactory
        );

        var entityInstanceProjectionBuilder = new EntityInstanceProjectionBuilder(
            aggregateRepositoryFactory,
            projectionRepositoryFactory
        );

        projectionsEngine.AddProjectionBuilder(attributeConfigurationProjectionBuilder);
        projectionsEngine.AddProjectionBuilder(entityInstanceProjectionBuilder);

        await projectionsEngine.StartAsync("TestInstance");

        _eavService = new EAVService(
            _logger,
            mapper,
            aggregateRepositoryFactory,
            projectionRepositoryFactory,
            new EventUserInfo(Guid.NewGuid())
        );
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _eventStore.DeleteAll();

        try
        {
            //await _entityConfigurationProjectionRepository.DeleteAll();
            //await _attributeConfigurationProjectionRepository.DeleteAll();

            //var rebuildStateRepository = GetProjectionRebuildStateRepository();
            //await rebuildStateRepository.DeleteAll();
        }
        catch
        {
        }
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

        configuration.Should().BeEquivalentTo(createdConfiguration);

        var instanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        var (createdInstance, createProblemDetails) = await _eavService.CreateEntityInstance(instanceCreateRequest);

        createdInstance.EntityConfigurationId.Should().Be(instanceCreateRequest.EntityConfigurationId);
        createdInstance.TenantId.Should().Be(instanceCreateRequest.TenantId);
        createdInstance.Attributes.Should().BeEquivalentTo(instanceCreateRequest.Attributes, x => x.Excluding(w => w.ValueType));


        var query = new ProjectionQuery()
        {
            Filters = new List<Filter>() { { new Filter("Id", FilterOperator.Equal, createdInstance.Id) } }
        };

        await Task.Delay(ProjectionsUpdateDelay);

        var results = await _eavService
            .QueryInstances(createdConfiguration.Id, query);

        results?.TotalRecordsFound.Should().BeGreaterThan(0);

        results?.Records.Select(r => r.Document).First().Should().BeEquivalentTo(createdInstance);
    }
    
    
    [TestMethod]
    public async Task CreateCategory_Success()
    {
        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameCategoryConfigurationCreateRequest(0, 9);
        (EntityConfigurationViewModel? categoryConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        var treeRequest = new CategoryTreeCreateRequest()
        {
            MachineName = "Main",
            EntityConfigurationId = categoryConfiguration!.Id,
        };
        
        var (createdTree, _) = await _eavService.CreateCategoryTreeAsync(treeRequest, configurationCreateRequest.TenantId, CancellationToken.None);
        
        var categoryInstanceRequest =
            EntityInstanceFactory.CreateCategoryInstanceRequest(categoryConfiguration.Id, createdTree.Id, null, configurationCreateRequest.TenantId, 0, 9);
        
        var (createdCategory1, validationErrors) =
            await _eavService.CreateCategoryInstanceAsync(categoryInstanceRequest);

        categoryInstanceRequest.ParentId = createdCategory1.Id;
        var (createdCategory12, _) =
            await _eavService.CreateCategoryInstanceAsync(categoryInstanceRequest);

        categoryInstanceRequest.ParentId = createdCategory1.Id;
        var (createdCategory13, _) =
            await _eavService.CreateCategoryInstanceAsync(categoryInstanceRequest);
        
        categoryInstanceRequest.ParentId = createdCategory12.Id;
        var (createdCategory121, _) =
            await _eavService.CreateCategoryInstanceAsync(categoryInstanceRequest);
        
        categoryInstanceRequest.ParentId = createdCategory121.Id;
        var ( createdCategory1211, _) =
            await _eavService.CreateCategoryInstanceAsync(categoryInstanceRequest);

        var list = await _eavService.GetCategoryTreeViewAsync(createdTree.Id, CancellationToken.None);

        var allResults = await _eavService.QueryInstances(categoryConfiguration.Id,
            new ProjectionQuery()
            {
            });
        var subcategories12 = await _eavService.QueryInstances(categoryConfiguration.Id,
            new ProjectionQuery()
            {
                Filters = new List<Filter>()
                {
                    new Filter("CategoryPaths.TreeId", FilterOperator.Equal, createdTree.Id),
                    new Filter("CategoryPaths.Path", FilterOperator.StartsWith, $"\\/{createdCategory1.Id}\\/{createdCategory12.Id}")
                }
            });
        list.Should().Contain(x => x.Id == createdCategory1.Id);
        var instance1 = list.FirstOrDefault(x => x.Id == createdCategory1.Id);
        instance1.Children.Should().Contain(x => x.Id == createdCategory13.Id);
        var instance12 = instance1.Children.FirstOrDefault(x => x.Id == createdCategory12.Id);
        var instance13 = instance1.Children.FirstOrDefault(x => x.Id == createdCategory13.Id);
        instance12.Should().NotBeNull();
        instance13.Should().NotBeNull();
        var instance121 = instance12?.Children.FirstOrDefault(x => x.Id == createdCategory121.Id);
        instance121.Should().NotBeNull();
        var instance1211 = instance121?.Children.FirstOrDefault(x => x.Id == createdCategory1211.Id);
        instance1211.Should().NotBeNull();


        subcategories12.Records.Count.Should().Be(2);
        
        var productConfigurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        var (createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            productConfigurationCreateRequest,
            CancellationToken.None
        );
        
        var instanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);
    
        var (createdInstance, createProblemDetails) = await _eavService.CreateEntityInstance(instanceCreateRequest);
        
    }

    [TestMethod]
    public async Task TestCreateInstanceUpdateAndQuery()
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

        configuration.Should().BeEquivalentTo(createdConfiguration);

        var instanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        var (createdInstance, createProblemDetails) = await _eavService.CreateEntityInstance(instanceCreateRequest);

        createdInstance.EntityConfigurationId.Should().Be(instanceCreateRequest.EntityConfigurationId);
        createdInstance.TenantId.Should().Be(instanceCreateRequest.TenantId);
        createdInstance.Attributes.Should().BeEquivalentTo(instanceCreateRequest.Attributes, x => x.Excluding(w => w.ValueType));


        var query = new ProjectionQuery()
        {
            Filters = new List<Filter>() { { new Filter("Id", FilterOperator.Equal, createdInstance.Id) } }
        };

        await Task.Delay(ProjectionsUpdateDelay);

        var results = await _eavService
            .QueryInstances(createdConfiguration.Id, query);

        results?.TotalRecordsFound.Should().BeGreaterThan(0);

        results?.Records.Select(r => r.Document).First().Should().BeEquivalentTo(createdInstance);

        var updatedAttributes = new List<AttributeInstanceCreateUpdateRequest>(instanceCreateRequest.Attributes);
        var nameAttributeValue = (LocalizedTextAttributeInstanceCreateUpdateRequest)updatedAttributes
            .First(a => a.ConfigurationAttributeMachineName == "name");
        nameAttributeValue.Value = new List<LocalizedStringCreateRequest>()
        {
            new LocalizedStringCreateRequest()
            {
                CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID, String = "Azul 2"
            },
            new LocalizedStringCreateRequest()
            {
                CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID, String = "Азул 2"
            }
        };

        var (updateResult, updateErrors) = await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(),
            new EntityInstanceUpdateRequest()
            {
                Id = createdInstance.Id,
                EntityConfigurationId = createdInstance.EntityConfigurationId,
                Attributes = updatedAttributes
            },
            CancellationToken.None
        );

        updateErrors.Should().BeNull();

        await Task.Delay(ProjectionsUpdateDelay);

        var searchResultsAfterUpdate = await _eavService
            .QueryInstances(createdConfiguration.Id, query);

        searchResultsAfterUpdate?.TotalRecordsFound.Should().BeGreaterThan(0);

        var nameAttributeAfterUpdate = (LocalizedTextAttributeInstanceViewModel)searchResultsAfterUpdate?.Records
            .Select(r => r.Document).First()
            ?.Attributes.First(a => a.ConfigurationAttributeMachineName == "name")!;

        nameAttributeAfterUpdate.Value.First(v => v.CultureInfoId == CultureInfo.GetCultureInfo("EN-us").LCID).String.Should()
            .Be("Azul 2");

        nameAttributeAfterUpdate.Value.First(v => v.CultureInfoId == CultureInfo.GetCultureInfo("RU-ru").LCID).String.Should()
            .Be("Азул 2");
    }

    protected abstract IEventsObserver GetEventStoreEventsObserver();

    //[TestMethod]
    public async Task LoadTest()
    {
        var watch = Stopwatch.StartNew();

        var tasks = new List<Task>();

        for (var i = 0; i < 100; i++)
        {
            for (var j = 0; j < 10; j++)
            {
                tasks.Add(TestCreateInstanceAndQuery());
            }

            await Task.WhenAll(tasks);
            tasks.Clear();
        }

        watch.Stop();

        Console.WriteLine($"It took {watch.Elapsed}!");
    }

    protected abstract IEventStore GetEventStore();
    protected abstract ProjectionRepositoryFactory GetProjectionRepositoryFactory();
}