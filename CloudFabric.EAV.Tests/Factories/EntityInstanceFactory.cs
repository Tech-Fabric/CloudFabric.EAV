using System.Globalization;

using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;

namespace CloudFabric.EAV.Tests.Factories;

public class EntityInstanceFactory
{
    public static CategoryInstanceCreateRequest CreateCategoryInstanceRequest(Guid entityConfigurationId,
        Guid treeId,
        Guid? parentId,
        Guid? tenantId,
        int attributeIndexFrom = 0,
        int attributeIndexTo = 1)
    {
        var attributeInstances = new List<AttributeInstanceCreateUpdateRequest>();
        for (var i = attributeIndexFrom; i < attributeIndexTo; i++)
        {
            attributeInstances.Add(new NumberAttributeInstanceCreateUpdateRequest()
            {
                ConfigurationAttributeMachineName = $"category_attribute_{i}",
                Value = i
            });
        }
        return new CategoryInstanceCreateRequest
        {
            CategoryConfigurationId = entityConfigurationId,
            Attributes = attributeInstances,
            ParentId = parentId,
            TenantId = tenantId,
            CategoryTreeId = treeId
        };
    }

    public static EntityInstanceCreateRequest CreateValidBoardGameEntityInstanceCreateRequest(Guid entityConfigurationId)
    {
        return new EntityInstanceCreateRequest()
        {
            EntityConfigurationId = entityConfigurationId,
            TenantId = Guid.NewGuid(),
            Attributes = new List<AttributeInstanceCreateUpdateRequest>()
            {
                new LocalizedTextAttributeInstanceCreateUpdateRequest()
                {
                    ConfigurationAttributeMachineName = "name",
                    Value = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Azul"
                        },
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Азул"
                        }
                    },
                },
                new LocalizedTextAttributeInstanceCreateUpdateRequest()
                {
                    ConfigurationAttributeMachineName = "description",
                    Value = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "BlahBlahBlah"
                        },
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "БлаБлаБла"
                        }
                    },
                },
                new TextAttributeInstanceCreateUpdateRequest()
                {
                    ConfigurationAttributeMachineName = "brand",
                    Value = "HappyGames"
                },
                new NumberAttributeInstanceCreateUpdateRequest()
                {
                    ConfigurationAttributeMachineName = "players_min",
                    Value = 1
                },
                new NumberAttributeInstanceCreateUpdateRequest()
                {
                    ConfigurationAttributeMachineName = "players_max",
                    Value = 4
                },
                new NumberAttributeInstanceCreateUpdateRequest()
                {
                    ConfigurationAttributeMachineName = "avg_time_mins",
                    Value = 15
                },
                new NumberAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = "price",
                    Value = 1000
                },
                new DateRangeAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = "release_date",
                    Value = new DateRangeAttributeInstanceValueRequest
                    {
                        From = DateTime.Today
                    }
                },
                new ArrayAttributeInstanceCreateUpdateRequest()
                {
                    ConfigurationAttributeMachineName = "images",
                    Items = new List<AttributeInstanceCreateUpdateRequest>()
                    {
                        new ImageAttributeInstanceCreateUpdateRequest()
                        {
                            Value = new ImageAttributeValueCreateUpdateRequest()
                            {
                                Title = "Photo 1",
                                Url = "/images/photo1.jpg",
                                Alt = "A photo of Azul board game box"
                            }
                        },
                        new ImageAttributeInstanceCreateUpdateRequest()
                        {
                            Value = new ImageAttributeValueCreateUpdateRequest()
                            {
                                Title = "Azul Rulebook",
                                Url = "/images/rulebook.jpg",
                                Alt = "A photo of Azul board game rulebook, page 1"
                            }
                        }
                    }
                }
            }
        };
    }
}
