// using System.Text.Json;
//
// using AutoMapper;
//
// using CloudFabric.EAV.Domain.GeneratedValues;
// using CloudFabric.EAV.Domain.Models;
// using CloudFabric.EAV.Enums;
// using CloudFabric.EAV.Models.RequestModels;
// using CloudFabric.EAV.Models.ViewModels;
// using CloudFabric.EAV.Service.Serialization;
// using CloudFabric.EventSourcing.Domain;
// using CloudFabric.EventSourcing.EventStore.Persistence;
// using CloudFabric.Projections;
//
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Logging;
//
// using ProjectionDocumentSchemaFactory =
//     CloudFabric.EAV.Domain.Projections.EntityInstanceProjection.ProjectionDocumentSchemaFactory;
// namespace CloudFabric.EAV.Service;
//
// public class EAVEntityInstanceService : EAVService<EntityInstanceUpdateRequest, EntityInstance, EntityInstanceViewModel>
// {
//
//     public EAVEntityInstanceService(ILogger<EAVService<EntityInstanceUpdateRequest, EntityInstance, EntityInstanceViewModel>> logger,
//         IMapper mapper,
//         JsonSerializerOptions jsonSerializerOptions,
//         AggregateRepositoryFactory aggregateRepositoryFactory,
//         ProjectionRepositoryFactory projectionRepositoryFactory,
//         EventUserInfo userInfo,
//         ValueAttributeService valueAttributeService) : base(logger,
//         new EntityInstanceFromDictionaryDeserializer(mapper),
//         mapper,
//         jsonSerializerOptions,
//         aggregateRepositoryFactory,
//         projectionRepositoryFactory,
//         userInfo,
//         valueAttributeService)
//     {
//     }
//
// }
