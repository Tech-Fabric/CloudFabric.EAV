using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Postgresql;
using CloudFabric.Projections;
using CloudFabric.Projections.ElasticSearch;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudFabric.EAV.Tests.EntityInstanceQueryingTests;

[TestClass]
public class EntityInstanceQueryingTestsPostgresqlWithElasticSearch : EntityInstanceQueryingTests
{
    private readonly ProjectionRepositoryFactory _projectionRepositoryFactory;
    private readonly ILogger<PostgresqlEventStoreEventObserver> _logger;

    public EntityInstanceQueryingTestsPostgresqlWithElasticSearch()
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

        var loggerFactory = new LoggerFactory();

        _projectionRepositoryFactory = new ElasticSearchProjectionRepositoryFactory(
            new ElasticSearchBasicAuthConnectionSettings(
            "http://127.0.0.1:9200",
            "",
            "",
            ""),
            loggerFactory
        );

        _store = new PostgresqlStore(connectionString, "eav_tests_item_store");

        _logger = loggerFactory.CreateLogger<PostgresqlEventStoreEventObserver>();
    }

    protected override TimeSpan ProjectionsUpdateDelay { get; set; } = TimeSpan.FromMilliseconds(1000);

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
