using System.Reflection;
using System.Text.Json;

using AutoMapper;

using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;
using CloudFabric.EAV.Service;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Persistence;
using CloudFabric.Projections;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudFabric.EAV.Tests.BaseQueryTests;

public abstract class BaseQueryTests
{
    protected EAVEntityInstanceService _eavEntityInstanceService;
    protected EAVCategoryService _eavCategoryService;
    
    protected IEventStore _eventStore;
    protected ILogger<EAVEntityInstanceService> _eiLogger;
    protected ILogger<EAVCategoryService> _cLogger;

    protected virtual TimeSpan ProjectionsUpdateDelay { get; set; } = TimeSpan.FromMilliseconds(0);

    protected abstract IEventStore GetEventStore();
    protected abstract ProjectionRepositoryFactory GetProjectionRepositoryFactory();
    protected abstract IEventsObserver GetEventStoreEventsObserver();

    [TestInitialize]
    public async Task SetUp()
    {
        var loggerFactory = new LoggerFactory();
        _eiLogger = loggerFactory.CreateLogger<EAVEntityInstanceService>();
        _cLogger = loggerFactory.CreateLogger<EAVCategoryService>();
        
        var eiConfiguration = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(Assembly.GetAssembly(typeof(EAVEntityInstanceService)));
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

        var aggregateRepositoryFactory = new AggregateRepositoryFactory(_eventStore);

        ProjectionRepositoryFactory projectionRepositoryFactory = GetProjectionRepositoryFactory();

        // Projections engine - takes events from events observer and passes them to multiple projection builders
        var projectionsEngine = new ProjectionsEngine(
            projectionRepositoryFactory.GetProjectionRepository<ProjectionRebuildState>()
        );
        projectionsEngine.SetEventsObserver(GetEventStoreEventsObserver());

        var attributeConfigurationProjectionBuilder = new AttributeConfigurationProjectionBuilder(
            projectionRepositoryFactory, aggregateRepositoryFactory
        );

        var entityInstanceProjectionBuilder = new EntityInstanceProjectionBuilder(
            projectionRepositoryFactory, aggregateRepositoryFactory
        );

        projectionsEngine.AddProjectionBuilder(attributeConfigurationProjectionBuilder);
        projectionsEngine.AddProjectionBuilder(entityInstanceProjectionBuilder);

        await projectionsEngine.StartAsync("TestInstance");

        _eavEntityInstanceService = new EAVEntityInstanceService(
            _eiLogger,
            eiMapper,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            },
            aggregateRepositoryFactory,
            projectionRepositoryFactory,
            new EventUserInfo(Guid.NewGuid())
        );
        
        _eavCategoryService = new EAVCategoryService(
            _cLogger,
            cMapper,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            },
            aggregateRepositoryFactory,
            projectionRepositoryFactory,
            new EventUserInfo(Guid.NewGuid())
        );
    }
}
