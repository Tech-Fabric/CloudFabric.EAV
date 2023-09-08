using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Options;
using CloudFabric.EAV.Service.Serialization;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Persistence;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ProjectionDocumentSchemaFactory =
    CloudFabric.EAV.Domain.Projections.EntityInstanceProjection.ProjectionDocumentSchemaFactory;

namespace CloudFabric.EAV.Service;

public class EAVCategoryService: EAVService<CategoryUpdateRequest, Category, CategoryViewModel>
{

    private readonly ElasticSearchQueryOptions _elasticSearchQueryOptions;

    public EAVCategoryService(ILogger<EAVService<CategoryUpdateRequest, Category, CategoryViewModel>> logger,
        IMapper mapper,
        JsonSerializerOptions jsonSerializerOptions,
        AggregateRepositoryFactory aggregateRepositoryFactory,
        ProjectionRepositoryFactory projectionRepositoryFactory,
        EventUserInfo userInfo,
        ValueAttributeService valueAttributeService,
        IOptions<ElasticSearchQueryOptions>? elasticSearchQueryOptions = null) : base(logger,
        new CategoryFromDictionaryDeserializer(mapper),
        mapper,
        jsonSerializerOptions,
        aggregateRepositoryFactory,
        projectionRepositoryFactory,
        userInfo,
        valueAttributeService)
    {

        _elasticSearchQueryOptions = elasticSearchQueryOptions != null
            ? elasticSearchQueryOptions.Value
            : new ElasticSearchQueryOptions();
    }


    #region Categories

    public async Task<(HierarchyViewModel, ProblemDetails)> CreateCategoryTreeAsync(
        CategoryTreeCreateRequest entity,
        Guid? tenantId,
        CancellationToken cancellationToken = default
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

        var tree = new CategoryTree(
            Guid.NewGuid(),
            entity.EntityConfigurationId,
            entity.MachineName,
            tenantId
        );

        _ = await _categoryTreeRepository.SaveAsync(_userInfo, tree, cancellationToken).ConfigureAwait(false);
        return (_mapper.Map<HierarchyViewModel>(tree), null)!;
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
    ///     "desprition": "Main Category description",
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
    public Task<(JsonDocument?, ProblemDetails?)> CreateCategoryInstance(
        string categoryJsonString,
        Func<CategoryInstanceCreateRequest, Task<CategoryInstanceCreateRequest>>? requestDeserializedCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        JsonDocument categoryJson = JsonDocument.Parse(categoryJsonString);

        return CreateCategoryInstance(
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
    ///     "desprition": "Main Category description"
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
    public Task<(JsonDocument?, ProblemDetails?)> CreateCategoryInstance(
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

        return CreateCategoryInstance(
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
    ///     "desprition": "Main Category description",
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
    public async Task<(JsonDocument?, ProblemDetails?)> CreateCategoryInstance(
        JsonElement categoryJson,
        Func<CategoryInstanceCreateRequest, Task<CategoryInstanceCreateRequest>>? requestDeserializedCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        var (categoryInstanceCreateRequest, deserializationErrors) =
           await DeserializeCategoryInstanceCreateRequestFromJson(categoryJson, cancellationToken: cancellationToken);

        if (deserializationErrors != null)
        {
            return (null, deserializationErrors);
        }

        return await CreateCategoryInstance(
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
    ///     "desprition": "Main Category description"
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
    public async Task<(JsonDocument?, ProblemDetails?)> CreateCategoryInstance(
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
          = await DeserializeCategoryInstanceCreateRequestFromJson(
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

        var (createdCategory, validationErrors) = await CreateCategoryInstance(
            categoryInstanceCreateRequest!, cancellationToken
        );

        if (validationErrors != null)
        {
            return (null, validationErrors);
        }

        return (SerializeEntityInstanceToJsonMultiLanguage(_mapper.Map<CategoryViewModel>(createdCategory)), null);
    }

    public async Task<(CategoryViewModel, ProblemDetails)> CreateCategoryInstance(
        CategoryInstanceCreateRequest categoryCreateRequest,
        CancellationToken cancellationToken = default
    )
    {
        CategoryTree? tree = await _categoryTreeRepository.LoadAsync(
            categoryCreateRequest.CategoryTreeId,
            categoryCreateRequest.CategoryTreeId.ToString(),
            cancellationToken
        ).ConfigureAwait(false);

        if (tree == null)
        {
            return (null, new ValidationErrorResponse("CategoryTreeId", "Category tree not found"))!;
        }

        if (tree.EntityConfigurationId != categoryCreateRequest.CategoryConfigurationId)
        {
            return (null,
                new ValidationErrorResponse("CategoryConfigurationId",
                    "Category tree uses another configuration for categories"
                ))!;
        }

        EntityConfiguration? entityConfiguration = await _entityConfigurationRepository.LoadAsync(
            categoryCreateRequest.CategoryConfigurationId,
            categoryCreateRequest.CategoryConfigurationId.ToString(),
            cancellationToken
        ).ConfigureAwait(false);


        if (entityConfiguration == null)
        {
            return (null, new ValidationErrorResponse("CategoryConfigurationId", "Configuration not found"))!;
        }

        List<AttributeConfiguration> attributeConfigurations =
            await GetAttributeConfigurationsForEntityConfiguration(
                entityConfiguration,
                cancellationToken
            ).ConfigureAwait(false);


        (var categoryPath, Guid? parentId, ProblemDetails? errors) =
            await BuildCategoryPath(tree.Id, categoryCreateRequest.ParentId, cancellationToken).ConfigureAwait(false);

        if (errors != null)
        {
            return (null, errors)!;
        }

        var categoryInstance = new Category(
            Guid.NewGuid(),
            categoryCreateRequest.MachineName,
            categoryCreateRequest.CategoryConfigurationId,
            _mapper.Map<List<AttributeInstance>>(categoryCreateRequest.Attributes),
            categoryCreateRequest.TenantId,
            categoryPath!,
            parentId,
            categoryCreateRequest.CategoryTreeId
        );

        var validationErrors = new Dictionary<string, string[]>();
        foreach (AttributeConfiguration a in attributeConfigurations)
        {
            AttributeInstance? attributeValue = categoryInstance.Attributes
                .FirstOrDefault(attr => a.MachineName == attr.ConfigurationAttributeMachineName);

            List<string> attrValidationErrors = a.ValidateInstance(attributeValue);
            if (attrValidationErrors is { Count: > 0 })
            {
                validationErrors.Add(a.MachineName, attrValidationErrors.ToArray());
            }
        }

        if (validationErrors.Count > 0)
        {
            return (null, new ValidationErrorResponse(validationErrors))!;
        }



        var saved = await _categoryInstanceRepository.SaveAsync(_userInfo, categoryInstance, cancellationToken)
            .ConfigureAwait(false);
        if (!saved)
        {
            //TODO: What do we want to do with internal exceptions and unsuccessful flow?
            throw new Exception("Entity was not saved");
        }

        return (_mapper.Map<CategoryViewModel>(categoryInstance), null)!;
    }

    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "name": "Main Category",
    ///     "desprition": "Main Category description",
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
    public async Task<(CategoryInstanceCreateRequest?, ProblemDetails?)> DeserializeCategoryInstanceCreateRequestFromJson(
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

        return await DeserializeCategoryInstanceCreateRequestFromJson(categoryJson, machineName!, categoryConfigurationId, categoryTreeId, parentId, tenantId, cancellationToken);
    }

    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "name": "Main Category",
    ///     "desprition": "Main Category description"
    /// }
    /// ```
    ///
    /// Where "name" and "description" are attributes machine names.
    /// Note that this overload accepts "entityConfigurationId", "categoryTreeId", "parentId" and "tenantId" via method arguments,
    /// so they should not be in json.
    ///
    /// </remarks>
    public async Task<(CategoryInstanceCreateRequest?, ProblemDetails?)> DeserializeCategoryInstanceCreateRequestFromJson(
        JsonElement categoryJson,
        string machineName,
        Guid categoryConfigurationId,
        Guid categoryTreeId,
        Guid? parentId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    )
    {
        EntityConfiguration? categoryConfiguration = await _entityConfigurationRepository.LoadAsync(
                categoryConfigurationId,
                categoryConfigurationId.ToString(),
                cancellationToken
            )
            .ConfigureAwait(false);

        if (categoryConfiguration == null)
        {
            return (null, new ValidationErrorResponse("CategoryConfigurationId", "CategoryConfiguration not found"))!;
        }

        List<AttributeConfiguration> attributeConfigurations = await GetAttributeConfigurationsForEntityConfiguration(
                    categoryConfiguration,
                    cancellationToken
                )
                .ConfigureAwait(false);

        return await _entityInstanceCreateUpdateRequestFromJsonDeserializer.DeserializeCategoryInstanceCreateRequest(
            categoryConfigurationId, machineName, tenantId, categoryTreeId, parentId, attributeConfigurations, categoryJson
        );
    }

    /// <summary>
    /// Returns full category tree.
    /// If notDeeperThanCategoryId is specified - returns category tree with all categories that are above or on the same lavel as a provided.
    /// <param name="treeId"></param>
    /// <param name="notDeeperThanCategoryId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>

    [SuppressMessage("Performance", "CA1806:Do not ignore method results")]
    public async Task<List<EntityTreeInstanceViewModel>> GetCategoryTreeViewAsync(
        Guid treeId,
        Guid? notDeeperThanCategoryId = null,
        CancellationToken cancellationToken = default
    )
    {
        CategoryTree? tree = await _categoryTreeRepository.LoadAsync(treeId, treeId.ToString(), cancellationToken)
            .ConfigureAwait(false);
        if (tree == null)
        {
            throw new NotFoundException("Category tree not found");
        }

        ProjectionQueryResult<CategoryViewModel> treeElementsQueryResult =
            await QueryInstances(tree.EntityConfigurationId,
                new ProjectionQuery
                {
                    Filters = new List<Filter> { new("CategoryPaths.TreeId", FilterOperator.Equal, treeId) },
                    Limit = _elasticSearchQueryOptions.MaxSize
                },
                cancellationToken
            ).ConfigureAwait(false);

        var treeElements = treeElementsQueryResult.Records
            .Select(x => x.Document!)
            .Select(x =>
            {
                x.CategoryPaths = x.CategoryPaths.Where(cp => cp.TreeId == treeId).ToList();
                return x;
            }).ToList();


        return BuildTreeView(treeElements, notDeeperThanCategoryId);

    }

    private List<EntityTreeInstanceViewModel> BuildTreeView(List<CategoryViewModel> categories, Guid? notDeeperThanCategoryId)
    {

        int searchedLevelPathLenght;

        if (notDeeperThanCategoryId != null)
        {
            var category = categories.FirstOrDefault(x => x.Id == notDeeperThanCategoryId);

            if (category == null)
            {
                throw new NotFoundException("Category not found");
            }

            searchedLevelPathLenght = category.CategoryPaths.FirstOrDefault()!.Path.Length;

            categories = categories
                .Where(x => x.CategoryPaths.FirstOrDefault()!.Path.Length <= searchedLevelPathLenght).ToList();
        }

        var treeViewModel = new List<EntityTreeInstanceViewModel>();

        // Go through each instance once
        foreach (CategoryViewModel treeElement in categories
                     .OrderBy(x => x.CategoryPaths.FirstOrDefault()?.Path.Length))
        {
            var treeElementViewModel = _mapper.Map<EntityTreeInstanceViewModel>(treeElement);
            var categoryPath = treeElement.CategoryPaths.FirstOrDefault()?.Path;

            // If categoryPath is empty, that this is a root model -> add it directly to the tree
            if (string.IsNullOrEmpty(categoryPath))
            {
                treeViewModel.Add(treeElementViewModel);
            }
            else
            {
                // Else split categoryPath and extract each parent machine name
                IEnumerable<string> categoryPathElements =
                    categoryPath.Split('/').Where(x => !string.IsNullOrEmpty(x));

                // Go through each element of the path, remembering where we are atm, and passing current version of treeViewModel
                // Applies an accumulator function over a sequence of paths.
                EntityTreeInstanceViewModel? currentLevel = null;

                categoryPathElements.Aggregate(
                    treeViewModel, // initial value
                    (treeViewModelCurrent, pathComponent) => // apply function to a sequence
                    {
                        // try to find parent with current pathComponent in the current version of treeViewModel in case
                        // it had already been added to our tree model on previous iterations
                        EntityTreeInstanceViewModel? parent =
                            treeViewModelCurrent.FirstOrDefault(y => y.MachineName == pathComponent);

                        // If it is not still there -> find it in the global list of categories and add to our treeViewModel
                        if (parent == null)
                        {
                            CategoryViewModel? parentInstance = categories.FirstOrDefault(y => y.MachineName == pathComponent);
                            parent = _mapper.Map<EntityTreeInstanceViewModel>(parentInstance);
                            treeViewModelCurrent.Add(parent);
                        }

                        // Move to the next level
                        currentLevel = parent;
                        return parent.Children;
                    }
                );
                currentLevel?.Children.Add(treeElementViewModel);
            }
        }
        return treeViewModel;

    }

    /// <summary>
    /// Returns children at one level below of the parent category in internal CategoryParentChildrenViewModel format.
    /// <param name="categoryTreeId"></param>
    /// <param name="parentId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<CategoryViewModel?>> GetSubcategories(
        Guid categoryTreeId,
        Guid? parentId = null,
        string? parentMachineName = null,
        CancellationToken cancellationToken = default
    )
    {
        var categoryTree = await _categoryTreeRepository.LoadAsync(
            categoryTreeId, categoryTreeId.ToString(), cancellationToken
        ).ConfigureAwait(false);

        if (categoryTree == null)
        {
            throw new NotFoundException("Category tree not found");
        }

        var query = GetSubcategoriesPrepareQuery(categoryTree, parentId, parentMachineName, cancellationToken);

        var queryResult = _mapper.Map<ProjectionQueryResult<CategoryViewModel>>(
            await QueryInstances(categoryTree.EntityConfigurationId, query, cancellationToken)
        );

        return queryResult.Records.Select(x => x.Document).ToList() ?? new List<CategoryViewModel?>();
    }

    private ProjectionQuery GetSubcategoriesPrepareQuery(
        CategoryTree categoryTree,
        Guid? parentId,
        string? parentMachineName,
        CancellationToken cancellationToken = default
    )
    {
        ProjectionQuery query = new ProjectionQuery
        {
            Limit = _elasticSearchQueryOptions.MaxSize
        };

        query.Filters.Add(new Filter
        {
            PropertyName = $"{nameof(CategoryViewModel.CategoryPaths)}.{nameof(CategoryPath.TreeId)}",
            Operator = FilterOperator.Equal,
            Value = categoryTree.Id.ToString(),
        });

        // If nothing is set - get subcategories of master level
        if (parentId == null && string.IsNullOrEmpty(parentMachineName))
        {
            query.Filters.Add(new Filter
            {
                PropertyName = $"{nameof(CategoryViewModel.CategoryPaths)}.{nameof(CategoryPath.ParentMachineName)}",
                Operator = FilterOperator.Equal,
                Value = string.Empty,
            });
            return query;
        }

        if (parentId != null)
        {

            query.Filters.Add(new Filter
            {
                PropertyName = $"{nameof(CategoryViewModel.CategoryPaths)}.{nameof(CategoryPath.ParentId)}",
                Operator = FilterOperator.Equal,
                Value = parentId.ToString()
            });
        }

        if (!string.IsNullOrEmpty(parentMachineName))
        {

            query.Filters.Add(new Filter
            {
                PropertyName = $"{nameof(CategoryViewModel.CategoryPaths)}.{nameof(CategoryPath.ParentMachineName)}",
                Operator = FilterOperator.Equal,
                Value = parentMachineName
            });
        }
        return query;

    }

    #endregion

}
