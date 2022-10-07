using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Repositories;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Service;
using CloudFabric.EAV.Tests.Factories;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore.Persistence;
using CloudFabric.EventSourcing.EventStore.Postgresql;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudFabric.EAV.Tests;

[TestClass]
public class Tests
{
    private EAVService _eavService;
    private ILogger<EAVService> _logger;

    [TestInitialize]
    public async Task SetUp()
    {
        var loggerFactory = new LoggerFactory();
        _logger = loggerFactory.CreateLogger<EAVService>();
        
        var configuration = new MapperConfiguration(cfg => 
        {
            cfg.AddMaps(Assembly.GetAssembly(typeof(EAVService)));
        });
        var mapper = configuration.CreateMapper();

        var eventStore = new PostgresqlEventStore("Host=localhost;Username=cloudfabric_eventsourcing_test;Password=cloudfabric_eventsourcing_test;Database=cloudfabric_eventsourcing_test;Maximum Pool Size=1000", "eav_tests_event_store");
        await eventStore.Initialize();
        
        var entityConfigurationRepository = new AggregateRepository<EntityConfiguration>(eventStore);
        var entityInstanceRepository = new AggregateRepository<EntityInstance>(eventStore);
        
        _eavService = new EAVService(
            _logger,
            mapper,
            entityConfigurationRepository,
            entityInstanceRepository, 
            new EventUserInfo("test")
        );
    }

    [TestMethod]
    public async Task Test()
    {
        var configurationCreateRequest = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();

        var createdConfiguration = await _eavService.CreateEntityConfiguration(
            Guid.Empty, configurationCreateRequest, CancellationToken.None
        );

        var configuration = await _eavService.GetEntityConfiguration(createdConfiguration.Id, createdConfiguration.Id.ToString());

        var entityInstance = EntityInstanceFactory.CreateBoardGameEntityInstanceCreateRequest(createdConfiguration.Id);

        var createdInstance = await _eavService.CreateEntityInstance(Guid.Empty, entityInstance);

        createdInstance.Id.Should().NotBe(null);
    }
    
}
