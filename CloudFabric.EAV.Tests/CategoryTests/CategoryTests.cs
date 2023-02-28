using System.Reflection;

using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Service;
using CloudFabric.EAV.Tests.Factories;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Persistence;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AsyncConverter.AsyncMethodNamingHighlighting
// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace CloudFabric.EAV.Tests
{
    public abstract class CategoryTests: BaseQueryTests
    {
        private async Task<(HierarchyViewModel tree,
            CategoryViewModel laptopsCategory,
            CategoryViewModel gamingLaptopsCategory,
            CategoryViewModel officeLaptopsCategory,
            CategoryViewModel asusGamingLaptopsCategory,
            CategoryViewModel rogAsusGamingLaptopsCategory)> BuildTestTreeAsync()
        {
            // Create config for categories
            var categoryConfigurationCreateRequest = EntityConfigurationFactory.CreateBoardGameCategoryConfigurationCreateRequest(0, 9);
            (EntityConfigurationViewModel? categoryConfiguration, _) = await _eavService.CreateEntityConfiguration(
                categoryConfigurationCreateRequest,
                CancellationToken.None
            );

            // Create a tree
            var treeRequest = new CategoryTreeCreateRequest()
            {
                MachineName = "Main",
                EntityConfigurationId = categoryConfiguration!.Id,
            };

            var (createdTree, _) = await _eavService.CreateCategoryTreeAsync(treeRequest, categoryConfigurationCreateRequest.TenantId, CancellationToken.None);

            var categoryInstanceRequest =
                EntityInstanceFactory.CreateCategoryInstanceRequest(categoryConfiguration.Id, createdTree.Id, null, categoryConfigurationCreateRequest.TenantId, 0, 9);

            var (laptopsCategory, validationErrors) =
                await _eavService.CreateCategoryInstanceAsync(categoryInstanceRequest);

            categoryInstanceRequest.ParentId = laptopsCategory.Id;
            var (gamingLaptopsCategory, _) =
                await _eavService.CreateCategoryInstanceAsync(categoryInstanceRequest);

            categoryInstanceRequest.ParentId = laptopsCategory.Id;
            var (officeLaptopsCategory, _) =
                await _eavService.CreateCategoryInstanceAsync(categoryInstanceRequest);

            categoryInstanceRequest.ParentId = gamingLaptopsCategory.Id;
            var (asusGamingLaptopsCategory, _) =
                await _eavService.CreateCategoryInstanceAsync(categoryInstanceRequest);

            categoryInstanceRequest.ParentId = asusGamingLaptopsCategory.Id;
            var (rogAsusGamingLaptopsCategory, _) =
                await _eavService.CreateCategoryInstanceAsync(categoryInstanceRequest);

            await Task.Delay(ProjectionsUpdateDelay);
            
            return (createdTree, laptopsCategory, gamingLaptopsCategory, officeLaptopsCategory, asusGamingLaptopsCategory, rogAsusGamingLaptopsCategory);
        }

        [TestMethod]
        public async Task CreateCategory_Success()
        {
            var (_, laptopsCategory, gamingLaptopsCategory, _, _, _) = await BuildTestTreeAsync();

            laptopsCategory.Id.Should().NotBeEmpty();
            laptopsCategory.Attributes.Count.Should().Be(9);
            laptopsCategory.TenantId.Should().NotBeNull();
            gamingLaptopsCategory.CategoryPaths.Should().Contain(x => x.Path.Contains(laptopsCategory.Id.ToString()));
        }

        [TestMethod]
        public async Task GetTreeViewAsync()
        {
            var (createdTree, laptopsCategory, gamingLaptopsCategory, officeLaptopsCategory, asusGamingLaptopsCategory, rogAsusGamingLaptopsCategory) = await BuildTestTreeAsync();

            var list = await _eavService.GetCategoryTreeViewAsync(createdTree.Id, CancellationToken.None);

            var laptops = list.FirstOrDefault(x => x.Id == laptopsCategory.Id);
            laptops.Should().NotBeNull();
            laptops.Children.Should().Contain(x => x.Id == officeLaptopsCategory.Id);
            var gamingLaptops = laptops.Children.FirstOrDefault(x => x.Id == gamingLaptopsCategory.Id);
            var officeLaptops = laptops.Children.FirstOrDefault(x => x.Id == officeLaptopsCategory.Id);
            gamingLaptops.Should().NotBeNull();
            officeLaptops.Should().NotBeNull();
            var asusGamingLaptops = gamingLaptops?.Children.FirstOrDefault(x => x.Id == asusGamingLaptopsCategory.Id);
            asusGamingLaptops.Should().NotBeNull();
            var rogAsusGamingLaptops = asusGamingLaptops?.Children.FirstOrDefault(x => x.Id == rogAsusGamingLaptopsCategory.Id);
            rogAsusGamingLaptops.Should().NotBeNull();
        }

        [TestMethod]
        public async Task GetSubcategories_Success()
        {
            var (createdTree, laptopsCategory, gamingLaptopsCategory, officeLaptopsCategory, asusGamingLaptopsCategory, rogAsusGamingLaptopsCategory) = await BuildTestTreeAsync();
            await Task.Delay(ProjectionsUpdateDelay);

            var categoryPathValue = $"/{laptopsCategory.Id}/{gamingLaptopsCategory.Id}";
            var subcategories12 = await _eavService.QueryInstances(createdTree.EntityConfigurationId,
                new ProjectionQuery()
                {
                    Filters = new List<Filter>()
                    {
                        new Filter("CategoryPaths.TreeId", FilterOperator.Equal, createdTree.Id),
                        new Filter("CategoryPaths.Path", FilterOperator.StartsWithIgnoreCase, categoryPathValue)
                    }
                });

            subcategories12.TotalRecordsFound.Should().Be(2);
        }

        [TestMethod]
        public async Task MoveAndGetItemsFromCategory_Success()
        {
            var (createdTree, laptopsCategory, gamingLaptopsCategory, officeLaptops, asusGamingLaptops, rogAsusGamingLaptops) = await BuildTestTreeAsync();

            var itemEntityConfig = EntityConfigurationFactory.CreateBoardGameEntityConfigurationCreateRequest();
            var (itemEntityConfiguration, _) = await _eavService.CreateEntityConfiguration(
                itemEntityConfig,
                CancellationToken.None
            );

            var itemInstanceRequest = EntityInstanceFactory.CreateValidBoardGameEntityInstanceCreateRequest(itemEntityConfiguration.Id);
            var (createdItemInstance1, _) = await _eavService.CreateEntityInstance(itemInstanceRequest);

            var (createdItemInstance2, _) = await _eavService.CreateEntityInstance(itemInstanceRequest);
            (createdItemInstance2, _) = await _eavService.UpdateCategoryPathAsync(createdItemInstance2.Id, createdItemInstance2.PartitionKey, createdTree.Id, asusGamingLaptops.Id, CancellationToken.None);

            var (createdItemInstance3, _) = await _eavService.CreateEntityInstance(itemInstanceRequest);
            (_, _) = await _eavService.UpdateCategoryPathAsync(createdItemInstance3.Id, createdItemInstance2.PartitionKey, createdTree.Id, rogAsusGamingLaptops.Id, CancellationToken.None);

            await Task.Delay(ProjectionsUpdateDelay);

            var pathFilterValue121 = $"/{laptopsCategory.Id}/{gamingLaptopsCategory.Id}/{asusGamingLaptops.Id}";


            var itemsFrom121 = await _eavService.QueryInstances(itemEntityConfiguration.Id,
                new ProjectionQuery()
                {
                    Filters = new List<Filter>()
                    {
                        new Filter("CategoryPaths.TreeId", FilterOperator.Equal, createdTree.Id),
                        new Filter("CategoryPaths.Path", FilterOperator.Equal, pathFilterValue121)
                    }

                });
            var pathFilterValue1211 = $"/{laptopsCategory.Id}/{gamingLaptopsCategory.Id}/{asusGamingLaptops.Id}/{rogAsusGamingLaptops.Id}";

            var itemsFrom1211 = await _eavService.QueryInstances(itemEntityConfiguration.Id,
                new ProjectionQuery()
                {
                    Filters = new List<Filter>()
                    {
                        new Filter("CategoryPaths.Path", FilterOperator.StartsWithIgnoreCase, pathFilterValue1211)
                    }
                });

            itemsFrom121.Records.Count.Should().Be(1);
            itemsFrom1211.Records.Count.Should().Be(1);
        }
    }
}