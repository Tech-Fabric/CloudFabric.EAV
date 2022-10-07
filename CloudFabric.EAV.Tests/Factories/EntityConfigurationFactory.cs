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
                new LocalizedStringCreateRequest() { CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID, String = "Board Game" },
                new LocalizedStringCreateRequest() { CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID, String = "Настольная Игра" }
            },
            MachineName = "BoardGame",
            Attributes = new List<AttributeConfigurationCreateUpdateRequest>()
            {
                new LocalizedTextAttributeConfigurationCreateUpdateRequest() {
                    MachineName = "name",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest() { CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID, String = "Name" },
                        new LocalizedStringCreateRequest() { CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID, String = "Название" }
                    },
                },
                new LocalizedTextAttributeConfigurationCreateUpdateRequest() {
                    MachineName = "description",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest() { CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID, String = "Description" },
                        new LocalizedStringCreateRequest() { CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID, String = "Описание" }
                    },
                },
                new ArrayAttributeConfigurationCreateUpdateRequest() {
                    MachineName = "images",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest() { CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID, String = "Images" },
                        new LocalizedStringCreateRequest() { CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID, String = "Изображения" }
                    },
                    ItemsType = EavAttributeType.Image,
                    ItemsAttributeConfiguration = new ImageAttributeConfigurationCreateUpdateRequest() {
                        ThumbnailsConfiguration = new List<ImageThumbnailDefinitionCreateUpdateRequest>() {
                            new ImageThumbnailDefinitionCreateUpdateRequest() { MaxHeight = 400, MaxWidth = 400 }
                        }
                    }
                },
                new NumberAttributeConfigurationCreateUpdateRequest() {
                    MachineName = "players_min",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest() { CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID, String = "Number of players min" },
                        new LocalizedStringCreateRequest() { CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID, String = "Количество игроков, от" }
                    },
                },
                new NumberAttributeConfigurationCreateUpdateRequest() {
                    MachineName = "players_max",
                    Name = new List<LocalizedStringCreateRequest>()
                    {
                        new LocalizedStringCreateRequest() { CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID, String = "Number of players max" },
                        new LocalizedStringCreateRequest() { CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID, String = "Количество игроков, до" }
                    },
                }
            }
        };
    }
}