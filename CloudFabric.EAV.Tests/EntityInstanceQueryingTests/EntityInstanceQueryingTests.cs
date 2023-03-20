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

// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace CloudFabric.EAV.Tests.EntityInstanceQueryingTests;

public abstract class EntityInstanceQueryingTests : BaseQueryTests.BaseQueryTests
{
    [TestMethod]
    public async Task TestCreateInstanceAndQuery()
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

        EntityInstanceCreateRequest instanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        (EntityInstanceViewModel createdInstance, ProblemDetails createProblemDetails) =
            await _eavService.CreateEntityInstance(instanceCreateRequest);

        createdInstance.EntityConfigurationId.Should().Be(instanceCreateRequest.EntityConfigurationId);
        createdInstance.TenantId.Should().Be(instanceCreateRequest.TenantId);
        createdInstance.Attributes.Should()
            .BeEquivalentTo(instanceCreateRequest.Attributes, x => x.Excluding(w => w.ValueType));


        var query = new ProjectionQuery
        {
            Filters = new List<Filter> { new("Id", FilterOperator.Equal, createdInstance.Id) }
        };

        await Task.Delay(ProjectionsUpdateDelay);

        ProjectionQueryResult<EntityInstanceViewModel>? results = await _eavService
            .QueryInstances(createdConfiguration.Id, query);

        results?.TotalRecordsFound.Should().BeGreaterThan(0);

        results?.Records.Select(r => r.Document).First().Should().BeEquivalentTo(createdInstance);
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

        EntityInstanceCreateRequest instanceCreateRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        (EntityInstanceViewModel createdInstance, ProblemDetails createProblemDetails) =
            await _eavService.CreateEntityInstance(instanceCreateRequest);

        createdInstance.EntityConfigurationId.Should().Be(instanceCreateRequest.EntityConfigurationId);
        createdInstance.TenantId.Should().Be(instanceCreateRequest.TenantId);
        createdInstance.Attributes.Should()
            .BeEquivalentTo(instanceCreateRequest.Attributes, x => x.Excluding(w => w.ValueType));


        var query = new ProjectionQuery
        {
            Filters = new List<Filter> { new("Id", FilterOperator.Equal, createdInstance.Id) }
        };

        await Task.Delay(ProjectionsUpdateDelay);

        ProjectionQueryResult<EntityInstanceViewModel>? results = await _eavService
            .QueryInstances(createdConfiguration.Id, query);

        results?.TotalRecordsFound.Should().BeGreaterThan(0);

        results?.Records.Select(r => r.Document).First().Should().BeEquivalentTo(createdInstance);

        var updatedAttributes = new List<AttributeInstanceCreateUpdateRequest>(instanceCreateRequest.Attributes);
        var nameAttributeValue = (LocalizedTextAttributeInstanceCreateUpdateRequest)updatedAttributes
            .First(a => a.ConfigurationAttributeMachineName == "name");
        nameAttributeValue.Value = new List<LocalizedStringCreateRequest>
        {
            new() { CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID, String = "Azul 2" },
            new() { CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID, String = "Азул 2" }
        };

        (EntityInstanceViewModel updateResult, ProblemDetails updateErrors) =
            await _eavService.UpdateEntityInstance(createdConfiguration.Id.ToString(),
                new EntityInstanceUpdateRequest
                {
                    Id = createdInstance.Id,
                    EntityConfigurationId = createdInstance.EntityConfigurationId,
                    Attributes = updatedAttributes
                },
                CancellationToken.None
            );

        updateErrors.Should().BeNull();

        await Task.Delay(ProjectionsUpdateDelay);

        ProjectionQueryResult<EntityInstanceViewModel>? searchResultsAfterUpdate = await _eavService
            .QueryInstances(createdConfiguration.Id, query);

        searchResultsAfterUpdate?.TotalRecordsFound.Should().BeGreaterThan(0);

        var nameAttributeAfterUpdate = (LocalizedTextAttributeInstanceViewModel)searchResultsAfterUpdate?.Records
            .Select(r => r.Document).First()
            ?.Attributes.First(a => a.ConfigurationAttributeMachineName == "name")!;

        nameAttributeAfterUpdate.Value.First(v => v.CultureInfoId == CultureInfo.GetCultureInfo("EN-us").LCID)
            .String.Should()
            .Be("Azul 2");

        nameAttributeAfterUpdate.Value.First(v => v.CultureInfoId == CultureInfo.GetCultureInfo("RU-ru").LCID)
            .String.Should()
            .Be("Азул 2");
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
