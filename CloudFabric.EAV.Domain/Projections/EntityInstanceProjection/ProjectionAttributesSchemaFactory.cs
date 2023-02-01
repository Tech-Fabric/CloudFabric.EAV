using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.Projections;

namespace CloudFabric.EAV.Domain.Projections.EntityInstanceProjection
{
    public static class ProjectionAttributesSchemaFactory
    {
        public static ProjectionDocumentPropertySchema GetTextAttributeSchema(AttributeConfiguration attributeConfiguration)
        {
            var attribute = attributeConfiguration as TextAttributeConfiguration;

            if (attribute == null)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            return new ProjectionDocumentPropertySchema
            {
                PropertyName = attributeConfiguration.MachineName,
                PropertyType = GetPropertyType(attributeConfiguration.ValueType).GetValueOrDefault(),
                IsSearchable = attribute.IsSearchable,
                IsRetrievable = true,
                IsFilterable = true,
                IsSortable = true
            };
        }

        public static ProjectionDocumentPropertySchema GetNumberAttributeSchema(AttributeConfiguration attributeConfiguration)
        {
            if (attributeConfiguration is not NumberAttributeConfiguration)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            return new ProjectionDocumentPropertySchema
            {
                PropertyName = attributeConfiguration.MachineName,
                PropertyType = GetPropertyType(attributeConfiguration.ValueType).GetValueOrDefault(),
                IsRetrievable = true,
                IsFilterable = true,
                IsSortable = true
            };
        }

        public static ProjectionDocumentPropertySchema GetBooleanAttributeSchema(AttributeConfiguration attributeConfiguration)
        {
            if (attributeConfiguration is not BooleanAttributeConfiguration)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            return new ProjectionDocumentPropertySchema
            {
                PropertyName = attributeConfiguration.MachineName,
                PropertyType = GetPropertyType(attributeConfiguration.ValueType).GetValueOrDefault(),
                IsRetrievable = true,
                IsFilterable = true,
                IsSortable = true
            };
        }

        public static ProjectionDocumentPropertySchema GetHtmlTextAttributeSchema(AttributeConfiguration attributeConfiguration)
        {
            if (attributeConfiguration is not HtmlTextAttributeConfiguration)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            return new ProjectionDocumentPropertySchema
            {
                PropertyName = attributeConfiguration.MachineName,
                PropertyType = GetPropertyType(attributeConfiguration.ValueType).GetValueOrDefault(),
                IsRetrievable = true,
                IsFilterable = true,
                IsSortable = true
            };
        }

        public static ProjectionDocumentPropertySchema GetEntityReferenceAttributeSchema(AttributeConfiguration attributeConfiguration)
        {
            if (attributeConfiguration is not EntityReferenceAttributeConfiguration)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            return new ProjectionDocumentPropertySchema
            {
                PropertyName = attributeConfiguration.MachineName,
                PropertyType = GetPropertyType(attributeConfiguration.ValueType).GetValueOrDefault(),
                IsRetrievable = true,
                IsFilterable = true
            };
        }

        public static ProjectionDocumentPropertySchema GetDateAttributeSchema(AttributeConfiguration attributeConfiguration)
        {
            if (attributeConfiguration is not DateRangeAttributeConfiguration)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            return new ProjectionDocumentPropertySchema
            {
                PropertyName = attributeConfiguration.MachineName,
                PropertyType = GetPropertyType(attributeConfiguration.ValueType).GetValueOrDefault(),
                IsRetrievable = true,
                IsFilterable = true,
                IsSortable = true,
                IsNestedObject = true,
                NestedObjectProperties = GetDateAttributeNestedProperties()
            };
        }

        public static ProjectionDocumentPropertySchema GetLocalizedTextAttributeSchema(AttributeConfiguration attributeConfiguration)
        {
            if (attributeConfiguration is not LocalizedTextAttributeConfiguration)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            return new ProjectionDocumentPropertySchema
            {
                PropertyName = attributeConfiguration.MachineName,
                PropertyType = GetPropertyType(attributeConfiguration.ValueType).GetValueOrDefault(),
                IsRetrievable = true,
                IsFilterable = true,
                IsSortable = true,
                IsNestedArray = true,
                ArrayElementType = Type.GetTypeCode(typeof(LocalizedString)),
                NestedObjectProperties = GetLocalizedTextAttributeNestedProperties()
            };
        }

        public static ProjectionDocumentPropertySchema GetValueFromListAttributeSchema(AttributeConfiguration attributeConfiguration)
        {
            if (attributeConfiguration is not ValueFromListAttributeConfiguration)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            return new ProjectionDocumentPropertySchema
            {
                PropertyName = attributeConfiguration.MachineName,
                PropertyType = GetPropertyType(attributeConfiguration.ValueType).GetValueOrDefault(),
                IsRetrievable = true,
                IsFilterable = true,
                IsSortable = true
            };
        }

        public static ProjectionDocumentPropertySchema GetImageAttributeSchema(AttributeConfiguration attributeConfiguration)
        {
            if (attributeConfiguration is not ImageAttributeConfiguration)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            return new ProjectionDocumentPropertySchema
            {
                PropertyName = attributeConfiguration.MachineName,
                PropertyType = GetPropertyType(attributeConfiguration.ValueType).GetValueOrDefault(),
                IsRetrievable = true,
                IsNestedObject = true,
                NestedObjectProperties = GetImageAttributeNestedProperties()
            };
        }

        public static ProjectionDocumentPropertySchema GetFileAttributeSchema(AttributeConfiguration attributeConfiguration)
        {
            if (attributeConfiguration is not FileAttributeConfiguration)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            return new ProjectionDocumentPropertySchema
            {
                PropertyName = attributeConfiguration.MachineName,
                PropertyType = GetPropertyType(attributeConfiguration.ValueType).GetValueOrDefault(),
                IsRetrievable = true,
                IsNestedObject = true,
                NestedObjectProperties = GetFileAttributeNestedProperties()
            };
        }

        public static ProjectionDocumentPropertySchema GetArrayAttributeSchema(AttributeConfiguration attributeConfiguration)
        {
            var attribute = attributeConfiguration as ArrayAttributeConfiguration;

            if (attribute == null)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            TypeCode arrayElementType = GetPropertyType(attribute.ItemsType).GetValueOrDefault();

            // for array elements we need only nested objects in property schema
            List<ProjectionDocumentPropertySchema>? nestedProperties = attribute.ItemsType switch
            {
                EavAttributeType.Number => null,
                EavAttributeType.Text => null,
                EavAttributeType.Boolean => null,
                EavAttributeType.HtmlText => null,
                EavAttributeType.EntityReference => null,
                EavAttributeType.ValueFromList => null,
                EavAttributeType.LocalizedText => GetLocalizedTextAttributeNestedProperties(),
                EavAttributeType.DateRange => GetDateAttributeNestedProperties(),
                EavAttributeType.Image => GetImageAttributeNestedProperties(),
                EavAttributeType.File => GetFileAttributeNestedProperties(),
                _ => throw new Exception($"EavAttributeType {attribute.ItemsType} is not supported as an array element.")
            };

            return new ProjectionDocumentPropertySchema
            {
                PropertyName = attributeConfiguration.MachineName,
                PropertyType = GetPropertyType(attributeConfiguration.ValueType).GetValueOrDefault(),
                IsRetrievable = true,
                IsNestedArray = true,
                ArrayElementType = arrayElementType,
                // TODO: for simple configs we will not have nested object properties
                // for complex - take nested object properties array
                NestedObjectProperties = (arrayElementType == TypeCode.Object)
                    ? nestedProperties
                    : null
            };
        }

        public static ProjectionDocumentPropertySchema GetSerialAttributeShema(AttributeConfiguration attributeConfiguration)
        {
            var attribute = attributeConfiguration as SerialAttributeConfiguration;

            if (attribute == null)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            return new ProjectionDocumentPropertySchema
            {
                PropertyName = attributeConfiguration.MachineName,
                PropertyType = GetPropertyType(attributeConfiguration.ValueType).GetValueOrDefault(),
                IsRetrievable = true,
                IsFilterable = true,
                IsSortable = true
            };
        }

        #region Nested Properties

        private static List<ProjectionDocumentPropertySchema> GetDateAttributeNestedProperties()
        {
            return new List<ProjectionDocumentPropertySchema>
            {
                new ProjectionDocumentPropertySchema
                {
                    PropertyName = nameof(DateRangeAttributeInstanceValue.From),
                    PropertyType = Type.GetTypeCode(
                        typeof(DateRangeAttributeInstanceValue)
                            .GetProperty(nameof(DateRangeAttributeInstanceValue.From))
                            ?.GetType()
                    ),
                    IsRetrievable = true,
                    IsFilterable = true,
                    IsSortable = true,
                    IsFacetable = true
                },
                new ProjectionDocumentPropertySchema
                {
                    PropertyName = nameof(DateRangeAttributeInstanceValue.To),
                    PropertyType = Type.GetTypeCode(
                        typeof(DateRangeAttributeInstanceValue)
                            .GetProperty(nameof(DateRangeAttributeInstanceValue.To))
                            ?.GetType()
                    ),
                    IsRetrievable = true,
                    IsFilterable = true,
                    IsSortable = true
                }
            };
        }

        private static List<ProjectionDocumentPropertySchema> GetLocalizedTextAttributeNestedProperties()
        {
            return new List<ProjectionDocumentPropertySchema>
            {
                new ProjectionDocumentPropertySchema
                {
                    PropertyName = nameof(LocalizedString.CultureInfoId),
                    PropertyType = Type.GetTypeCode(
                        typeof(LocalizedString)
                            .GetProperty(nameof(LocalizedString.CultureInfoId))
                            ?.PropertyType
                    ),
                    IsRetrievable = true,
                    IsFilterable = false,
                    IsSortable = true,
                },
                new ProjectionDocumentPropertySchema
                {
                    PropertyName = nameof(LocalizedString.String),
                    PropertyType = Type.GetTypeCode(
                        typeof(LocalizedString)
                            .GetProperty(nameof(LocalizedString.String))
                            ?.PropertyType
                    ),
                    IsRetrievable = true,
                    IsFilterable = false,
                    IsSortable = true
                }
            };
        }

        private static List<ProjectionDocumentPropertySchema> GetImageAttributeNestedProperties()
        {
            return new List<ProjectionDocumentPropertySchema>
            {
                new ProjectionDocumentPropertySchema
                {
                    PropertyName = nameof(ImageAttributeInstance.Value.Url),
                    PropertyType = Type.GetTypeCode(
                        typeof(ImageAttributeInstance)
                            .GetProperty(nameof(ImageAttributeInstance.Value))
                            ?.PropertyType
                            .GetProperty(nameof(ImageAttributeInstance.Value.Url))
                            ?.PropertyType
                    ),
                    IsRetrievable = true
                },
                new ProjectionDocumentPropertySchema
                {
                    PropertyName = nameof(ImageAttributeInstance.Value.Title),
                    PropertyType = Type.GetTypeCode(
                        typeof(ImageAttributeInstance)
                            .GetProperty(nameof(ImageAttributeInstance.Value))
                            ?.PropertyType
                            .GetProperty(nameof(ImageAttributeInstance.Value.Title))
                            ?.PropertyType
                    ),
                    IsRetrievable = true
                },
                new ProjectionDocumentPropertySchema
                {
                    PropertyName = nameof(ImageAttributeInstance.Value.Alt),
                    PropertyType = Type.GetTypeCode(
                        typeof(ImageAttributeInstance)
                            .GetProperty(nameof(ImageAttributeInstance.Value))
                            ?.PropertyType
                            .GetProperty(nameof(ImageAttributeInstance.Value.Alt))
                            ?.PropertyType
                    ),
                    IsRetrievable = true
                },
            };
        }

        private static List<ProjectionDocumentPropertySchema> GetFileAttributeNestedProperties()
        {
            return new List<ProjectionDocumentPropertySchema>
            {
                new ProjectionDocumentPropertySchema
                {
                    PropertyName = nameof(FileAttributeInstance.Value.Url),
                    PropertyType = Type.GetTypeCode(
                        typeof(FileAttributeInstance)
                            .GetProperty(nameof(FileAttributeInstance.Value))
                            ?.PropertyType
                            .GetProperty(nameof(FileAttributeInstance.Value.Url))
                            ?.PropertyType
                    ),
                    IsRetrievable = true
                },
                new ProjectionDocumentPropertySchema
                {
                    PropertyName = nameof(FileAttributeInstance.Value.Filename),
                    PropertyType = Type.GetTypeCode(
                        typeof(FileAttributeInstance)
                            .GetProperty(nameof(FileAttributeInstance.Value))
                            ?.PropertyType
                            .GetProperty(nameof(FileAttributeInstance.Value.Filename))
                            ?.PropertyType
                    ),
                    IsRetrievable = true
                }
            };
        }

        #endregion

        private static TypeCode? GetPropertyType(EavAttributeType valueType)
        {
            TypeCode? propertyType;

            switch (valueType)
            {
                case EavAttributeType.Number:
                    propertyType = TypeCode.Decimal;
                    break;
                case EavAttributeType.Text:
                case EavAttributeType.HtmlText:
                case EavAttributeType.EntityReference:
                case EavAttributeType.ValueFromList:
                    propertyType = TypeCode.String;
                    break;
                case EavAttributeType.Boolean:
                    propertyType = TypeCode.Boolean;
                    break;
                case EavAttributeType.LocalizedText:
                case EavAttributeType.DateRange:
                case EavAttributeType.Image:
                case EavAttributeType.Array:
                case EavAttributeType.File:
                    propertyType = TypeCode.Object;
                    break;
                case EavAttributeType.Serial:
                    propertyType = TypeCode.Int64;
                    break;
                default:
                    throw new Exception($"EavAttributeType {valueType} is not supported.");
            }

            return propertyType;
        }
    }
}