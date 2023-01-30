using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.Projections;

namespace CloudFabric.EAV.Domain.Projections.EntityInstanceProjection;

public static class ProjectionDocumentSchemaFactory
{
    public static ProjectionDocumentSchema FromEntityConfiguration(
        EntityConfiguration entityConfiguration,
        List<AttributeConfiguration> attributeConfigurations
    )
    {
        var schema = new ProjectionDocumentSchema()
        {
            SchemaName = entityConfiguration.MachineName,
            Properties = new List<ProjectionDocumentPropertySchema>()
        };

        schema.Properties.Add(
            new ProjectionDocumentPropertySchema()
            {
                PropertyName = "Id",
                PropertyType = TypeCode.Object,
                IsKey = true,
                IsSearchable = false,
                IsRetrievable = true,
                IsFilterable = true,
                IsSortable = false,
                IsFacetable = false
            }
        );

        schema.Properties.Add(
            new ProjectionDocumentPropertySchema()
            {
                PropertyName = "EntityConfigurationId",
                PropertyType = TypeCode.Object,
                IsKey = false,
                IsSearchable = false,
                IsRetrievable = true,
                IsFilterable = true,
                IsSortable = false,
                IsFacetable = false
            }
        );

        schema.Properties.Add(
            new ProjectionDocumentPropertySchema()
            {
                PropertyName = "TenantId",
                PropertyType = TypeCode.Object,
                IsKey = false,
                IsSearchable = false,
                IsRetrievable = true,
                IsFilterable = true,
                IsSortable = false,
                IsFacetable = false
            }
        );

        schema.Properties.AddRange(attributeConfigurations.Select(GetAttributeProjectionPropertySchema));

        return schema;
    }

    private static ProjectionDocumentPropertySchema GetAttributeProjectionPropertySchema(
        AttributeConfiguration attributeConfiguration
    )
    {
        switch (attributeConfiguration.ValueType)
        {
            case EavAttributeType.Number:
                return ProjectionAttributesSchemaFactory.GetNumberAttributeSchema(attributeConfiguration);
            case EavAttributeType.Text:
                return ProjectionAttributesSchemaFactory.GetTextAttributeSchema(attributeConfiguration);
            case EavAttributeType.Boolean:
                return ProjectionAttributesSchemaFactory.GetBooleanAttributeSchema(attributeConfiguration);
            case EavAttributeType.HtmlText:
                return ProjectionAttributesSchemaFactory.GetHtmlTextAttributeSchema(attributeConfiguration);
            case EavAttributeType.EntityReference:
                return ProjectionAttributesSchemaFactory.GetEntityReferenceAttributeSchema(attributeConfiguration);
            case EavAttributeType.LocalizedText:
                return ProjectionAttributesSchemaFactory.GetLocalizedTextAttributeSchema(attributeConfiguration);
            case EavAttributeType.ValueFromList:
                return ProjectionAttributesSchemaFactory.GetValueFromListAttributeSchema(attributeConfiguration);
            case EavAttributeType.DateRange:
                return ProjectionAttributesSchemaFactory.GetDateAttributeSchema(attributeConfiguration);
            case EavAttributeType.Image:
                return ProjectionAttributesSchemaFactory.GetImageAttributeSchema(attributeConfiguration);
            case EavAttributeType.File:
                return ProjectionAttributesSchemaFactory.GetFileAttributeSchema(attributeConfiguration);
            case EavAttributeType.Array:
                return ProjectionAttributesSchemaFactory.GetArrayAttributeSchema(attributeConfiguration);
            case EavAttributeType.Serial:
                return ProjectionAttributesSchemaFactory.GetSerialAttributeShema(attributeConfiguration);
            default:
                throw new Exception($"EavAttributeType {attributeConfiguration.ValueType} is not supported.");
        }
    }
}