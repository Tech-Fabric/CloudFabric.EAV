using System.Text.Json;

using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Options;
using CloudFabric.EAV.Service.Serialization;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Persistence;
using CloudFabric.Projections;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ProjectionDocumentSchemaFactory =
    CloudFabric.EAV.Domain.Projections.EntityInstanceProjection.ProjectionDocumentSchemaFactory;
namespace CloudFabric.EAV.Service;

public class EAVEntityInstanceService: EAVService<EntityInstanceUpdateRequest, EntityInstance, EntityInstanceViewModel>
{

    public EAVEntityInstanceService(ILogger<EAVService<EntityInstanceUpdateRequest, EntityInstance, EntityInstanceViewModel>> logger,
        IMapper mapper,
        JsonSerializerOptions jsonSerializerOptions,
        AggregateRepositoryFactory aggregateRepositoryFactory,
        ProjectionRepositoryFactory projectionRepositoryFactory,
        EventUserInfo userInfo) : base(logger,
        new EntityInstanceFromDictionaryDeserializer(mapper),
        mapper,
        jsonSerializerOptions,
        aggregateRepositoryFactory,
        projectionRepositoryFactory,
        userInfo)
    {
    }

    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "sku": "123",
    ///     "name": "New Entity",
    ///     "entityConfigurationId": "fb80cb74-6f47-4d38-bb87-25bd820efee7",
    ///     "tenantId": "b6842a71-162b-411d-86e9-3ec01f909c82"
    /// }
    /// ```
    ///
    /// Where "sku" and "name" are attributes machine names,
    /// "entityConfigurationId" - obviously the id of entity configuration which has all attributes,
    /// "tenantId" - tenant id guid. A guid which uniquely identifies and isolates the data. For single tenant
    /// application this should be one hardcoded guid for whole app.
    ///
    /// </remarks>
    public async Task<(EntityInstanceCreateRequest?, ProblemDetails?)> DeserializeEntityInstanceCreateRequestFromJson(
        JsonElement entityJson,
        CancellationToken cancellationToken = default
    )
    {
        Guid entityConfigurationId;
        if (entityJson.TryGetProperty("entityConfigurationId", out var entityConfigurationIdJsonElement))
        {
            if (entityConfigurationIdJsonElement.TryGetGuid(out var entityConfigurationIdGuid))
            {
                entityConfigurationId = entityConfigurationIdGuid;
            }
            else
            {
                return (null, new ValidationErrorResponse("entityConfigurationId", "Value is not a valid Guid"))!;
            }
        }
        else
        {
            return (null, new ValidationErrorResponse("entityConfigurationId", "Value is missing"));
        }

        Guid tenantId;
        if (entityJson.TryGetProperty("tenantId", out var tenantIdJsonElement))
        {
            if (tenantIdJsonElement.TryGetGuid(out var tenantIdGuid))
            {
                tenantId = tenantIdGuid;
            }
            else
            {
                return (null, new ValidationErrorResponse("tenantId", "Value is not a valid Guid"))!;
            }
        }
        else
        {
            return (null, new ValidationErrorResponse("tenantId", "Value is missing"));
        }

        return await DeserializeEntityInstanceCreateRequestFromJson(
            entityJson, entityConfigurationId, tenantId, cancellationToken
        );
    }

    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "sku": "123",
    ///     "name": "New Entity"
    /// }
    /// ```
    ///
    /// Note that this overload accepts "entityConfigurationId" and "tenantId" via method arguments,
    /// so they should not be in json.
    ///
    /// </remarks>
    public async Task<(EntityInstanceCreateRequest?, ProblemDetails?)> DeserializeEntityInstanceCreateRequestFromJson(
        JsonElement entityJson,
        Guid entityConfigurationId,
        Guid tenantId,
        CancellationToken cancellationToken = default
    )
    {
        EntityConfiguration? entityConfiguration = await _entityConfigurationRepository.LoadAsync(
                entityConfigurationId,
                entityConfigurationId.ToString(),
                cancellationToken
            )
            .ConfigureAwait(false);

        if (entityConfiguration == null)
        {
            return (null, new ValidationErrorResponse("EntityConfigurationId", "EntityConfiguration not found"))!;
        }

        List<AttributeConfiguration> attributeConfigurations =
            await GetAttributeConfigurationsForEntityConfiguration(
                    entityConfiguration,
                    cancellationToken
                )
                .ConfigureAwait(false);

        return await _entityInstanceCreateUpdateRequestFromJsonDeserializer.DeserializeEntityInstanceCreateRequest(
            entityConfigurationId, tenantId, attributeConfigurations, entityJson
        );
    }

    /// <summary>
    /// Create new entity instance from provided json string.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "sku": "123",
    ///     "name": "New Entity",
    ///     "entityConfigurationId": "fb80cb74-6f47-4d38-bb87-25bd820efee7",
    ///     "tenantId": "b6842a71-162b-411d-86e9-3ec01f909c82"
    /// }
    /// ```
    ///
    /// Where "sku" and "name" are attributes machine names,
    /// "entityConfigurationId" - obviously the id of entity configuration which has all attributes,
    /// "tenantId" - tenant id guid. A guid which uniquely identifies and isolates the data. For single tenant
    /// application this should be one hardcoded guid for whole app.
    ///
    /// </remarks>
    /// <param name="entityJsonString"></param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<EntityInstanceCreateRequest>(EntityInstanceCreateRequest createRequest, bool dryRun); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to EntityInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    ///
    /// Note that it's important to check dryRun parameter and not make any changes to persistent store if
    /// the parameter equals to 'true'.
    /// </param>
    /// <param name="dryRun">If true, entity will only be validated but not saved to the database</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<(JsonDocument?, ProblemDetails?)> CreateEntityInstance(
        string entityJsonString,
        Func<EntityInstanceCreateRequest, bool, Task<EntityInstanceCreateRequest>>? requestDeserializedCallback = null,
        bool dryRun = false,
        bool requiredAttributesCanBeNull = false,
        CancellationToken cancellationToken = default
    )
    {
        JsonDocument entityJson = JsonDocument.Parse(entityJsonString);

        return CreateEntityInstance(
            entityJson.RootElement,
            requestDeserializedCallback,
            dryRun,
            requiredAttributesCanBeNull,
            cancellationToken
        );
    }

    /// <summary>
    /// Create new entity instance from provided json string.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "sku": "123",
    ///     "name": "New Entity"
    /// }
    /// ```
    ///
    /// Note that this overload accepts "entityConfigurationId" and "tenantId" via method arguments,
    /// so they should not be in json.
    ///
    /// </remarks>
    /// <param name="entityJsonString"></param>
    /// <param name="entityConfigurationId">Id of entity configuration which has all attributes</param>
    /// <param name="tenantId">Tenant id guid. A guid which uniquely identifies and isolates the data. For single
    /// tenant application this should be one hardcoded guid for whole app.</param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<EntityInstanceCreateRequest>(EntityInstanceCreateRequest createRequest, bool dryRun); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to EntityInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    ///
    /// Note that it's important to check dryRun parameter and not make any changes to persistent store if
    /// the parameter equals to 'true'.
    /// </param>
    /// <param name="dryRun">If true, entity will only be validated but not saved to the database</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<(JsonDocument?, ProblemDetails?)> CreateEntityInstance(
        string entityJsonString,
        Guid entityConfigurationId,
        Guid tenantId,
        Func<EntityInstanceCreateRequest, bool, Task<EntityInstanceCreateRequest>>? requestDeserializedCallback = null,
        bool dryRun = false,
        bool requiredAttributesCanBeNull = false,
        CancellationToken cancellationToken = default
    )
    {
        JsonDocument entityJson = JsonDocument.Parse(entityJsonString);

        return CreateEntityInstance(
            entityJson.RootElement,
            entityConfigurationId,
            tenantId,
            requestDeserializedCallback,
            dryRun,
            requiredAttributesCanBeNull,
            cancellationToken
        );
    }

    /// <summary>
    /// Create new entity instance from provided json document.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "sku": "123",
    ///     "name": "New Entity",
    ///     "entityConfigurationId": "fb80cb74-6f47-4d38-bb87-25bd820efee7",
    ///     "tenantId": "b6842a71-162b-411d-86e9-3ec01f909c82"
    /// }
    /// ```
    ///
    /// Where "sku" and "name" are attributes machine names,
    /// "entityConfigurationId" - obviously the id of entity configuration which has all attributes,
    /// "tenantId" - tenant id guid. A guid which uniquely identifies and isolates the data. For single tenant
    /// application this should be one hardcoded guid for whole app.
    ///
    /// </remarks>
    /// <param name="entityJson"></param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<EntityInstanceCreateRequest>(EntityInstanceCreateRequest createRequest, bool dryRun); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to EntityInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    ///
    /// Note that it's important to check dryRun parameter and not make any changes to persistent store if
    /// the parameter equals to 'true'.
    /// </param>
    /// <param name="dryRun">If true, entity will only be validated but not saved to the database</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(JsonDocument?, ProblemDetails?)> CreateEntityInstance(
        JsonElement entityJson,
        Func<EntityInstanceCreateRequest, bool, Task<EntityInstanceCreateRequest>>? requestDeserializedCallback = null,
        bool dryRun = false,
        bool requiredAttributesCanBeNull = false,
        CancellationToken cancellationToken = default
    )
    {
        var (entityInstanceCreateRequest, deserializationErrors) =
            await DeserializeEntityInstanceCreateRequestFromJson(entityJson, cancellationToken);

        if (deserializationErrors != null)
        {
            return (null, deserializationErrors);
        }

        return await CreateEntityInstance(
            entityJson,
            // Deserialization method ensures that EntityConfigurationId and TenantId exist and returns errors if not
            // so it's safe to use ! here
            entityInstanceCreateRequest!.EntityConfigurationId,
            entityInstanceCreateRequest.TenantId!.Value,
            requestDeserializedCallback,
            dryRun,
            requiredAttributesCanBeNull,
            cancellationToken
        );
    }

    /// <summary>
    /// Create new entity instance from provided json document.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "sku": "123",
    ///     "name": "New Entity"
    /// }
    /// ```
    ///
    /// Note that this overload accepts "entityConfigurationId" and "tenantId" via method arguments,
    /// so they should not be in json.
    ///
    /// </remarks>
    /// <param name="entityJson"></param>
    /// <param name="entityConfigurationId">Id of entity configuration which has all attributes</param>
    /// <param name="tenantId">Tenant id guid. A guid which uniquely identifies and isolates the data. For single
    /// tenant application this should be one hardcoded guid for whole app.</param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<EntityInstanceCreateRequest>(EntityInstanceCreateRequest createRequest, bool dryRun); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to EntityInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    ///
    /// Note that it's important to check dryRun parameter and not make any changes to persistent store if
    /// the parameter equals to 'true'.
    /// </param>
    /// <param name="dryRun">If true, entity will only be validated but not saved to the database</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(JsonDocument?, ProblemDetails?)> CreateEntityInstance(
        JsonElement entityJson,
        Guid entityConfigurationId,
        Guid tenantId,
        Func<EntityInstanceCreateRequest, bool, Task<EntityInstanceCreateRequest>>? requestDeserializedCallback = null,
        bool dryRun = false,
        bool requiredAttributesCanBeNull = false,
        CancellationToken cancellationToken = default
    )
    {
        var (entityInstanceCreateRequest, deserializationErrors) = await
            DeserializeEntityInstanceCreateRequestFromJson(
                entityJson, entityConfigurationId, tenantId, cancellationToken
            );

        if (deserializationErrors != null)
        {
            return (null, deserializationErrors);
        }

        if (requestDeserializedCallback != null)
        {
            entityInstanceCreateRequest = await requestDeserializedCallback(entityInstanceCreateRequest!, dryRun);
        }

        var (createdEntity, validationErrors) = await CreateEntityInstance(
            entityInstanceCreateRequest!, dryRun, requiredAttributesCanBeNull, cancellationToken
        );

        if (validationErrors != null)
        {
            return (null, validationErrors);
        }

        return (SerializeEntityInstanceToJsonMultiLanguage(createdEntity), null);
    }



     public async Task<(EntityInstanceViewModel?, ProblemDetails?)> CreateEntityInstance(
        EntityInstanceCreateRequest entity, bool dryRun = false, bool requiredAttributesCanBeNull = false, CancellationToken cancellationToken = default
    )
    {
        EntityConfiguration? entityConfiguration = await _entityConfigurationRepository.LoadAsync(
            entity.EntityConfigurationId,
            entity.EntityConfigurationId.ToString(),
            cancellationToken
        ).ConfigureAwait(false);

        if (entityConfiguration == null)
        {
            return (null, new ValidationErrorResponse("EntityConfigurationId", "Configuration not found"))!;
        }

        List<AttributeConfiguration> attributeConfigurations =
            await GetAttributeConfigurationsForEntityConfiguration(
                entityConfiguration,
                cancellationToken
            ).ConfigureAwait(false);

        //TODO: add check for categoryPath
        var entityInstance = new EntityInstance(
            Guid.NewGuid(),
            entity.EntityConfigurationId,
            _mapper.Map<List<AttributeInstance>>(entity.Attributes),
            entity.TenantId
        );

        var validationErrors = new Dictionary<string, string[]>();
        foreach (AttributeConfiguration a in attributeConfigurations)
        {
            AttributeInstance? attributeValue = entityInstance.Attributes
                .FirstOrDefault(attr => a.MachineName == attr.ConfigurationAttributeMachineName);

            List<string> attrValidationErrors = a.ValidateInstance(attributeValue, requiredAttributesCanBeNull);
            if (attrValidationErrors is { Count: > 0 })
            {
                validationErrors.Add(a.MachineName, attrValidationErrors.ToArray());
            }

            // Note that this method updates entityConfiguration state (for serial attribute it increments the number
            // stored in externalvalues) but does not save entity configuration, we need to do that manually outside of
            // the loop
            InitializeAttributeInstanceWithExternalValuesFromEntity(entityConfiguration, a, attributeValue);
        }

        if (validationErrors.Count > 0)
        {
            return (null, new ValidationErrorResponse(validationErrors))!;
        }

        if (!dryRun)
        {
            var entityConfigurationSaved = await _entityConfigurationRepository
                .SaveAsync(_userInfo, entityConfiguration, cancellationToken)
                .ConfigureAwait(false);

            if (!entityConfigurationSaved)
            {
                throw new Exception("Entity was not saved");
            }

            ProjectionDocumentSchema schema = ProjectionDocumentSchemaFactory
                .FromEntityConfiguration(entityConfiguration, attributeConfigurations);

            IProjectionRepository projectionRepository = _projectionRepositoryFactory.GetProjectionRepository(schema);
            await projectionRepository.EnsureIndex(cancellationToken).ConfigureAwait(false);

            var entityInstanceSaved =
                await _entityInstanceRepository.SaveAsync(_userInfo, entityInstance, cancellationToken);

            if (!entityInstanceSaved)
            {
                //TODO: What do we want to do with internal exceptions and unsuccessful flow?
                throw new Exception("Entity was not saved");
            }

            return (_mapper.Map<EntityInstanceViewModel>(entityInstance), null);
        }

        return (_mapper.Map<EntityInstanceViewModel>(entityInstance), null);
    }

     private void InitializeAttributeInstanceWithExternalValuesFromEntity(
        EntityConfiguration entityConfiguration,
        AttributeConfiguration attributeConfiguration,
        AttributeInstance? attributeInstance
    )
    {
        switch (attributeConfiguration.ValueType)
        {
            case EavAttributeType.Serial:
                {
                    if (attributeInstance == null)
                    {
                        return;
                    }

                    var serialAttributeConfiguration = attributeConfiguration as SerialAttributeConfiguration;

                    var serialInstance = attributeInstance as SerialAttributeInstance;

                    if (serialAttributeConfiguration == null || serialInstance == null)
                    {
                        throw new ArgumentException("Invalid attribute type");
                    }

                    EntityConfigurationAttributeReference? entityAttribute = entityConfiguration.Attributes
                        .FirstOrDefault(x => x.AttributeConfigurationId == attributeConfiguration.Id);

                    if (entityAttribute == null)
                    {
                        throw new NotFoundException("Attribute not found");
                    }

                    var existingAttributeValue =
                        entityAttribute.AttributeConfigurationExternalValues.FirstOrDefault();

                    long? deserializedValue = null;

                    if (existingAttributeValue != null)
                    {
                        deserializedValue = JsonSerializer.Deserialize<long>(existingAttributeValue.ToString()!);
                    }

                    var newExternalValue = existingAttributeValue == null
                        ? serialAttributeConfiguration.StartingNumber
                        : deserializedValue += serialAttributeConfiguration.Increment;

                    serialInstance.Value = newExternalValue!.Value;

                    entityConfiguration.UpdateAttrributeExternalValues(attributeConfiguration.Id,
                        new List<object> { newExternalValue }
                    );
                }
                break;
        }
    }
}
