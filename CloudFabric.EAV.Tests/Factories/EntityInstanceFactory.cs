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
            attributeInstances.Add(new NumberAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = $"category_attribute_{i}", Value = i
                }
            );
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

    public static string CreateValidBoardGameEntityInstanceCreateRequestJsonSingleLanguage(Guid entityConfigurationId)
    {
        return @"
            {
                ""entityConfigurationId"": """ + entityConfigurationId + @""",
                ""name"": ""Azul"",
                ""description"": ""In the game Azul, players take turns drafting colored tiles from suppliers to their player board. Later in the round, players score points based on how they've placed their tiles to decorate the palace. Extra points are scored for specific patterns and completing sets; wasted supplies harm the player's score. The player with the most points at the end of the game wins."",
                ""brand"": ""HobbyGames"",
                ""players_min"": 1,
                ""players_max"": 4,
                ""avg_time_mins"": 15,
                ""price"": 31.50,
                ""release_date"": ""2017"",
                ""images"": [
                    {
                        ""title"": ""Photo 1"",
                        ""url"": ""/images/photo1.jpg"",
                        ""alt"": ""A photo of Azul board game box""
                    },
                    {
                        ""title"": ""Azul Rulebook"",
                        ""url"": ""/images/rulebook.jpg"",
                        ""alt"": ""A photo of Azul board game rulebook, page 1""
                    }
                ]
            }
        ";
    }

    public static string CreateValidBoardGameEntityInstanceCreateRequestJsonMultiLanguage(Guid entityConfigurationId)
    {
        return @"
            {
                ""entityConfigurationId"": """ + entityConfigurationId + @""",
                ""name"": [
                    {
                        ""EN-us"": ""Azul"",
                        ""RU-ru"": ""Азул""
                    }
                ],
                ""description"": [
                    {
                         ""EN-us"": ""In the game Azul, players take turns drafting colored tiles from suppliers to their player board. Later in the round, players score points based on how they've placed their tiles to decorate the palace. Extra points are scored for specific patterns and completing sets; wasted supplies harm the player's score. The player with the most points at the end of the game wins."",
                         ""RU-ru"": ""Представьте, что вы попали в красивый дворец, все стены которого выложены изящной разноцветной плиткой. Вы понимаете, что не один мастер тут потрудился, это была целая команда художников и ремесленников, которая работала не покладая рук много лет, чтобы вы смогли сегодня восхититься красотой узоров. В ней нет и кусочка повторения, как будто бы в каждой плитке свой особый сюжет, пропитанный духом времени.""
                    }
                ],
                ""brand"": ""HobbyGames"",
                ""players_min"": 1,
                ""players_max"": 4,
                ""avg_time_mins"": 15,
                ""price"": 31.50,
                ""release_date"": ""2017"",
                ""images"": [
                    {{
                        ""title"": ""Photo 1"",
                        ""url"": ""/images/photo1.jpg"",
                        ""llt"": ""A photo of Azul board game box""
                    }},
                    {{
                        ""title"": ""Azul Rulebook"",
                        ""url"": ""/images/rulebook.jpg"",
                        ""alt"": ""A photo of Azul board game rulebook, page 1""
                    }}
                ]
            }}
        ";
    }

    public static EntityInstanceCreateRequest CreateValidBoardGameEntityInstanceCreateRequest(
        Guid entityConfigurationId)
    {
        return new EntityInstanceCreateRequest
        {
            EntityConfigurationId = entityConfigurationId,
            TenantId = Guid.NewGuid(),
            Attributes = new List<AttributeInstanceCreateUpdateRequest>
            {
                new LocalizedTextAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = "name",
                    Value = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Azul"
                        },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Азул"
                        }
                    }
                },
                new LocalizedTextAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = "description",
                    Value = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "BlahBlahBlah"
                        },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "БлаБлаБла"
                        }
                    }
                },
                new TextAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = "brand", Value = "HappyGames"
                },
                new NumberAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = "players_min", Value = 1
                },
                new NumberAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = "players_max", Value = 4
                },
                new NumberAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = "avg_time_mins", Value = 15
                },
                new NumberAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = "price", Value = 1000
                },
                new DateRangeAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = "release_date",
                    Value = new DateRangeAttributeInstanceValueRequest { From = DateTime.Today }
                },
                new ArrayAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = "images",
                    Items = new List<AttributeInstanceCreateUpdateRequest>
                    {
                        new ImageAttributeInstanceCreateUpdateRequest
                        {
                            Value = new ImageAttributeValueCreateUpdateRequest
                            {
                                Title = "Photo 1",
                                Url = "/images/photo1.jpg",
                                Alt = "A photo of Azul board game box"
                            }
                        },
                        new ImageAttributeInstanceCreateUpdateRequest
                        {
                            Value = new ImageAttributeValueCreateUpdateRequest
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
