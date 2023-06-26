using System.Text.Json;

using CloudFabric.EAV.Domain.Models;
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
    public async Task TestCreateInstanceMultiLangAndQuery()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityConfigurationViewModel configuration = await _eavService.GetEntityConfiguration(
            createdConfiguration!.Id
        );

        configuration.Should().BeEquivalentTo(createdConfiguration);

        string instanceCreateRequest = EntityInstanceFactory
            .CreateValidBoardGameEntityInstanceCreateRequestJsonMultiLanguage(createdConfiguration.Id);

        (JsonDocument createdInstance, ProblemDetails createProblemDetails) =
            await _eavService.CreateEntityInstance(
                instanceCreateRequest,
                configuration.Id,
                configuration.TenantId.Value
            );

        createdInstance.RootElement.GetProperty("entityConfigurationId").GetString()
            .Should().Be(createdConfiguration.Id.ToString());

        createdInstance.RootElement.GetProperty("tenantId").GetString()
            .Should().Be(createdConfiguration.TenantId.ToString());

        var query = new ProjectionQuery
        {
            Filters = new List<Filter>
            {
                new("Id", FilterOperator.Equal, Guid.Parse(createdInstance.RootElement.GetProperty("id").GetString()!))
            }
        };

        await Task.Delay(ProjectionsUpdateDelay);

        ProjectionQueryResult<JsonDocument>? results = await _eavService
            .QueryInstancesJsonMultiLanguage(createdConfiguration.Id, query);

        results?.TotalRecordsFound.Should().BeGreaterThan(0);

        var resultDocument = results?.Records.Select(r => r.Document).First();

        // Query result documents may not have some of the values, so we only compare few basic properties
        // FluentAssertions library is very bad at comparing JsonElements, so we have to do everything manually here.
        resultDocument.RootElement.GetProperty("id").GetString()
            .Should()
            .BeEquivalentTo(createdInstance.RootElement.GetProperty("id").GetString());

        resultDocument.RootElement.GetProperty("entityConfigurationId").GetString()
            .Should()
            .BeEquivalentTo(createdInstance.RootElement.GetProperty("entityConfigurationId").GetString());

        resultDocument.RootElement.GetProperty("tenantId").GetString()
            .Should()
            .BeEquivalentTo(createdInstance.RootElement.GetProperty("tenantId").GetString());

        resultDocument.RootElement.GetProperty("name").GetProperty("en-US").GetString()
            .Should()
            .BeEquivalentTo(createdInstance.RootElement.GetProperty("name").GetProperty("en-US").GetString());

        resultDocument.RootElement.GetProperty("players_min").GetInt16()
            .Should()
            .Be(createdInstance.RootElement.GetProperty("players_min").GetInt16());

        resultDocument.RootElement.GetProperty("price").GetDecimal()
            .Should()
            .Be(createdInstance.RootElement.GetProperty("price").GetDecimal());

        resultDocument.RootElement.GetProperty("release_date").GetProperty("from").GetDateTime()
            .Should()
            .Be(createdInstance.RootElement.GetProperty("release_date").GetProperty("from").GetDateTime());

        ProjectionQueryResult<JsonDocument> resultsJsonMultiLanguage = await _eavService
            .QueryInstancesJsonMultiLanguage(createdConfiguration.Id, query);

        resultsJsonMultiLanguage.Records.First().Document.RootElement.GetProperty("name")
            .GetProperty("en-US").GetString().Should().Be("Azul");
        resultsJsonMultiLanguage.Records.First().Document.RootElement.GetProperty("name")
            .GetProperty("ru-RU").GetString().Should().Be("Азул");

        ProjectionQueryResult<JsonDocument> resultsJsonSingleLanguage = await _eavService
            .QueryInstancesJsonSingleLanguage(
                createdConfiguration.Id, query, "en-US"
            );

        resultsJsonSingleLanguage.Records.First().Document.RootElement.GetProperty("name")
            .GetString().Should().Be("Azul");

        ProjectionQueryResult<JsonDocument>? resultsJsonSingleLanguageRu = await _eavService
            .QueryInstancesJsonSingleLanguage(
                createdConfiguration.Id, query, "ru-RU"
            );

        resultsJsonSingleLanguageRu.Records.First().Document.RootElement.GetProperty("name")
            .GetString().Should().Be("Азул");

        results?.TotalRecordsFound.Should().BeGreaterThan(0);

        // -- one instance test

        var firstDocumentId = resultsJsonMultiLanguage.Records[0]?.Document!.RootElement.GetProperty("id").GetString();

        var oneInstanceJsonMultiLang = await _eavService.GetEntityInstanceJsonMultiLanguage(
            Guid.Parse(firstDocumentId!),
            createdInstance.RootElement.GetProperty("entityConfigurationId").GetString()!
        );

        oneInstanceJsonMultiLang.RootElement.GetProperty("name").GetProperty("en-US").GetString().Should().Be("Azul");

        var oneInstanceJsonSingleLang = await _eavService.GetEntityInstanceJsonSingleLanguage(
            Guid.Parse(firstDocumentId!),
            createdInstance.RootElement.GetProperty("entityConfigurationId").GetString()!,
            "en-US"
        );

        oneInstanceJsonSingleLang.RootElement.GetProperty("name").GetString().Should().Be("Azul");
    }

    [TestMethod]
    public async Task TestCreateInstanceSingleLangAndQuery()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityConfigurationViewModel configuration = await _eavService.GetEntityConfiguration(
            createdConfiguration!.Id
        );

        configuration.Should().BeEquivalentTo(createdConfiguration);

        string instanceCreateRequest = EntityInstanceFactory
            .CreateValidBoardGameEntityInstanceCreateRequestJsonSingleLanguage(createdConfiguration.Id);

        (JsonDocument createdInstance, ProblemDetails createProblemDetails) =
            await _eavService.CreateEntityInstance(
                instanceCreateRequest,
                configuration.Id,
                configuration.TenantId.Value
            );

        createdInstance.RootElement.GetProperty("entityConfigurationId").GetString()
            .Should().Be(createdConfiguration.Id.ToString());

        createdInstance.RootElement.GetProperty("tenantId").GetString()
            .Should().Be(createdConfiguration.TenantId.ToString());

        var query = new ProjectionQuery
        {
            Filters = new List<Filter>
            {
                new("Id", FilterOperator.Equal, Guid.Parse(createdInstance.RootElement.GetProperty("id").GetString()!))
            }
        };

        await Task.Delay(ProjectionsUpdateDelay);

        ProjectionQueryResult<JsonDocument>? results = await _eavService
            .QueryInstancesJsonMultiLanguage(createdConfiguration.Id, query);

        results?.TotalRecordsFound.Should().BeGreaterThan(0);

        var resultDocument = results?.Records.Select(r => r.Document).First();

        // Query result documents may not have some of the values, so we only compare few basic properties
        // FluentAssertions library is very bad at comparing JsonElements, so we have to do everything manually here.
        resultDocument.RootElement.GetProperty("id").GetString()
            .Should()
            .BeEquivalentTo(createdInstance.RootElement.GetProperty("id").GetString());

        resultDocument.RootElement.GetProperty("entityConfigurationId").GetString()
            .Should()
            .BeEquivalentTo(createdInstance.RootElement.GetProperty("entityConfigurationId").GetString());

        resultDocument.RootElement.GetProperty("tenantId").GetString()
            .Should()
            .BeEquivalentTo(createdInstance.RootElement.GetProperty("tenantId").GetString());

        resultDocument.RootElement.GetProperty("name").GetProperty("en-US").GetString()
            .Should()
            .BeEquivalentTo(createdInstance.RootElement.GetProperty("name").GetProperty("en-US").GetString());

        resultDocument.RootElement.GetProperty("players_min").GetInt16()
            .Should()
            .Be(createdInstance.RootElement.GetProperty("players_min").GetInt16());

        resultDocument.RootElement.GetProperty("price").GetDecimal()
            .Should()
            .Be(createdInstance.RootElement.GetProperty("price").GetDecimal());

        resultDocument.RootElement.GetProperty("release_date").GetProperty("from").GetDateTime()
            .Should()
            .Be(createdInstance.RootElement.GetProperty("release_date").GetProperty("from").GetDateTime());

        ProjectionQueryResult<JsonDocument> resultsJsonMultiLanguage = await _eavService
            .QueryInstancesJsonMultiLanguage(createdConfiguration.Id, query);

        resultsJsonMultiLanguage.Records.First().Document.RootElement.GetProperty("name")
            .GetProperty("en-US").GetString().Should().Be("Azul");
    }

    [TestMethod]
    public async Task CreateCategoryInstance()
    {
        JsonSerializerOptions _serializerOptions = new JsonSerializerOptions();
        _serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        EntityConfigurationCreateRequest categoryConfigurationRequest =
            EntityConfigurationFactory.CreateBoardGameCategoryConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdCategoryConfiguration, _) =
            await _eavService.CreateEntityConfiguration(categoryConfigurationRequest, CancellationToken.None);

        (HierarchyViewModel hierarchy, _) = await _eavService.CreateCategoryTreeAsync(
            new CategoryTreeCreateRequest
            {
                EntityConfigurationId = createdCategoryConfiguration!.Id,
                MachineName = "main",
                TenantId = categoryConfigurationRequest.TenantId
            },
            categoryConfigurationRequest.TenantId);

        EntityConfigurationViewModel configuration = await _eavService.GetEntityConfiguration(
            createdCategoryConfiguration!.Id
        );

        configuration.Should().BeEquivalentTo(createdCategoryConfiguration);

        string categoryJsonStringCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameCategoryCreateRequestJson(
                createdCategoryConfiguration!.Id, hierarchy.Id, createdCategoryConfiguration.TenantId!.Value
            );

        (JsonDocument? createdCategory, _) = await _eavService.CreateCategoryInstance(categoryJsonStringCreateRequest);

        var query = new ProjectionQuery
        {
            Filters = new List<Filter>
            {
                new("Id", FilterOperator.Equal, Guid.Parse(createdCategory!.RootElement.GetProperty("id").GetString()!))
            }
        };

        var results = await _eavService.QueryInstancesJsonSingleLanguage(createdCategoryConfiguration.Id, query);

        var resultDocument = results?.Records.Select(r => r.Document).First();

        resultDocument!.RootElement.GetProperty("entityConfigurationId").GetString().Should().Be(createdCategoryConfiguration.Id.ToString());
        resultDocument!.RootElement.GetProperty("tenantId").GetString().Should().Be(createdCategoryConfiguration.TenantId.ToString());

        JsonSerializer.Deserialize<List<CategoryPath>>(resultDocument!.RootElement.GetProperty("categoryPaths"), _serializerOptions)!
            .First().TreeId.Should().Be(hierarchy.Id);

        (createdCategory, _) = await _eavService.CreateCategoryInstance(
            categoryJsonStringCreateRequest,
            "test-category",
            createdCategoryConfiguration.Id,
            hierarchy.Id,
            null,
            createdCategoryConfiguration.TenantId.Value
        );

        createdCategory!.RootElement.GetProperty("entityConfigurationId").GetString().Should().Be(createdCategoryConfiguration.Id.ToString());
        createdCategory!.RootElement.GetProperty("tenantId").GetString().Should().Be(createdCategoryConfiguration.TenantId.ToString());

        JsonSerializer.Deserialize<List<CategoryPath>>(resultDocument!.RootElement.GetProperty("categoryPaths"), _serializerOptions)!
            .First().TreeId.Should().Be(hierarchy.Id);
    }
}
