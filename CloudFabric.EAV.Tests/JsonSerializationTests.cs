using System.Text.Json;

using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Tests.Factories;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.InMemory;
using CloudFabric.Projections;
using CloudFabric.Projections.InMemory;
using CloudFabric.Projections.Queries;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudFabric.EAV.Tests;

[TestClass]
public class JsonSerializationTests : BaseQueryTests.BaseQueryTests
{
    private readonly ProjectionRepositoryFactory _projectionRepositoryFactory;

    public JsonSerializationTests()
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

    [TestMethod]
    public async Task TestCreateInstanceUpdateAndQuery()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityConfigurationViewModel configuration = await _eavService.GetEntityConfiguration(
            createdConfiguration.Id
        );

        configuration.Should().BeEquivalentTo(createdConfiguration);

        string instanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequestJsonSingleLanguage(createdConfiguration.Id);

        (EntityInstanceViewModel createdInstance, ProblemDetails createProblemDetails) =
            await _eavService.CreateEntityInstance(
                instanceCreateRequest,
                configuration.Id,
                configuration.TenantId.Value
            );

        createdInstance.EntityConfigurationId.Should().Be(createdConfiguration.Id);
        createdInstance.TenantId.Should().Be(createdConfiguration.TenantId);

        var query = new ProjectionQuery
        {
            Filters = new List<Filter> { new("Id", FilterOperator.Equal, createdInstance.Id) }
        };

        await Task.Delay(ProjectionsUpdateDelay);

        ProjectionQueryResult<EntityInstanceViewModel>? results = await _eavService
            .QueryInstances(createdConfiguration.Id, query);

        results?.TotalRecordsFound.Should().BeGreaterThan(0);

        results?.Records.Select(r => r.Document).First().Should().BeEquivalentTo(createdInstance);

        ProjectionQueryResult<JsonDocument>? resultsJsonMultiLanguage = await _eavService
            .QueryInstancesJsonMultiLanguage(createdConfiguration.Id, query);

        ProjectionQueryResult<JsonDocument>? resultsJsonSingleLanguage = await _eavService.QueryInstancesJsonSingleLanguage(
            createdConfiguration.Id, query, "EN-us"
        );

        string resultJsonMultiLanguageString = JsonSerializer.Serialize(
            resultsJsonMultiLanguage, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        results?.TotalRecordsFound.Should().BeGreaterThan(0);
    }

}
