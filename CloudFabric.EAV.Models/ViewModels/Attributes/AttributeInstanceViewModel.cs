﻿using CloudFabric.EAV.Json.Utilities;

using System.Text.Json.Serialization;

namespace CloudFabric.EAV.Service.Models.ViewModels.Attributes
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeInstanceViewModel>))]
    public class AttributeInstanceViewModel
    {
    }
}