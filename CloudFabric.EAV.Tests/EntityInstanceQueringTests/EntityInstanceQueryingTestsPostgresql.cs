using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Postgresql;
using CloudFabric.Projections;
using CloudFabric.Projections.Postgresql;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudFabric.EAV.Tests;

[TestClass]
public class EntityInstanceQueryingTestsPostgresql : EntityInstanceQueryingTests
{
    private readonly ProjectionRepositoryFactory _projectionRepositoryFactory;
    
    public EntityInstanceQueryingTestsPostgresql()
    {
        var connectionString = "Host=localhost;"
                               + "Username=cloudfabric_eventsourcing_test;"
                               + "Password=cloudfabric_eventsourcing_test;"
                               + "Database=cloudfabric_eventsourcing_test;"
                               + "Maximum Pool Size=1000";

        _eventStore = new PostgresqlEventStore(
            connectionString,
            "eav_tests_event_store"
        );
        _projectionRepositoryFactory = new PostgresqlProjectionRepositoryFactory(connectionString);
    }

    protected override IEventStore GetEventStore()
    {
        return _eventStore;
    }

    protected override IEventsObserver GetEventStoreEventsObserver()
    {
        return new PostgresqlEventStoreEventObserver((PostgresqlEventStore)_eventStore);
    }

    protected override ProjectionRepositoryFactory GetProjectionRepositoryFactory()
    {
        return _projectionRepositoryFactory;
    }
}