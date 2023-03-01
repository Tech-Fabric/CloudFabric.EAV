using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.InMemory;
using CloudFabric.Projections;
using CloudFabric.Projections.InMemory;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudFabric.EAV.Tests;

[TestClass]
public class EntityInstanceQueryingTestsInMemory : EntityInstanceQueryingTests
{
    private readonly ProjectionRepositoryFactory _projectionRepositoryFactory;
    
    public EntityInstanceQueryingTestsInMemory()
    {
        _eventStore = new InMemoryEventStore(new Dictionary<(Guid, string), List<string>>());
        _projectionRepositoryFactory = new InMemoryProjectionRepositoryFactory();
    }

    protected override IEventsObserver GetEventStoreEventsObserver()
    {
        return new InMemoryEventStoreEventObserver((InMemoryEventStore)_eventStore);
    }

    protected override IEventStore GetEventStore()
    {
        return _eventStore;
    }

    protected override ProjectionRepositoryFactory GetProjectionRepositoryFactory()
    {
        return _projectionRepositoryFactory;
    }
    
}