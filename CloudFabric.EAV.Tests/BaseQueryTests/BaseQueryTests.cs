using System.Reflection;

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
    protected EAVService _eavService;
    protected IEventStore _eventStore;
    protected ILogger<EAVService> _logger;

    protected virtual TimeSpan ProjectionsUpdateDelay { get; set; } = TimeSpan.FromMilliseconds(0);

    protected abstract IEventStore GetEventStore();
    protected abstract ProjectionRepositoryFactory GetProjectionRepositoryFactory();
    protected abstract IEventsObserver GetEventStoreEventsObserver();

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
        IMapper? mapper = configuration.CreateMapper();

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
}
