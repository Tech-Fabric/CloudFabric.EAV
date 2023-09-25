using System.Text.Json;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Service.Serialization;

using Microsoft.AspNetCore.Mvc;

namespace CloudFabric.EAV.Service;

public static class EAVCategoryServiceJsonExtensions
{
    /// <summary>
    /// Create new category from provided json string.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "name": "Main Category",
    ///     "description": "Main Category description",
    ///     "entityConfigurationId": "fb80cb74-6f47-4d38-bb87-25bd820efee7",
    ///     "categoryTreeId": "65053391-9f0e-4b86-959e-2fe342e705d4",
    ///     "parentId": "3e302832-ce6b-4c41-9cf8-e2b3fdd7b01c",
    ///     "tenantId": "b6842a71-162b-411d-86e9-3ec01f909c82"
    /// }
    /// ```
    ///
    /// Where "name" and "description" are attributes machine names,
    /// "entityConfigurationId" - obviously the id of entity configuration which has all category attributes,
    /// "categoryTreeId" - guid of category tree, which represents separated hirerarchy with relations between categories
    /// "parentId" - id guid of category from which new branch of hierarchy will be built.
    /// Can be null if placed at the root of category tree.
    /// "tenantId" - tenant id guid. A guid which uniquely identifies and isolates the data. For single tenant
    /// application this should be one hardcoded guid for whole app.
    ///
    /// </remarks>
    /// <param name="categoryJsonString"></param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<CategoryInstanceCreateRequest>(CategoryInstanceCreateRequest createRequest); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to CategoryInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task<(JsonDocument?, ProblemDetails?)> CreateCategoryInstance(
        this EAVCategoryService eavCategoryService,
        string categoryJsonString,
        Func<CategoryInstanceCreateRequest, Task<CategoryInstanceCreateRequest>>? requestDeserializedCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        JsonDocument categoryJson = JsonDocument.Parse(categoryJsonString);

        return eavCategoryService.CreateCategoryInstance(
            categoryJson.RootElement,
            requestDeserializedCallback,
            cancellationToken
        );
    }

    /// <summary>
    /// Create new category from provided json string.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "name": "Main Category",
    ///     "description": "Main Category description"
    /// }
    /// ```
    ///
    /// Where "name" and "description" are attributes machine names.
    /// Note that this overload accepts "entityConfigurationId", "categoryTreeId", "parentId" and "tenantId" via method arguments,
    /// so they should not be in json.
    ///
    /// </remarks>
    /// <param name="categoryJsonString"></param>
    /// <param name="machineName"></param>
    /// <param name="categoryConfigurationId">id of entity configuration which has all category attributes</param>
    /// <param name="categoryTreeId">id of category tree, which represents separated hirerarchy with relations between categories</param>
    /// <param name="parentId">id of category from which new branch of hierarchy will be built. Can be null if placed at the root of category tree.</param>
    /// <param name="tenantId">tenant id guid. A guid which uniquely identifies and isolates the data. For single
    /// tenant application this should be one hardcoded guid for whole app.</param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<CategoryInstanceCreateRequest>(CategoryInstanceCreateRequest createRequest); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to CategoryInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task<(JsonDocument?, ProblemDetails?)> CreateCategoryInstance(
        this EAVCategoryService eavCategoryService,
        string categoryJsonString,
        string machineName,
        Guid categoryConfigurationId,
        Guid categoryTreeId,
        Guid? parentId,
        Guid? tenantId,
        Func<CategoryInstanceCreateRequest, Task<CategoryInstanceCreateRequest>>? requestDeserializedCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        JsonDocument categoryJson = JsonDocument.Parse(categoryJsonString);

        return eavCategoryService.CreateCategoryInstance(
            categoryJson.RootElement,
            machineName,
            categoryConfigurationId,
            categoryTreeId,
            parentId,
            tenantId,
            requestDeserializedCallback,
            cancellationToken
        );
    }

    /// <summary>
    /// Create new category from provided json document.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "name": "Main Category",
    ///     "description": "Main Category description",
    ///     "entityConfigurationId": "fb80cb74-6f47-4d38-bb87-25bd820efee7",
    ///     "categoryTreeId": "65053391-9f0e-4b86-959e-2fe342e705d4",
    ///     "parentId": "3e302832-ce6b-4c41-9cf8-e2b3fdd7b01c",
    ///     "tenantId": "b6842a71-162b-411d-86e9-3ec01f909c82"
    /// }
    /// ```
    ///
    /// Where "name" and "description" are attributes machine names,
    /// "entityConfigurationId" - obviously the id of entity configuration which has all category attributes,
    /// "categoryTreeId" - guid of category tree, which represents separated hirerarchy with relations between categories
    /// "parentId" - id guid of category from which new branch of hierarchy will be built.
    /// Can be null if placed at the root of category tree.
    /// "tenantId" - tenant id guid. A guid which uniquely identifies and isolates the data. For single tenant
    /// application this should be one hardcoded guid for whole app.
    ///
    /// </remarks>
    /// <param name="categoryJson"></param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<CategoryInstanceCreateRequest>(CategoryInstanceCreateRequest createRequest); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to CategoryInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<(JsonDocument?, ProblemDetails?)> CreateCategoryInstance(
        this EAVCategoryService eavCategoryService,
        JsonElement categoryJson,
        Func<CategoryInstanceCreateRequest, Task<CategoryInstanceCreateRequest>>? requestDeserializedCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        var (categoryInstanceCreateRequest, deserializationErrors) =
           await eavCategoryService.DeserializeCategoryInstanceCreateRequestFromJson(categoryJson, cancellationToken: cancellationToken);

        if (deserializationErrors != null)
        {
            return (null, deserializationErrors);
        }

        return await eavCategoryService.CreateCategoryInstance(
            categoryJson,
            categoryInstanceCreateRequest!.MachineName,
            categoryInstanceCreateRequest!.CategoryConfigurationId,
            categoryInstanceCreateRequest.CategoryTreeId,
            categoryInstanceCreateRequest.ParentId,
            categoryInstanceCreateRequest.TenantId,
            requestDeserializedCallback,
            cancellationToken
        );
    }

    /// <summary>
    /// Create new category from provided json document.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "name": "Main Category",
    ///     "description": "Main Category description"
    /// }
    /// ```
    ///
    /// Where "name" and "description" are attributes machine names.
    /// Note that this overload accepts "entityConfigurationId", "categoryTreeId", "parentId" and "tenantId" via method arguments,
    /// so they should not be in json.
    ///
    /// </remarks>
    /// <param name="categoryJson"></param>
    /// <param name="categoryConfigurationId">id of entity configuration which has all category attributes</param>
    /// <param name="categoryTreeId">id of category tree, which represents separated hirerarchy with relations between categories</param>
    /// <param name="parentId">id of category from which new branch of hierarchy will be built. Can be null if placed at the root of category tree.</param>
    /// <param name="tenantId">Tenant id guid. A guid which uniquely identifies and isolates the data. For single
    /// tenant application this should be one hardcoded guid for whole app.</param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<CategoryInstanceCreateRequest>(CategoryInstanceCreateRequest createRequest); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to CategoryInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<(JsonDocument?, ProblemDetails?)> CreateCategoryInstance(
        this EAVCategoryService eavCategoryService,
        JsonElement categoryJson,
        string machineName,
        Guid categoryConfigurationId,
        Guid categoryTreeId,
        Guid? parentId,
        Guid? tenantId,
        Func<CategoryInstanceCreateRequest, Task<CategoryInstanceCreateRequest>>? requestDeserializedCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        (CategoryInstanceCreateRequest? categoryInstanceCreateRequest, ProblemDetails? deserializationErrors)
          = await eavCategoryService.DeserializeCategoryInstanceCreateRequestFromJson(
              categoryJson,
              machineName,
              categoryConfigurationId,
              categoryTreeId,
              parentId,
              tenantId,
              cancellationToken
            );

        if (deserializationErrors != null)
        {
            return (null, deserializationErrors);
        }

        if (requestDeserializedCallback != null)
        {
            categoryInstanceCreateRequest = await requestDeserializedCallback(categoryInstanceCreateRequest!);
        }

        var (createdCategory, validationErrors) = await eavCategoryService
            .CreateCategoryInstance(
                categoryInstanceCreateRequest!, cancellationToken
            );

        if (validationErrors != null)
        {
            return (null, validationErrors);
        }

        return (eavCategoryService.EAVService.SerializeEntityInstanceToJsonMultiLanguage(
            eavCategoryService.Mapper.Map<EntityInstanceViewModel>(createdCategory)), null
        );
    }


    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "name": "Main Category",
    ///     "description": "Main Category description",
    ///     "entityConfigurationId": "fb80cb74-6f47-4d38-bb87-25bd820efee7",
    ///     "categoryTreeId": "65053391-9f0e-4b86-959e-2fe342e705d4",
    ///     "parentId": "3e302832-ce6b-4c41-9cf8-e2b3fdd7b01c",
    ///     "tenantId": "b6842a71-162b-411d-86e9-3ec01f909c82"
    /// }
    /// ```
    ///
    /// Where "name" and "description" are attributes machine names,
    /// "entityConfigurationId" - obviously the id of entity configuration which has all category attributes,
    /// "categoryTreeId" - guid of category tree, which represents separated hirerarchy with relations between categories
    /// "parentId" - id guid of category from which new branch of hierarchy will be built.
    /// Can be null if placed at the root of category tree.
    /// "tenantId" - tenant id guid. A guid which uniquely identifies and isolates the data. For single tenant
    /// application this should be one hardcoded guid for whole app.
    ///
    /// </remarks>
    public static async Task<(CategoryInstanceCreateRequest?, ProblemDetails?)> DeserializeCategoryInstanceCreateRequestFromJson(
        this EAVCategoryService eavCategoryService,
        JsonElement categoryJson,
        CancellationToken cancellationToken = default
    )
    {
        Guid categoryConfigurationId;
        if (categoryJson.TryGetProperty("categoryConfigurationId", out var categoryConfigurationIdJsonElement))
        {
            if (categoryConfigurationIdJsonElement.TryGetGuid(out var categoryConfigurationIdGuid))
            {
                categoryConfigurationId = categoryConfigurationIdGuid;
            }
            else
            {
                return (null, new ValidationErrorResponse("categoryConfigurationId", "Value is not a valid Guid"))!;
            }
        }
        else
        {
            return (null, new ValidationErrorResponse("categoryConfigurationId", "Value is missing"));
        }

        Guid categoryTreeId;
        if (categoryJson.TryGetProperty("categoryTreeId", out var categoryTreeIdJsonElement))
        {
            if (categoryTreeIdJsonElement.TryGetGuid(out var categoryTreeIdGuid))
            {
                categoryTreeId = categoryTreeIdGuid;
            }
            else
            {
                return (null, new ValidationErrorResponse("categoryTreeId", "Value is not a valid Guid"))!;
            }
        }
        else
        {
            return (null, new ValidationErrorResponse("categoryTreeId", "Value is missing"));
        }

        Guid? parentId = null;
        if (categoryJson.TryGetProperty("parentId", out var parentIdJsonElement))
        {
            if (parentIdJsonElement.ValueKind == JsonValueKind.Null)
            {
                parentId = null;
            }
            else if (parentIdJsonElement.TryGetGuid(out var parentIdGuid))
            {
                parentId = parentIdGuid;
            }
            else
            {
                return (null, new ValidationErrorResponse("parentId", "Value is not a valid Guid"))!;
            }
        }

        Guid? tenantId = null;
        if (categoryJson.TryGetProperty("tenantId", out var tenantIdJsonElement))
        {
            if (tenantIdJsonElement.ValueKind == JsonValueKind.Null)
            {
                tenantId = null;
            }
            else if (tenantIdJsonElement.TryGetGuid(out var tenantIdGuid))
            {
                tenantId = tenantIdGuid;
            }
            else
            {
                return (null, new ValidationErrorResponse("tenantId", "Value is not a valid Guid"))!;
            }
        }

        string? machineName = null;
        if (categoryJson.TryGetProperty("machineName", out var machineNameJsonElement))
        {
            machineName = machineNameJsonElement.ValueKind == JsonValueKind.Null ? null : machineNameJsonElement.GetString();
            if (machineName == null)
            {
                return (null, new ValidationErrorResponse("machineName", "Value is not a valid"));
            }
        }

        return await eavCategoryService.DeserializeCategoryInstanceCreateRequestFromJson(
            categoryJson, machineName!, categoryConfigurationId, categoryTreeId, parentId, tenantId, cancellationToken
        );
    }

    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "name": "Main Category",
    ///     "description": "Main Category description"
    /// }
    /// ```
    ///
    /// Where "name" and "description" are attributes machine names.
    /// Note that this overload accepts "entityConfigurationId", "categoryTreeId", "parentId" and "tenantId" via method arguments,
    /// so they should not be in json.
    public static async Task<(CategoryInstanceCreateRequest?, ProblemDetails?)> DeserializeCategoryInstanceCreateRequestFromJson(
        this EAVCategoryService eavCategoryService,
        JsonElement categoryJson,
        string machineName,
        Guid categoryConfigurationId,
        Guid categoryTreeId,
        Guid? parentId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    )
    {
        EntityConfiguration? categoryConfiguration = await eavCategoryService.EAVService.EntityConfigurationRepository
            .LoadAsync(
                categoryConfigurationId,
                categoryConfigurationId.ToString(),
                cancellationToken
            );

        if (categoryConfiguration == null)
        {
            return (null, new ValidationErrorResponse("CategoryConfigurationId", "CategoryConfiguration not found"))!;
        }

        List<AttributeConfiguration> attributeConfigurations = await eavCategoryService.EAVService
            .GetAttributeConfigurationsForEntityConfiguration(
                categoryConfiguration,
                cancellationToken
            );

        var deserializer = new EntityInstanceCreateUpdateRequestFromJsonDeserializer(
            eavCategoryService.EAVService.AttributeConfigurationRepository,
            eavCategoryService.EAVService.JsonSerializerOptions
        );

        return await deserializer.DeserializeCategoryInstanceCreateRequest(
            categoryConfigurationId, machineName, tenantId, categoryTreeId, parentId, attributeConfigurations,
            categoryJson
        );
    }
}
