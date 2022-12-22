using System.Globalization;

using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;

namespace CloudFabric.EAV.Tests.Factories;

public class EntityInstanceFactory
{
    public static EntityInstanceCreateRequest CreateValidBoardGameEntityInstanceCreateRequest(Guid entityConfigurationId)
    {
        return new EntityInstanceCreateRequest()
        {
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
                new DateRangeAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = "release_date",
                    From = DateTime.Today
                },
                new NumberAttributeInstanceCreateUpdateRequest
                {
                    ConfigurationAttributeMachineName = "price",
                    Value = 1000
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