using System.Reflection;
using System.Text.Json;

using AutoMapper;

using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;
using CloudFabric.EAV.Service;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Persistence;
using CloudFabric.Projections;
using CloudFabric.Projections.Worker;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudFabric.EAV.Tests.BaseQueryTests;

public abstract class BaseQueryTests
{
    protected EAVService _eavService;
    protected EAVCategoryService _eavCategoryService;
    protected ValueAttributeService _valueAttributeService;

    protected IEventStore _eventStore;
    protected IStore _store;
    protected ILogger<EAVService> _eavServiceLogger;
    protected ILogger<EAVCategoryService> _eavCategoryServiceLogger;

    protected virtual TimeSpan ProjectionsUpdateDelay { get; set; } = TimeSpan.FromMilliseconds(0);

    protected abstract IEventStore GetEventStore();
    protected abstract IStore GetStore();
    protected abstract ProjectionRepositoryFactory GetProjectionRepositoryFactory();
    protected abstract EventsObserver GetEventStoreEventsObserver();

    protected ProjectionsRebuildProcessor ProjectionsRebuildProcessor;

    [TestInitialize]
    public async Task SetUp()
    {
        var loggerFactory = new LoggerFactory();
        _eavServiceLogger = loggerFactory.CreateLogger<EAVService>();
        _eavCategoryServiceLogger = loggerFactory.CreateLogger<EAVCategoryService>();

        var eiConfiguration = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(Assembly.GetAssembly(typeof(EAVService)));
            }
        );

        var cConfiguration = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(Assembly.GetAssembly(typeof(EAVCategoryService)));
            }
        );
        IMapper? eiMapper = eiConfiguration.CreateMapper();
        IMapper? cMapper = eiConfiguration.CreateMapper();
        _eventStore = GetEventStore();
        await _eventStore.Initialize();
        await _eventStore.DeleteAll();

        _store = GetStore();
        await _store.Initialize();

        var aggregateRepositoryFactory = new AggregateRepositoryFactory(_eventStore);

        ProjectionRepositoryFactory projectionRepositoryFactory = GetProjectionRepositoryFactory();
        var projectionRepository = projectionRepositoryFactory
            .GetProjectionRepository(
                new ProjectionDocumentSchema
                {
                    SchemaName = ""
                }
            );
        await projectionRepository.DeleteAll();

        // Projections engine - takes events from events observer and passes them to multiple projection builders
        var projectionsEngine = new ProjectionsEngine();
        projectionsEngine.SetEventsObserver(GetEventStoreEventsObserver());

        var attributeConfigurationProjectionBuilder = new AttributeConfigurationProjectionBuilder(
            projectionRepositoryFactory, ProjectionOperationIndexSelector.Write
        );

        var entityConfigurationProjectionBuilder = new EntityConfigurationProjectionBuilder(
            projectionRepositoryFactory, ProjectionOperationIndexSelector.Write
        );

        var entityInstanceProjectionBuilder = new EntityInstanceProjectionBuilder(
            projectionRepositoryFactory, aggregateRepositoryFactory, ProjectionOperationIndexSelector.Write
        );

        projectionsEngine.AddProjectionBuilder(attributeConfigurationProjectionBuilder);
        projectionsEngine.AddProjectionBuilder(entityConfigurationProjectionBuilder);
        projectionsEngine.AddProjectionBuilder(entityInstanceProjectionBuilder);

        await projectionsEngine.StartAsync("TestInstance");

        ProjectionsRebuildProcessor = new ProjectionsRebuildProcessor(
            GetProjectionRepositoryFactory().GetProjectionsIndexStateRepository(),
            async (string connectionId) =>
            {
                var rebuildProjectionsEngine = new ProjectionsEngine();
                rebuildProjectionsEngine.SetEventsObserver(GetEventStoreEventsObserver());

                var attributeConfigurationProjectionBuilder2 = new AttributeConfigurationProjectionBuilder(
                    projectionRepositoryFactory, ProjectionOperationIndexSelector.Write
                );

                var entityConfigurationProjectionBuilder2 = new EntityConfigurationProjectionBuilder(
                    projectionRepositoryFactory, ProjectionOperationIndexSelector.Write
                );

                var entityInstanceProjectionBuilder2 = new EntityInstanceProjectionBuilder(
                    projectionRepositoryFactory, aggregateRepositoryFactory, ProjectionOperationIndexSelector.Write
                );

                rebuildProjectionsEngine.AddProjectionBuilder(attributeConfigurationProjectionBuilder2);
                rebuildProjectionsEngine.AddProjectionBuilder(entityConfigurationProjectionBuilder2);
                rebuildProjectionsEngine.AddProjectionBuilder(entityInstanceProjectionBuilder2);

                return rebuildProjectionsEngine;
            },
            NullLogger<ProjectionsRebuildProcessor>.Instance
        );

        var attributeConfigurationProjectionRepository =
            projectionRepositoryFactory.GetProjectionRepository<AttributeConfigurationProjectionDocument>();
        await attributeConfigurationProjectionRepository.EnsureIndex();

        var entityConfigurationProjectionRepository =
            projectionRepositoryFactory.GetProjectionRepository<EntityConfigurationProjectionDocument>();
        await entityConfigurationProjectionRepository.EnsureIndex();

        await ProjectionsRebuildProcessor.RebuildProjectionsThatRequireRebuild();

        _valueAttributeService = new ValueAttributeService(
            new SerialCounterService(new StoreRepository(_store)),
            eiMapper
        );

        _eavService = new EAVService(
            _eavServiceLogger,
            eiMapper,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            },
            aggregateRepositoryFactory,
            projectionRepositoryFactory,
            new EventUserInfo(Guid.NewGuid()),
            _valueAttributeService
        );

        _eavCategoryService = new EAVCategoryService(
            _eavCategoryServiceLogger,
            _eavService,
            cMapper,
            aggregateRepositoryFactory,
            new EventUserInfo(Guid.NewGuid())
        );
    }
}
