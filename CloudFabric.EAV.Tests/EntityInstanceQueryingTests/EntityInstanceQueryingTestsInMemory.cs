using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.InMemory;
using CloudFabric.Projections;
using CloudFabric.Projections.InMemory;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudFabric.EAV.Tests.EntityInstanceQueryingTests;

[TestClass]
public class EntityInstanceQueryingTestsInMemory : EntityInstanceQueryingTests
{
    private readonly ProjectionRepositoryFactory _projectionRepositoryFactory;
    private readonly ILogger<InMemoryEventStoreEventObserver> _logger;

    public EntityInstanceQueryingTestsInMemory()
    {
        var loggerFactory = new LoggerFactory();

        _eventStore = new InMemoryEventStore(new Dictionary<(Guid, string), List<string>>());
        _projectionRepositoryFactory = new InMemoryProjectionRepositoryFactory(loggerFactory);
        _store = new InMemoryStore(new Dictionary<(string, string), string>());
        _logger = loggerFactory.CreateLogger<InMemoryEventStoreEventObserver>();
    }

    protected override EventsObserver GetEventStoreEventsObserver()
    {
        return new InMemoryEventStoreEventObserver((InMemoryEventStore)_eventStore, _logger);
    }

    protected override IEventStore GetEventStore()
    {
        return _eventStore;
    }

    protected override IStore GetStore()
    {
        return _store;
    }

    protected override ProjectionRepositoryFactory GetProjectionRepositoryFactory()
    {
        return _projectionRepositoryFactory;
    }
}
