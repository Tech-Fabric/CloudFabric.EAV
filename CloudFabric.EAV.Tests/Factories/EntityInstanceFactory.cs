using System.Globalization;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Models.LocalEventSourcingPackages.RequestModels;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;

namespace CloudFabric.EAV.Tests.Factories;

public class EntityInstanceFactory
{
    public static CategoryInstanceCreateRequest CreateCategoryInstanceRequest(Guid entityConfigurationId, 
        string categoryPath, 
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
        return new CategoryInstanceCreateRequest()
        {
            EntityConfigurationId = entityConfigurationId,
            Attributes = attributeInstances,
            CategoryPath = categoryPath,
        };
    }
    public static EntityInstanceCreateRequest CreateValidTireEntityInstanceCreateRequest(Guid entityConfigurationId)
    {
        return new EntityInstanceCreateRequest()
        {
            EntityConfigurationId = entityConfigurationId,
            Attributes = new List<AttributeInstanceCreateUpdateRequest>()
            {
                new LocalizedTextAttributeInstanceCreateUpdateRequest()
                {
                    ConfigurationAttributeMachineName = "brand",
                    Value = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                            String = "Pirelli"
                        },
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("de-DE").LCID,
                            String = "Pirelli"
                        }
                    }
                },
                new NumberAttributeInstanceCreateUpdateRequest()
                {
                    ConfigurationAttributeMachineName = "width",
                    Value = 205
                },
                new NumberAttributeInstanceCreateUpdateRequest()
                {
                    ConfigurationAttributeMachineName = "height",
                    Value = 55
                },
                new NumberAttributeInstanceCreateUpdateRequest()
                {
                    ConfigurationAttributeMachineName = "diameter",
                    Value = 16
                }
            }
        };
    }
    public static EntityInstanceCreateRequest CreateValidBoardGameEntityInstanceCreateRequest(Guid entityConfigurationId)
    {
        return new EntityInstanceCreateRequest()
        {
            CategoryPath = "",
            EntityConfigurationId = entityConfigurationId,
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
                    From = DateTime.Today
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