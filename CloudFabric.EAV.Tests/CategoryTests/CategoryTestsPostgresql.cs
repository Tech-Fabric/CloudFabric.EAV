using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Postgresql;
using CloudFabric.Projections;
using CloudFabric.Projections.Postgresql;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudFabric.EAV.Tests.CategoryTests;

[TestClass]
public class CategoryTestsPostgresql : CategoryTests
{
    private readonly ProjectionRepositoryFactory _projectionRepositoryFactory;
    private readonly ILogger<PostgresqlEventStoreEventObserver> _logger;

    public CategoryTestsPostgresql()
    {
        var connectionString = "Host=localhost;"
                               + "Username=cloudfabric_eventsourcing_test;"
                               + "Password=cloudfabric_eventsourcing_test;"
                               + "Database=cloudfabric_eventsourcing_test;"
                               + "Maximum Pool Size=1000";

        _eventStore = new PostgresqlEventStore(
            connectionString,
            "eav_tests_event_store",
            "eav_tests_item_store"
        );
        _projectionRepositoryFactory = new PostgresqlProjectionRepositoryFactory(new LoggerFactory(), connectionString);

        _store = new PostgresqlStore(connectionString, "eav_tests_item_store");

        using var loggerFactory = new LoggerFactory();
        _logger = loggerFactory.CreateLogger<PostgresqlEventStoreEventObserver>();
    }

    protected override IEventStore GetEventStore()
    {
        return _eventStore;
    }

    protected override IStore GetStore()
    {
        return _store;
    }

    protected override EventsObserver GetEventStoreEventsObserver()
    {
        return new PostgresqlEventStoreEventObserver((PostgresqlEventStore)_eventStore, _logger);
    }

    protected override ProjectionRepositoryFactory GetProjectionRepositoryFactory()
    {
        return _projectionRepositoryFactory;
    }
}
