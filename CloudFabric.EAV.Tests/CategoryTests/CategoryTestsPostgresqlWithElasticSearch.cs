using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Postgresql;
using CloudFabric.Projections;
using CloudFabric.Projections.ElasticSearch;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudFabric.EAV.Tests.CategoryTests
{
    [TestClass]
    public class CategoryTestsPostgresqlWithElasticSearch : CategoryTests
    {
        private readonly ProjectionRepositoryFactory _projectionRepositoryFactory;

        protected override TimeSpan ProjectionsUpdateDelay { get; set; } = TimeSpan.FromMilliseconds(1000);

        public CategoryTestsPostgresqlWithElasticSearch()
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

            _projectionRepositoryFactory = new ElasticSearchProjectionRepositoryFactory(
                "http://127.0.0.1:9200",
                "",
                "",
                "",
                new LoggerFactory()
            );
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
}
