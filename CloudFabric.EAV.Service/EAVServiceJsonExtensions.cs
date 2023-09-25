using System.Text.Json;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Service.Serialization;
using CloudFabric.Projections.Queries;

using Microsoft.AspNetCore.Mvc;

namespace CloudFabric.EAV.Service;

public static class EAVServiceJsonExtensions
{
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
    /// <param name="requiredAttributesCanBeNull">Well, sometimes it's needed to just import the data and then
    /// fill out missing values.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task<(JsonDocument?, ProblemDetails?)> CreateEntityInstance(
        this EAVService eavService,
        string entityJsonString,
        Func<EntityInstanceCreateRequest, bool, Task<EntityInstanceCreateRequest>>? requestDeserializedCallback = null,
        bool dryRun = false,
        bool requiredAttributesCanBeNull = false,
        CancellationToken cancellationToken = default
    )
    {
        JsonDocument entityJson = JsonDocument.Parse(entityJsonString);

        return eavService.CreateEntityInstance(
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
    /// <param name="requiredAttributesCanBeNull">Well, sometimes it's needed to just import the data and then
    /// fill out missing values.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task<(JsonDocument?, ProblemDetails?)> CreateEntityInstance(
        this EAVService eavService,
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

        return eavService.CreateEntityInstance(
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
    /// <param name="requiredAttributesCanBeNull">Well, sometimes it's needed to just import the data and then
    /// fill out missing values.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<(JsonDocument?, ProblemDetails?)> CreateEntityInstance(
        this EAVService eavService,
        JsonElement entityJson,
        Func<EntityInstanceCreateRequest, bool, Task<EntityInstanceCreateRequest>>? requestDeserializedCallback = null,
        bool dryRun = false,
        bool requiredAttributesCanBeNull = false,
        CancellationToken cancellationToken = default
    )
    {
        var (entityInstanceCreateRequest, deserializationErrors) =
            await eavService.DeserializeEntityInstanceCreateRequestFromJson(entityJson, cancellationToken);

        if (deserializationErrors != null)
        {
            return (null, deserializationErrors);
        }

        return await eavService.CreateEntityInstance(
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
    /// <param name="requiredAttributesCanBeNull">Well, sometimes it's needed to just import the data and then
    /// fill out missing values.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<(JsonDocument?, ProblemDetails?)> CreateEntityInstance(
        this EAVService eavService,
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
            eavService.DeserializeEntityInstanceCreateRequestFromJson(
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

        var (createdEntity, validationErrors) = await eavService.CreateEntityInstance(
            entityInstanceCreateRequest!, dryRun, requiredAttributesCanBeNull, cancellationToken
        );

        if (validationErrors != null)
        {
            return (null, validationErrors);
        }

        return (eavService.SerializeEntityInstanceToJsonMultiLanguage(createdEntity), null);
    }


    public static async Task<JsonDocument> GetEntityInstanceJsonMultiLanguage(
        this EAVService eavService, Guid id, string partitionKey)
    {
        EntityInstanceViewModel? entityInstanceViewModel = await eavService.GetEntityInstance(id, partitionKey);

        return eavService.SerializeEntityInstanceToJsonMultiLanguage(entityInstanceViewModel);
    }

    public static async Task<JsonDocument> GetEntityInstanceJsonSingleLanguage(
        this EAVService eavService,
        Guid id,
        string partitionKey,
        string language,
        string fallbackLanguage = "en-US")
    {
        EntityInstanceViewModel? entityInstanceViewModel = await eavService.GetEntityInstance(id, partitionKey);

        return eavService.SerializeEntityInstanceToJsonSingleLanguage(entityInstanceViewModel, language, fallbackLanguage);
    }

    public static JsonDocument SerializeEntityInstanceToJsonMultiLanguage(
        this EAVService eavService, EntityInstanceViewModel? entityInstanceViewModel)
    {
        var serializerOptions = new JsonSerializerOptions(eavService.JsonSerializerOptions);
        serializerOptions.Converters.Add(new LocalizedStringMultiLanguageSerializer());
        serializerOptions.Converters.Add(new EntityInstanceViewModelToJsonSerializer());

        return JsonSerializer.SerializeToDocument(entityInstanceViewModel, serializerOptions);
    }

    public static JsonDocument SerializeEntityInstanceToJsonSingleLanguage(
        this EAVService eavService,
        EntityInstanceViewModel? entityInstanceViewModel, string language, string fallbackLanguage = "en-US"
    )
    {
        var serializerOptions = new JsonSerializerOptions(eavService.JsonSerializerOptions);
        serializerOptions.Converters.Add(new LocalizedStringSingleLanguageSerializer(language, fallbackLanguage));
        serializerOptions.Converters.Add(new EntityInstanceViewModelToJsonSerializer());

        return JsonSerializer.SerializeToDocument(entityInstanceViewModel, serializerOptions);
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
    public static async Task<(EntityInstanceCreateRequest?, ProblemDetails?)> DeserializeEntityInstanceCreateRequestFromJson(
        this EAVService eavService,
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

        return await eavService.DeserializeEntityInstanceCreateRequestFromJson(
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
    public static async Task<(EntityInstanceCreateRequest?, ProblemDetails?)> DeserializeEntityInstanceCreateRequestFromJson(
        this EAVService eavService,
        JsonElement entityJson,
        Guid entityConfigurationId,
        Guid tenantId,
        CancellationToken cancellationToken = default
    )
    {
        EntityConfiguration? entityConfiguration = await eavService.EntityConfigurationRepository.LoadAsync(
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
            await eavService.GetAttributeConfigurationsForEntityConfiguration(
                    entityConfiguration,
                    cancellationToken
                )
                .ConfigureAwait(false);

        var deserializer = new EntityInstanceCreateUpdateRequestFromJsonDeserializer(
            eavService.AttributeConfigurationRepository, eavService.JsonSerializerOptions
        );

        return await deserializer.DeserializeEntityInstanceCreateRequest(
            entityConfigurationId, tenantId, attributeConfigurations, entityJson
        );
    }

    /// <summary>
    /// Returns records in json serialized format.
    /// LocalizedStrings are returned as objects whose property names are language identifiers
    /// and property values are language translation strings.
    ///
    /// EntityInstance with:
    ///
    /// - one text attribute of type LocalizedString "productName"
    /// - one number attribute of type Number "price"
    ///
    /// will be returned in following json format:
    ///
    /// ```
    /// {
    ///   "productName": {
    ///     "en-US": "Terraforming Mars",
    ///     "ru-RU": "Покорение Марса"
    ///   },
    ///   "price": 100
    /// }
    /// ```
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<ProjectionQueryResult<JsonDocument>> QueryInstancesJsonMultiLanguage(
        this EAVService eavService,
        Guid entityConfigurationId,
        ProjectionQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var results = await eavService.QueryInstances(
            entityConfigurationId,
            query,
            cancellationToken
        );

        var serializerOptions = new JsonSerializerOptions(eavService.JsonSerializerOptions);
        serializerOptions.Converters.Add(new EntityInstanceViewModelToJsonSerializer());
        serializerOptions.Converters.Add(new LocalizedStringMultiLanguageSerializer());

        return results.TransformResultDocuments(
            r => JsonSerializer.SerializeToDocument(r, serializerOptions)
        );
    }

    /// <summary>
    /// Returns records in json serialized format.
    /// LocalizedStrings are converted to a single language string of the language passed in parameters.
    ///
    /// EntityInstance with:
    ///
    /// - one text attribute of type LocalizedString "productName"
    /// - one number attribute of type Number "price"
    ///
    /// will be returned in following json format:
    ///
    /// ```
    /// {
    ///   "productName": "Terraforming Mars",
    ///   "price": 100
    /// }
    /// ```
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="query"></param>
    /// <param name="language">Language to use from all localized strings. Only this language strings will be returned.</param>
    /// <param name="fallbackLanguage">If main language will not be found, this language will be tried. Defaults to en-US.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<ProjectionQueryResult<JsonDocument>> QueryInstancesJsonSingleLanguage(
        this EAVService eavService,
        Guid entityConfigurationId,
        ProjectionQuery query,
        string language = "en-US",
        string fallbackLanguage = "en-US",
        CancellationToken cancellationToken = default
    )
    {
        var results = await eavService.QueryInstances(
            entityConfigurationId,
            query,
            cancellationToken
        );

        var serializerOptions = new JsonSerializerOptions(eavService.JsonSerializerOptions);
        serializerOptions.Converters.Add(new EntityInstanceViewModelToJsonSerializer());
        serializerOptions.Converters.Add(new LocalizedStringSingleLanguageSerializer(language, fallbackLanguage));

        return results.TransformResultDocuments(
            r => JsonSerializer.SerializeToDocument(r, serializerOptions)
        );
    }
}
