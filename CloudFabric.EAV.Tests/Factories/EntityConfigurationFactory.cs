using System.Collections.Generic;
using System.Globalization;

using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;

namespace CloudFabric.EAV.Tests.Factories;

public static class EntityConfigurationFactory
{
    public static EntityConfigurationCreateRequest CreateBoardGameEntityConfigurationCreateRequest()
    {
        return new EntityConfigurationCreateRequest()
        {
            Name = new List<LocalizedStringCreateRequest>()
            {
                new LocalizedStringCreateRequest()
                {
                    CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                    String = "Board Game"
                },
                new LocalizedStringCreateRequest()
                {
                    CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                    String = "Настольная Игра"
                }
            },
            MachineName = "BoardGame",
            Attributes = new List<AttributeConfigurationCreateUpdateRequest>()
            {
                new LocalizedTextAttributeConfigurationCreateUpdateRequest()
                {
                    MachineName = "name",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Name"
                        },
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Название"
                        }
                    },
                },
                new LocalizedTextAttributeConfigurationCreateUpdateRequest()
                {
                    MachineName = "description",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Description"
                        },
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Описание"
                        }
                    },
                },
                new ArrayAttributeConfigurationCreateUpdateRequest()
                {
                    MachineName = "images",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Images"
                        },
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Изображения"
                        }
                    },
                    ItemsType = EavAttributeType.Image,
                    ItemsAttributeConfiguration = new ImageAttributeConfigurationCreateUpdateRequest()
                    {
                        ThumbnailsConfiguration = new List<ImageThumbnailDefinitionCreateUpdateRequest>()
                        {
                            new ImageThumbnailDefinitionCreateUpdateRequest()
                            {
                                MaxHeight = 400,
                                MaxWidth = 400
                            }
                        }
                    }
                },
                new NumberAttributeConfigurationCreateUpdateRequest()
                {
                    MachineName = "players_min",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Number of players min"
                        },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Количество игроков, от"
                        }
                    },
                    MinimumValue = 1,
                    IsRequired = true
                },
                new NumberAttributeConfigurationCreateUpdateRequest()
                {
                    MachineName = "players_max",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Number of players max"
                        },
                        new()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Количество игроков, до"
                        }
                    },
                    MaximumValue = 10,
                    IsRequired = true
                },
                new NumberAttributeConfigurationCreateUpdateRequest
                {
                    MachineName = "avg_time_mins",
                    Name = new List<LocalizedStringCreateRequest>
                    {
                        new LocalizedStringCreateRequest
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Average playtime in minutes"
                        },
                        new LocalizedStringCreateRequest
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Средняя продолжительность игры"
                        }
                    }
                }
            }
        };
    }

    public static EntityConfigurationCreateRequest CreateCarTireEntityConfigurationCreateRequest()
    {
        return new EntityConfigurationCreateRequest()
        {
            Name = new List<LocalizedStringCreateRequest>()
            {
                new LocalizedStringCreateRequest()
                {
                    CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                    String = "Tire"
                },
                new LocalizedStringCreateRequest()
                {
                    CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                    String = "Шина"
                }
            },
            MachineName = "CarTire",
            Attributes = new List<AttributeConfigurationCreateUpdateRequest>()
            {
                new LocalizedTextAttributeConfigurationCreateUpdateRequest()
                {
                    MachineName = "brand",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Brand"
                        },
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Бренд"
                        }
                    },
                },
                new NumberAttributeConfigurationCreateUpdateRequest()
                {
                    MachineName = "width",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Width"
                        },
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Ширина"
                        }
                    },
                    DefaultValue = 0,
                    Description = new List<LocalizedStringCreateRequest>
                    {
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Tire width"
                        },
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Ширина шины"
                        }
                    }
                },
                new NumberAttributeConfigurationCreateUpdateRequest()
                {
                    MachineName = "diameter",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Diameter"
                        },
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Диаметр"
                        }
                    },
                    DefaultValue = 0,
                    Description = new List<LocalizedStringCreateRequest>
                    {
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Tire diameter"
                        },
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Диаметр шины"
                        }
                    }
                },
                new NumberAttributeConfigurationCreateUpdateRequest()
                {
                    MachineName = "height",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Height"
                        },
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Высота"
                        }
                    },
                    DefaultValue = 0,
                    Description = new List<LocalizedStringCreateRequest>
                    {
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Tire height"
                        },
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Высота шины"
                        }
                    }
                },
                new ArrayAttributeConfigurationCreateUpdateRequest()
                {
                    MachineName = "vehiclemakes",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = "Vehicle makes"
                        },
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
                            String = "Марки автомобилей"
                        }
                    },
                    ItemsType = EavAttributeType.Text,
                    ItemsAttributeConfiguration = new TextAttributeConfigurationCreateUpdateRequest
                    {
                        DefaultValue = "",
                        Name = new List<LocalizedStringCreateRequest>
                        {
                            new LocalizedStringCreateRequest()
                            {
                                CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                                String = "Vehicle make"
                            },
                            new LocalizedStringCreateRequest()
                            {
                                CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID,
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