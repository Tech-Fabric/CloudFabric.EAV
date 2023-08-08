using System.Diagnostics;
using System.Globalization;

using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.Attributes;
using CloudFabric.EAV.Tests.Factories;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Text.Json;

// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace CloudFabric.EAV.Tests.EntityInstanceQueryingTests;

public abstract class EntityInstanceQueryingTests : BaseQueryTests.BaseQueryTests
{
    [TestMethod]
    public async Task TestCreateInstanceAndQuery()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavEntityInstanceService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityConfigurationViewModel configuration = await _eavEntityInstanceService.GetEntityConfiguration(
            createdConfiguration.Id
        );

        configuration.Should().BeEquivalentTo(createdConfiguration);

        EntityInstanceCreateRequest instanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        (EntityInstanceViewModel createdInstance, ProblemDetails createProblemDetails) =
            await _eavEntityInstanceService.CreateEntityInstance(instanceCreateRequest);

        createdInstance.EntityConfigurationId.Should().Be(instanceCreateRequest.EntityConfigurationId);
        createdInstance.TenantId.Should().Be(instanceCreateRequest.TenantId);
        createdInstance.Attributes.Should()
            .BeEquivalentTo(instanceCreateRequest.Attributes, x => x.Excluding(w => w.ValueType));


        var query = new ProjectionQuery
        {
            Filters = new List<Filter> { new("Id", FilterOperator.Equal, createdInstance.Id) }
        };

        await Task.Delay(ProjectionsUpdateDelay);

        ProjectionQueryResult<EntityInstanceViewModel>? results = await _eavEntityInstanceService
            .QueryInstances(createdConfiguration.Id, query);

        results?.TotalRecordsFound.Should().BeGreaterThan(0);

        results?.Records.Select(r => r.Document).First().Should().BeEquivalentTo(createdInstance);
    }

    [TestMethod]
    public async Task TestCreateInstanceUpdateAndQuery()
    {
        EntityConfigurationCreateRequest configurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        (EntityConfigurationViewModel? createdConfiguration, _) = await _eavEntityInstanceService.CreateEntityConfiguration(
            configurationCreateRequest,
            CancellationToken.None
        );

        EntityConfigurationViewModel configuration = await _eavEntityInstanceService.GetEntityConfiguration(
            createdConfiguration.Id
        );

        configuration.Should().BeEquivalentTo(createdConfiguration);

        EntityInstanceCreateRequest instanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        (EntityInstanceViewModel createdInstance, ProblemDetails createProblemDetails) =
            await _eavEntityInstanceService.CreateEntityInstance(instanceCreateRequest);

        createdInstance.EntityConfigurationId.Should().Be(instanceCreateRequest.EntityConfigurationId);
        createdInstance.TenantId.Should().Be(instanceCreateRequest.TenantId);
        createdInstance.Attributes.Should()
            .BeEquivalentTo(instanceCreateRequest.Attributes, x => x.Excluding(w => w.ValueType));


        var query = new ProjectionQuery
        {
            Filters = new List<Filter> { new("Id", FilterOperator.Equal, createdInstance.Id) }
        };

        await Task.Delay(ProjectionsUpdateDelay);

        ProjectionQueryResult<EntityInstanceViewModel>? results = await _eavEntityInstanceService
            .QueryInstances(createdConfiguration.Id, query);

        results?.TotalRecordsFound.Should().BeGreaterThan(0);

        results?.Records.Select(r => r.Document).First().Should().BeEquivalentTo(createdInstance);

        var updatedAttributes = new List<AttributeInstanceCreateUpdateRequest>(instanceCreateRequest.Attributes);
        var nameAttributeValue = (LocalizedTextAttributeInstanceCreateUpdateRequest)updatedAttributes
            .First(a => a.ConfigurationAttributeMachineName == "name");
        nameAttributeValue.Value = new List<LocalizedStringCreateRequest>
        {
            new() { CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID, String = "Azul 2" },
            new() { CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID, String = "Азул 2" }
        };

        (EntityInstanceViewModel updateResult, ProblemDetails updateErrors) =
            await _eavEntityInstanceService.UpdateEntityInstance(createdConfiguration.Id.ToString(),
                new EntityInstanceUpdateRequest
                {
                    Id = createdInstance.Id,
                    EntityConfigurationId = createdInstance.EntityConfigurationId,
                    AttributesToAddOrUpdate = updatedAttributes
                }
            );

        updateErrors.Should().BeNull();

        await Task.Delay(ProjectionsUpdateDelay);

        ProjectionQueryResult<EntityInstanceViewModel>? searchResultsAfterUpdate = await _eavEntityInstanceService
            .QueryInstances(createdConfiguration.Id, query);

        searchResultsAfterUpdate?.TotalRecordsFound.Should().BeGreaterThan(0);

        var nameAttributeAfterUpdate = (LocalizedTextAttributeInstanceViewModel)searchResultsAfterUpdate?.Records
            .Select(r => r.Document).First()
            ?.Attributes.First(a => a.ConfigurationAttributeMachineName == "name")!;

        nameAttributeAfterUpdate.Value.First(v => v.CultureInfoId == CultureInfo.GetCultureInfo("en-US").LCID)
            .String.Should()
            .Be("Azul 2");

        nameAttributeAfterUpdate.Value.First(v => v.CultureInfoId == CultureInfo.GetCultureInfo("ru-RU").LCID)
            .String.Should()
            .Be("Азул 2");

        var resultsJson = await _eavEntityInstanceService
            .QueryInstancesJsonMultiLanguage(createdConfiguration.Id, query);

        var resultString = JsonSerializer.Serialize(resultsJson);

        resultsJson.Records.Count.Should().BeGreaterThan(0);
    }

    //[TestMethod]
    public async Task LoadTest()
    {
        var watch = Stopwatch.StartNew();

        var tasks = new List<Task>();

        for (var i = 0; i < 100; i++)
        {
            for (var j = 0; j < 10; j++)
            {
                tasks.Add(TestCreateInstanceAndQuery());
            }

            await Task.WhenAll(tasks);
            tasks.Clear();
        }

        watch.Stop();

        Console.WriteLine($"It took {watch.Elapsed}!");
    }
}
