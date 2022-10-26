using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Castle.DynamicProxy.Generators;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Service;
using CloudFabric.EAV.Tests.Factories;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Persistence;
using CloudFabric.EventSourcing.EventStore.Postgresql;
using CloudFabric.Projections;
using CloudFabric.Projections.InMemory;
using CloudFabric.Projections.Queries;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudFabric.EAV.Tests;

[TestClass]
public class Tests
{
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
        });
        var mapper = configuration.CreateMapper();

        _eventStore = new PostgresqlEventStore("Host=localhost;Username=cloudfabric_eventsourcing_test;Password=cloudfabric_eventsourcing_test;Database=cloudfabric_eventsourcing_test;Maximum Pool Size=1000", "eav_tests_event_store");
        await _eventStore.Initialize();
        
        var entityConfigurationRepository = new AggregateRepository<EntityConfiguration>(_eventStore);
        var entityInstanceRepository = new AggregateRepository<EntityInstance>(_eventStore);
        
        _eavService = new EAVService(
            _logger,
            mapper,
            entityConfigurationRepository,
            entityInstanceRepository, 
            new EventUserInfo("test")
        );
    }

    [TestMethod]
    public async Task Test()
    {
        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        var createdConfiguration = await _eavService.CreateEntityConfiguration(
            Guid.Empty, configurationCreateRequest, CancellationToken.None
        );

        var configuration = await _eavService.GetEntityConfiguration(createdConfiguration.Id, createdConfiguration.Id.ToString());

        var entityInstance = EntityInstanceFactory.CreateBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        var createdInstance = await _eavService.CreateEntityInstance(Guid.Empty, entityInstance);

        createdInstance.Id.Should().NotBeEmpty();
        createdInstance.Attributes.Count.Should().Be(entityInstance.Attributes.Count);
    }

    [TestMethod]
    public async Task TestEntityConfigurationProjectionCreated()
    {
        // configure projections
        var entityConfigurationEventsObserver = GetEventStoreEventsObserver();

        // Projections engine - takes events from events observer and passes them to multiple projection builders
        var projectionsEngine = new ProjectionsEngine(GetProjectionRebuildStateRepository());
        projectionsEngine.SetEventsObserver(entityConfigurationEventsObserver);

        var entityConfigurationProjectionRepository = GetProjectionRepository(ProjectionDocumentSchemaFactory.FromTypeWithAttributes<EntityConfigurationProjectionDocument>());
        var ordersListProjectionBuilder = new EntityConfigurationProjectionBuilder(entityConfigurationProjectionRepository);
        projectionsEngine.AddProjectionBuilder(ordersListProjectionBuilder);


        await projectionsEngine.StartAsync("TestInstance");


        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        var createdConfiguration = await _eavService.CreateEntityConfiguration(
            Guid.Empty,
            configurationCreateRequest,
            CancellationToken.None
        );

        // verify projection is created
        var configurationItems = await entityConfigurationProjectionRepository.Query(
            ProjectionQuery.Where<EntityConfigurationProjectionDocument>(x => x.MachineName == "BoardGame")
        );

        configurationItems.Count.Should().Be(1);


        await projectionsEngine.StopAsync();
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
