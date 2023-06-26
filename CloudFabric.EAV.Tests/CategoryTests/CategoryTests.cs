 using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Tests.Factories;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AsyncConverter.AsyncMethodNamingHighlighting
// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace CloudFabric.EAV.Tests.CategoryTests;

public abstract class CategoryTests : BaseQueryTests.BaseQueryTests
{
    private const string _laptopsCategoryMachineName = "laptops";
    private const string _gamingLaptopsCategoryMachineName = "gaming-laptops";
    private const string _officeLaptopsCategoryMachineName = "office-laptops";
    private const string _asusGamingLaptopsCategoryMachineName = "asus-gaming-laptops";
    private const string _rogAsusGamingLaptopsCategoryMachineName = "rog-gaming-laptops";

    private async Task<(HierarchyViewModel tree,
        CategoryViewModel laptopsCategory,
        CategoryViewModel gamingLaptopsCategory,
        CategoryViewModel officeLaptopsCategory,
        CategoryViewModel asusGamingLaptopsCategory,
        CategoryViewModel rogAsusGamingLaptopsCategory)> BuildTestTreeAsync()
    {
        // Create config for categories
        EntityConfigurationCreateRequest categoryConfigurationCreateRequest =
            EntityConfigurationFactory.CreateBoardGameCategoryConfigurationCreateRequest(0, 9);
        (EntityConfigurationViewModel? categoryConfiguration, _) = await _eavService.CreateEntityConfiguration(
            categoryConfigurationCreateRequest,
            CancellationToken.None
        );

        // Create a tree
        var treeRequest = new CategoryTreeCreateRequest
        {
            MachineName = "Main",
            EntityConfigurationId = categoryConfiguration!.Id
        };

        (HierarchyViewModel createdTree, _) = await _eavService.CreateCategoryTreeAsync(treeRequest,
            categoryConfigurationCreateRequest.TenantId,
            CancellationToken.None
        );

        (CategoryViewModel laptopsCategory, _) =
            await _eavService.CreateCategoryInstance(EntityInstanceFactory.CreateCategoryInstanceRequest(categoryConfiguration.Id,
                createdTree.Id,
                null,
                categoryConfigurationCreateRequest.TenantId,
                _laptopsCategoryMachineName,
                0,
                9
            ));

        (CategoryViewModel gamingLaptopsCategory, _) =
            await _eavService.CreateCategoryInstance(EntityInstanceFactory.CreateCategoryInstanceRequest(categoryConfiguration.Id,
                createdTree.Id,
                laptopsCategory.Id,
                categoryConfigurationCreateRequest.TenantId,
                _gamingLaptopsCategoryMachineName,
                0,
                9
            ));

        (CategoryViewModel officeLaptopsCategory, _) =
            await _eavService.CreateCategoryInstance(EntityInstanceFactory.CreateCategoryInstanceRequest(categoryConfiguration.Id,
                createdTree.Id,
                laptopsCategory.Id,
                categoryConfigurationCreateRequest.TenantId,
                _officeLaptopsCategoryMachineName,
                0,
                9
            ));

        (CategoryViewModel asusGamingLaptopsCategory, _) =
            await _eavService.CreateCategoryInstance(EntityInstanceFactory.CreateCategoryInstanceRequest(categoryConfiguration.Id,
                createdTree.Id,
                gamingLaptopsCategory.Id,
                categoryConfigurationCreateRequest.TenantId,
                _asusGamingLaptopsCategoryMachineName,
                0,
                9
            ));

        (CategoryViewModel rogAsusGamingLaptopsCategory, _) =
            await _eavService.CreateCategoryInstance(EntityInstanceFactory.CreateCategoryInstanceRequest(categoryConfiguration.Id,
                createdTree.Id,
                asusGamingLaptopsCategory.Id,
                categoryConfigurationCreateRequest.TenantId,
                _rogAsusGamingLaptopsCategoryMachineName,
                0,
                9
            ));
        await Task.Delay(ProjectionsUpdateDelay);

        return (createdTree, laptopsCategory, gamingLaptopsCategory, officeLaptopsCategory,
            asusGamingLaptopsCategory, rogAsusGamingLaptopsCategory);
    }

    [TestMethod]
    public async Task CreateCategory_Success()
    {
        (_, CategoryViewModel laptopsCategory, CategoryViewModel gamingLaptopsCategory, _, _, _) =
            await BuildTestTreeAsync();

        laptopsCategory.Id.Should().NotBeEmpty();
        laptopsCategory.Attributes.Count.Should().Be(9);
        laptopsCategory.TenantId.Should().NotBeNull();
        gamingLaptopsCategory.CategoryPaths.Should().Contain(x => x.Path.Contains(laptopsCategory.MachineName));
    }

    [TestMethod]
    public async Task GetTreeViewAsync()
    {
        (HierarchyViewModel createdTree, CategoryViewModel laptopsCategory, CategoryViewModel gamingLaptopsCategory,
            CategoryViewModel officeLaptopsCategory, CategoryViewModel asusGamingLaptopsCategory,
            CategoryViewModel rogAsusGamingLaptopsCategory) = await BuildTestTreeAsync();

        List<EntityTreeInstanceViewModel> list =
            await _eavService.GetCategoryTreeViewAsync(createdTree.Id);

        EntityTreeInstanceViewModel? laptops = list.FirstOrDefault(x => x.Id == laptopsCategory.Id);
        laptops.Should().NotBeNull();
        laptops.Children.Should().Contain(x => x.Id == officeLaptopsCategory.Id);
        EntityTreeInstanceViewModel? gamingLaptops =
            laptops.Children.FirstOrDefault(x => x.Id == gamingLaptopsCategory.Id);
        EntityTreeInstanceViewModel? officeLaptops =
            laptops.Children.FirstOrDefault(x => x.Id == officeLaptopsCategory.Id);
        gamingLaptops.Should().NotBeNull();
        officeLaptops.Should().NotBeNull();
        EntityTreeInstanceViewModel? asusGamingLaptops =
            gamingLaptops?.Children.FirstOrDefault(x => x.Id == asusGamingLaptopsCategory.Id);
        asusGamingLaptops.Should().NotBeNull();
        EntityTreeInstanceViewModel? rogAsusGamingLaptops =
            asusGamingLaptops?.Children.FirstOrDefault(x => x.Id == rogAsusGamingLaptopsCategory.Id);
        rogAsusGamingLaptops.Should().NotBeNull();

        list = await _eavService.GetCategoryTreeViewAsync(createdTree.Id, laptopsCategory.Id);
        laptops = list.FirstOrDefault(x => x.Id == laptopsCategory.Id);
        laptops.Children.Count.Should().Be(0);

        list = await _eavService.GetCategoryTreeViewAsync(createdTree.Id, asusGamingLaptopsCategory.Id);
        laptops = list.FirstOrDefault(x => x.Id == laptopsCategory.Id);
        gamingLaptops =
            laptops.Children.FirstOrDefault(x => x.Id == gamingLaptopsCategory.Id);
        officeLaptops =
            laptops.Children.FirstOrDefault(x => x.Id == officeLaptopsCategory.Id);
        asusGamingLaptops =
            gamingLaptops?.Children.FirstOrDefault(x => x.Id == asusGamingLaptopsCategory.Id);

        asusGamingLaptops.Children.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task GetTreeViewAsync_CategoryNofFound()
    {
        (HierarchyViewModel createdTree, CategoryViewModel _, CategoryViewModel _,
            CategoryViewModel _, CategoryViewModel _,
            CategoryViewModel _) = await BuildTestTreeAsync();

        Func<Task> action = async () => await _eavService.GetCategoryTreeViewAsync(createdTree.Id, Guid.NewGuid());

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task GetSubcategoriesBranch_Success()
    {
        (HierarchyViewModel createdTree, CategoryViewModel laptopsCategory, CategoryViewModel gamingLaptopsCategory,
            _, _, _) = await BuildTestTreeAsync();
        await Task.Delay(ProjectionsUpdateDelay);

        var categoryPathValue = $"/{_laptopsCategoryMachineName}/{_gamingLaptopsCategoryMachineName}";
        ProjectionQueryResult<EntityInstanceViewModel> subcategories12 = await _eavService.QueryInstances(
            createdTree.EntityConfigurationId,
            new ProjectionQuery
            {
                Filters = new List<Filter>
                {
                    new("CategoryPaths.TreeId", FilterOperator.Equal, createdTree.Id),
                    new("CategoryPaths.Path", FilterOperator.StartsWithIgnoreCase, categoryPathValue)
                }
            }
        );

        subcategories12.TotalRecordsFound.Should().Be(2);
    }

    [TestMethod]
    public async Task GetSubcategories_Success()
    {
        (HierarchyViewModel createdTree, CategoryViewModel laptopsCategory,
                    CategoryViewModel gamingLaptopsCategory, CategoryViewModel officeLaptopsCategory,
                    CategoryViewModel asusGamingLaptops, CategoryViewModel _) = await BuildTestTreeAsync();

        var subcategories = await _eavService.GetSubcategories(createdTree.Id, null);
        subcategories.Count.Should().Be(1);

        subcategories = await _eavService.GetSubcategories(createdTree.Id, laptopsCategory.Id);
        subcategories.Count.Should().Be(2);

        subcategories = await _eavService.GetSubcategories(createdTree.Id, gamingLaptopsCategory.Id);
        subcategories.Count.Should().Be(1);

        subcategories = await _eavService.GetSubcategories(createdTree.Id, asusGamingLaptops.Id);
        subcategories.Count.Should().Be(1);

        subcategories = await _eavService.GetSubcategories(createdTree.Id, officeLaptopsCategory.Id);
        subcategories.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task GetSubcategories_TreeNotFound()
    {
        (HierarchyViewModel createdTree, CategoryViewModel _,
            CategoryViewModel _, CategoryViewModel _,
            CategoryViewModel _, CategoryViewModel _) = await BuildTestTreeAsync();

        Func<Task> action = async () => await _eavService.GetSubcategories(Guid.NewGuid());

        await action.Should().ThrowAsync<NotFoundException>().WithMessage("Category tree not found");
    }

    [TestMethod]
    public async Task GetSubcategories_ParentNotFound()
    {
        (HierarchyViewModel createdTree, CategoryViewModel _,
            CategoryViewModel _, CategoryViewModel _,
            CategoryViewModel _, CategoryViewModel _) = await BuildTestTreeAsync();

        var result = await _eavService.GetSubcategories(createdTree.Id, parentId: Guid.NewGuid());
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task MoveAndGetItemsFromCategory_Success()
    {
        (HierarchyViewModel createdTree, CategoryViewModel laptopsCategory, CategoryViewModel gamingLaptopsCategory,
                _, CategoryViewModel asusGamingLaptops, CategoryViewModel rogAsusGamingLaptops) =
            await BuildTestTreeAsync();

        EntityConfigurationCreateRequest itemEntityConfig =
            EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
        (EntityConfigurationViewModel? itemEntityConfiguration, _) = await _eavService.CreateEntityConfiguration(
            itemEntityConfig,
            CancellationToken.None
        );

        EntityInstanceCreateRequest itemInstanceRequest =
            EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(itemEntityConfiguration.Id);

        var (_, _) = await _eavService.CreateEntityInstance(itemInstanceRequest);

        (EntityInstanceViewModel createdItemInstance2, _) =
            await _eavService.CreateEntityInstance(itemInstanceRequest);
        (createdItemInstance2, _) = await _eavService.UpdateCategoryPath(createdItemInstance2.Id,
            createdItemInstance2.PartitionKey,
            createdTree.Id,
            asusGamingLaptops.Id,
            CancellationToken.None
        );

        (EntityInstanceViewModel createdItemInstance3, _) =
            await _eavService.CreateEntityInstance(itemInstanceRequest);
        (_, _) = await _eavService.UpdateCategoryPath(createdItemInstance3.Id,
            createdItemInstance2.PartitionKey,
            createdTree.Id,
            rogAsusGamingLaptops.Id,
            CancellationToken.None
        );

        await Task.Delay(ProjectionsUpdateDelay);

        var pathFilterValue121 = $"/{_laptopsCategoryMachineName}/{_gamingLaptopsCategoryMachineName}/{_asusGamingLaptopsCategoryMachineName}";


        ProjectionQueryResult<EntityInstanceViewModel> itemsFrom121 = await _eavService.QueryInstances(
            itemEntityConfiguration.Id,
            new ProjectionQuery
            {
                Filters = new List<Filter>
                {
                    new("CategoryPaths.TreeId", FilterOperator.Equal, createdTree.Id),
                    new("CategoryPaths.Path", FilterOperator.Equal, pathFilterValue121)
                }
            }
        );
        var pathFilterValue1211 =
            $"/{_laptopsCategoryMachineName}/{_gamingLaptopsCategoryMachineName}/{_asusGamingLaptopsCategoryMachineName}/{_rogAsusGamingLaptopsCategoryMachineName}";

        ProjectionQueryResult<EntityInstanceViewModel> itemsFrom1211 = await _eavService.QueryInstances(
            itemEntityConfiguration.Id,
            new ProjectionQuery
            {
                Filters = new List<Filter>
                {
                    new("CategoryPaths.Path", FilterOperator.StartsWithIgnoreCase, pathFilterValue1211)
                }
            }
        );

        itemsFrom121.Records.Count.Should().Be(1);
        itemsFrom1211.Records.Count.Should().Be(1);
    }
}
