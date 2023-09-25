using System.Diagnostics.CodeAnalysis;

using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Options;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Persistence;
using CloudFabric.Projections.Queries;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudFabric.EAV.Service;

public class EAVCategoryService
{
    private readonly ILogger<EAVCategoryService> _logger;
    private readonly EventUserInfo _userInfo;

    internal readonly IMapper Mapper;
    internal readonly EAVService EAVService;

    private readonly AggregateRepository<CategoryTree> _categoryTreeAggregateRepository;

    private readonly ElasticSearchQueryOptions _elasticSearchQueryOptions;

    public EAVCategoryService(
        ILogger<EAVCategoryService> logger,
        EAVService eavService,
        IMapper mapper,
        AggregateRepositoryFactory aggregateRepositoryFactory,
        EventUserInfo userInfo,
        IOptions<ElasticSearchQueryOptions>? elasticSearchQueryOptions = null
    )
    {
        _logger = logger;
        _userInfo = userInfo;
        Mapper = mapper;

        EAVService = eavService;

        _categoryTreeAggregateRepository = aggregateRepositoryFactory.GetAggregateRepository<CategoryTree>();

        _elasticSearchQueryOptions = elasticSearchQueryOptions != null
            ? elasticSearchQueryOptions.Value
            : new ElasticSearchQueryOptions();
    }

    #region Categories

    public async Task<(CategoryTreeViewModel, ProblemDetails)> CreateCategoryTreeAsync(
        CategoryTreeCreateRequest categoryTreeCreateRequest,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    )
    {
        EntityConfigurationViewModel? entityConfiguration = await EAVService.GetEntityConfiguration(
            categoryTreeCreateRequest.EntityConfigurationId
        );

        if (entityConfiguration == null)
        {
            return (null, new ValidationErrorResponse("EntityConfigurationId", "Configuration not found"))!;
        }

        var tree = new CategoryTree(
            Guid.NewGuid(),
            categoryTreeCreateRequest.EntityConfigurationId,
            categoryTreeCreateRequest.MachineName,
            tenantId
        );

        _ = await _categoryTreeAggregateRepository.SaveAsync(_userInfo, tree, cancellationToken).ConfigureAwait(false);
        return (Mapper.Map<CategoryTreeViewModel>(tree), null)!;
    }

    public async Task<(EntityInstanceViewModel, ProblemDetails)> UpdateCategoryPath(Guid entityInstanceId,
        string entityInstancePartitionKey, Guid treeId, Guid? newParentId, CancellationToken cancellationToken = default)
    {
        EntityInstance? entityInstance = await EAVService.EntityInstanceRepository
            .LoadAsync(entityInstanceId, entityInstancePartitionKey, cancellationToken);

        if (entityInstance == null)
        {
            return (null, new ValidationErrorResponse(nameof(entityInstanceId), "Instance not found"))!;
        }

        (var newCategoryPath, var parentId, ProblemDetails? errors) =
            await BuildCategoryPath(treeId, newParentId, cancellationToken);

        if (errors != null)
        {
            return (null, errors)!;
        }

        entityInstance.UpdateCategoryPath(treeId, newCategoryPath ?? "", parentId!.Value);
        var saved = await EAVService.EntityInstanceRepository
            .SaveAsync(_userInfo, entityInstance, cancellationToken);

        if (!saved)
        {
            //TODO: What do we want to do with internal exceptions and unsuccessful flow?
            throw new Exception("Entity was not saved");
        }

        return (Mapper.Map<EntityInstanceViewModel>(entityInstance), null)!;
    }

    internal async Task<(string?, Guid?, ProblemDetails?)> BuildCategoryPath(Guid treeId, Guid? parentId,
        CancellationToken cancellationToken)
    {
        CategoryTree? tree = await _categoryTreeAggregateRepository.LoadAsync(treeId, treeId.ToString(), cancellationToken);
        if (tree == null)
        {
            return (null, null, new ValidationErrorResponse("TreeId", "Tree not found"));
        }

        EntityInstanceViewModel? parent = parentId == null
            ? null
            : await EAVService.GetEntityInstance(
                parentId.GetValueOrDefault(),
                tree.EntityConfigurationId.ToString()
            );

        if (parent == null && parentId != null)
        {
            return (null, null, new ValidationErrorResponse("ParentId", "Parent category not found"));
        }

        CategoryPathViewModel? parentPath = parent?.CategoryPaths.FirstOrDefault(x => x.TreeId == treeId);
        var categoryPath = parentPath == null ? "" : $"{parentPath.Path}/{parent?.MachineName}";
        return (categoryPath, parent?.Id, null);
    }

    public async Task<(EntityInstanceViewModel?, ProblemDetails?)> CreateCategoryInstance(
        CategoryInstanceCreateRequest categoryCreateRequest,
        CancellationToken cancellationToken = default
    )
    {
        CategoryTree? tree = await _categoryTreeAggregateRepository.LoadAsync(
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

        EntityConfigurationWithAttributesViewModel entityConfigurationWithAttributes = await
            EAVService.GetEntityConfigurationWithAttributes(
                categoryCreateRequest.CategoryConfigurationId, cancellationToken: cancellationToken
            );

        if (entityConfigurationWithAttributes == null)
        {
            return (null, new ValidationErrorResponse("CategoryConfigurationId", "Configuration not found"))!;
        }

        (var categoryPath, Guid? parentId, ProblemDetails? errors) =
            await BuildCategoryPath(tree.Id, categoryCreateRequest.ParentId, cancellationToken).ConfigureAwait(false);

        if (errors != null)
        {
            return (null, errors)!;
        }

        var categoryTreeInstanceCreateRequest = new EntityInstanceCreateRequest
        {
            EntityConfigurationId = categoryCreateRequest.CategoryConfigurationId,
            Attributes = categoryCreateRequest.Attributes,
            MachineName = categoryCreateRequest.MachineName,
            TenantId = categoryCreateRequest.TenantId,
            CategoryPaths = new List<CategoryPathCreateUpdateRequest>()
            {
                new () { Path = categoryPath, ParentId = parentId, TreeId = tree.Id }
            }
        };

        var (categoryTreeInstanceCreated, problemDetails) = await EAVService
            .CreateEntityInstance(categoryTreeInstanceCreateRequest, cancellationToken: cancellationToken);

        if (problemDetails != null)
        {
            return (null, problemDetails);
        }

        return (categoryTreeInstanceCreated, null);
    }

    /// Returns full category tree.
    /// If notDeeperThanCategoryId is specified - returns category tree with all categories that are above or on the same level as a provided.
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
        CategoryTree? tree = await _categoryTreeAggregateRepository
            .LoadAsync(treeId, treeId.ToString(), cancellationToken);

        if (tree == null)
        {
            throw new NotFoundException("Category tree not found");
        }

        ProjectionQueryResult<EntityInstanceViewModel> treeElementsQueryResult =
            await EAVService.QueryInstances(tree.EntityConfigurationId,
                new ProjectionQuery
                {
                    Filters = new List<Filter> { new("CategoryPaths.TreeId", FilterOperator.Equal, treeId) },
                    Limit = _elasticSearchQueryOptions.MaxSize
                },
                cancellationToken
            );

        var treeElements = treeElementsQueryResult.Records
            .Select(x => x.Document!)
            .Select(x => x with
                {
                    CategoryPaths = x.CategoryPaths.Where(cp => cp.TreeId == treeId).ToList().AsReadOnly()
                }
            ).ToList();

        return BuildTreeView(treeElements, notDeeperThanCategoryId);
    }

    private List<EntityTreeInstanceViewModel> BuildTreeView(List<EntityInstanceViewModel> categories, Guid? notDeeperThanCategoryId)
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
                .Where(x => x.CategoryPaths.FirstOrDefault()!
                    .Path.Length <= searchedLevelPathLenght)
                .ToList();
        }

        var treeViewModel = new List<EntityTreeInstanceViewModel>();

        // Go through each instance once
        foreach (EntityInstanceViewModel treeElement in categories
                     .OrderBy(x => x.CategoryPaths.FirstOrDefault()?.Path.Length))
        {
            var treeElementViewModel = Mapper.Map<EntityTreeInstanceViewModel>(treeElement);
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
                            EntityInstanceViewModel? parentInstance = categories.FirstOrDefault(y => y.MachineName == pathComponent);
                            parent = Mapper.Map<EntityTreeInstanceViewModel>(parentInstance);
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

    /// Returns children at one level below of the parent category in internal CategoryParentChildrenViewModel format.
    /// <param name="categoryTreeId"></param>
    /// <param name="parentId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<EntityInstanceViewModel?>> GetSubcategories(
        Guid categoryTreeId,
        Guid? parentId = null,
        string? parentMachineName = null,
        CancellationToken cancellationToken = default
    )
    {
        var categoryTree = await _categoryTreeAggregateRepository.LoadAsync(
            categoryTreeId, categoryTreeId.ToString(), cancellationToken
        ).ConfigureAwait(false);

        if (categoryTree == null)
        {
            throw new NotFoundException("Category tree not found");
        }

        var query = GetSubcategoriesPrepareQuery(categoryTree, parentId, parentMachineName, cancellationToken);

        var queryResult = Mapper.Map<ProjectionQueryResult<EntityInstanceViewModel>>(
            await EAVService.QueryInstances(categoryTree.EntityConfigurationId, query, cancellationToken)
        );

        return queryResult.Records.Select(x => x.Document).ToList() ?? new List<EntityInstanceViewModel?>();
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
            PropertyName = $"{nameof(EntityInstanceViewModel.CategoryPaths)}.{nameof(CategoryPath.TreeId)}",
            Operator = FilterOperator.Equal,
            Value = categoryTree.Id.ToString(),
        });

        // If nothing is set - get subcategories of master level
        if (parentId == null && string.IsNullOrEmpty(parentMachineName))
        {
            query.Filters.Add(new Filter
            {
                PropertyName = $"{nameof(EntityInstanceViewModel.CategoryPaths)}.{nameof(CategoryPath.ParentId)}",
                Operator = FilterOperator.Equal,
                Value = null,
            });
            return query;
        }

        if (parentId != null)
        {

            query.Filters.Add(new Filter
            {
                PropertyName = $"{nameof(EntityInstanceViewModel.CategoryPaths)}.{nameof(CategoryPath.ParentId)}",
                Operator = FilterOperator.Equal,
                Value = parentId.ToString()
            });
        }

        if (!string.IsNullOrEmpty(parentMachineName))
        {

            query.Filters.Add(new Filter
            {
                PropertyName = $"{nameof(EntityInstanceViewModel.CategoryPaths)}.{nameof(CategoryPath.ParentMachineName)}",
                Operator = FilterOperator.Equal,
                Value = parentMachineName
            });
        }
        return query;

    }

    #endregion

}
