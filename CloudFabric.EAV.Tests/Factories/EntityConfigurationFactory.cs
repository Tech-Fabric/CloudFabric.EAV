using System.Globalization;

using Castle.Components.DictionaryAdapter;

using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;

namespace CloudFabric.EAV.Tests.Factories;

public static class EntityConfigurationFactory
{
    public static EntityConfigurationCreateRequest CreateBoardGameCategoryConfigurationCreateRequest(
        int attributeIndexFrom = 0, int attributeIndexTo = 1
    )
    {
        var tenantId = Guid.NewGuid();
        var attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>();
        for (var i = attributeIndexFrom; i < attributeIndexTo; i++)
        {
            attributes.Add(new NumberAttributeConfigurationCreateUpdateRequest
                {
                    DefaultValue = i,
                    Description = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId =
                                CultureInfo.GetCultureInfo("en-US").LCID,
                            String = $"Description {i}"
                        }
                    },
                    IsRequired = false,
                    MachineName = $"category_attribute_{i}",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId =
                                CultureInfo.GetCultureInfo("en-US").LCID,
                            String = $"Category {i}"
                        }
                    },
                    NumberType = NumberAttributeType.Integer
                }
            );
        }

        return new EntityConfigurationCreateRequest
        {
            TenantId = tenantId,
            Attributes = attributes,
            MachineName = $"BoardGameCategory_{attributeIndexFrom}_{attributeIndexTo}",
            Name = new List<LocalizedStringCreateRequest>
            {
                new()
                {
                    CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                    String = $"Board Game Category {attributeIndexFrom} - {attributeIndexTo}"
                }
            }
        };
    }

    public static EntityConfigurationCreateRequest CreateBoardGameEntityConfigurationCreateRequest()
    {
        var tenantId = Guid.NewGuid();
        return new EntityConfigurationCreateRequest
        {
            Name =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID, String = "Board Game" },
                    new() { CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID, String = "Настольная Игра" }
                },
            MachineName = "BoardGame",
            TenantId = tenantId,
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>
            {
                new LocalizedTextAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "name",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new() { CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID, String = "Name" },
                        new() { CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID, String = "Название" }
                    },
                    TenantId = tenantId
                },
                new LocalizedTextAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "description",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID, String = "Description"
                        },
                        new() { CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID, String = "Описание" }
                    },
                    TenantId = tenantId
                },
                new TextAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "brand",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new() { CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID, String = "Brand" },
                        new() { CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID, String = "Бренд" }
                    },
                    IsRequired = true,
                    IsSearchable = true,
                    MaxLength = 100,
                    DefaultValue = "-",
                    TenantId = tenantId
                },
                new NumberAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "players_min",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                            String = "Number of players min"
                        },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID,
                            String = "Количество игроков, от"
                        }
                    },
                    DefaultValue = 1,
                    MinimumValue = 1,
                    IsRequired = true,
                    TenantId = tenantId
                },
                new NumberAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "players_max",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                            String = "Number of players max"
                        },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID,
                            String = "Количество игроков, до"
                        }
                    },
                    DefaultValue = 10,
                    MaximumValue = 10,
                    IsRequired = true,
                    TenantId = tenantId
                },
                new NumberAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "avg_time_mins",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                            String = "Average playtime in minutes"
                        },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID,
                            String = "Средняя продолжительность игры"
                        }
                    },
                    TenantId = tenantId
                },
                new NumberAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "price",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new() { CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID, String = "Price" }
                    },
                    DefaultValue = 1,
                    MinimumValue = 1,
                    NumberType = NumberAttributeType.Decimal,
                    IsRequired = true,
                    TenantId = tenantId
                },
                new DateRangeAttributeConfigurationUpdateRequest
                {
                    MachineName = "release_date",
                    Name = new EditableList<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                            String = "Release date"
                        }
                    },
                    TenantId = tenantId,
                    DateRangeAttributeType = DateRangeAttributeType.SingleDate
                },
                new ArrayAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "images",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new() { CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID, String = "Images" },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID, String = "Изображения"
                        }
                    },
                    ItemsType = EavAttributeType.Image,
                    ItemsAttributeConfiguration = new ImageAttributeConfigurationCreateUpdateRequest
                    {
                        ThumbnailsConfiguration = new List<ImageThumbnailDefinitionCreateUpdateRequest>
                        {
                            new() { MaxHeight = 400, MaxWidth = 400 }
                        }
                    },
                    TenantId = tenantId
                }
                // new ValueFromListAttributeConfigurationCreateUpdateRequest
                // {
                //     MachineName = "version",
                //     Name = new List<LocalizedStringCreateRequest>
                //     {
                //         new LocalizedStringCreateRequest
                //         {
                //             CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                //             String = "Version"
                //         }
                //     },
                //     AttributeMachineNameToAffect = "price",
                //     ValuesList = new List<ValueFromListOptionCreateUpdateRequest>
                //     {
                //         new ValueFromListOptionCreateUpdateRequest("EU", "eu", 100),
                //         new ValueFromListOptionCreateUpdateRequest("Extra", "extra", 500)
                //     },
                //     TenantId = tenantId
                // }
            }
        };
    }

    public static EntityConfigurationCreateRequest CreateCarTireEntityConfigurationCreateRequest()
    {
        return new EntityConfigurationCreateRequest
        {
            Name =
                new List<LocalizedStringCreateRequest>
                {
                    new() { CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID, String = "Tire" },
                    new() { CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID, String = "Шина" }
                },
            MachineName = "CarTire",
            Attributes = new List<EntityAttributeConfigurationCreateUpdateRequest>
            {
                new LocalizedTextAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "brand",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID, String = "Brand"
                        },
                        new() { CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID, String = "Бренд" }
                    }
                },
                new NumberAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "width",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID, String = "Width"
                        },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID, String = "Ширина"
                        }
                    },
                    DefaultValue = 0,
                    Description = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                            String = "Tire width"
                        },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID,
                            String = "Ширина шины"
                        }
                    }
                },
                new NumberAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "diameter",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                            String = "Diameter"
                        },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID, String = "Диаметр"
                        }
                    },
                    DefaultValue = 0,
                    Description = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                            String = "Tire diameter"
                        },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID,
                            String = "Диаметр шины"
                        }
                    }
                },
                new NumberAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "height",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID, String = "Height"
                        },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID, String = "Высота"
                        }
                    },
                    DefaultValue = 0,
                    Description = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                            String = "Tire height"
                        },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID,
                            String = "Высота шины"
                        }
                    }
                },
                new ArrayAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "vehiclemakes",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                            String = "Vehicle makes"
                        },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID,
                            String = "Марки автомобилей"
                        }
                    },
                    ItemsType = EavAttributeType.Text,
                    ItemsAttributeConfiguration = new TextAttributeConfigurationCreateUpdateRequest
                    {
                        DefaultValue = "",
                        Name = new List<LocalizedStringCreateRequest>
                        {
                            new()
                            {
                                CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                                String = "Vehicle make"
                            },
                            new()
                            {
                                CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID,
                                String = "Марка автомобиля"
                            }
                        },
                        MachineName = "vehiclemake"
                    }
                }
            }
        };
    }
}
