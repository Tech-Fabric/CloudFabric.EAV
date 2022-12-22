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
            Properties = attributeConfigurations.Select(
                GetAttributeProjectionPropertySchema
            ).ToList()
        };

        schema.Properties.Add(new ProjectionDocumentPropertySchema()
            {
                PropertyName = "Id",
                PropertyType = TypeCode.String,
                IsKey = true,
                IsSearchable = false,
                IsRetrievable = true,
                IsFilterable = true,
                IsSortable = false,
                IsFacetable = false
            }
        );

        schema.Properties.Add(new ProjectionDocumentPropertySchema()
            {
                PropertyName = "EntityConfigurationId",
                PropertyType = TypeCode.String,
                IsKey = false,
                IsSearchable = false,
                IsRetrievable = true,
                IsFilterable = true,
                IsSortable = false,
                IsFacetable = false
            }
        );

        return schema;
    }

    private static ProjectionDocumentPropertySchema GetAttributeProjectionPropertySchema(
        AttributeConfiguration attributeConfiguration
    )
    {
        TypeCode? propertyType = null;
        bool isNestedObject = false;
        bool isNestedArray = false;

        switch (attributeConfiguration.ValueType)
        {
            case EavAttributeType.Number:
                propertyType = TypeCode.Decimal;
                break;
            case EavAttributeType.Text:
            case EavAttributeType.HtmlText:
            case EavAttributeType.EntityReference:
                propertyType = TypeCode.String;
                break;
            case EavAttributeType.LocalizedText:
            case EavAttributeType.ValueFromList:
            case EavAttributeType.DateRange:
            case EavAttributeType.Image:
                propertyType = TypeCode.Object;
                isNestedObject = true;
                break;
            case EavAttributeType.Array:
                propertyType = TypeCode.Object;
                isNestedArray = true;
                break;
            default:
                throw new Exception($"EavAttributeType {attributeConfiguration.ValueType} is not supported.");
        }

        return new ProjectionDocumentPropertySchema()
        {
            PropertyName = attributeConfiguration.MachineName,
            PropertyType = propertyType.GetValueOrDefault(),
            IsKey = false,
            IsSearchable = true,
            IsRetrievable = true,
            //SynonymMaps = documentPropertyAttribute.SynonymMaps,
            //SearchableBoost = documentPropertyAttribute.SearchableBoost,
            IsFilterable = true,
            IsSortable = true,
            IsFacetable = true,
            //Analyzer = documentPropertyAttribute.Analyzer,
            //SearchAnalyzer = documentPropertyAttribute.SearchAnalyzer,
            //IndexAnalyzer = documentPropertyAttribute.IndexAnalyzer,
            //UseForSuggestions = documentPropertyAttribute.UseForSuggestions,
            //FacetableRanges = documentPropertyAttribute.FacetableRanges,
            IsNestedObject = isNestedObject,
            IsNestedArray = isNestedArray,
            //ArrayElementType = documentPropertyAttribute.IsNestedArray
            //    ? Type.GetTypeCode(propertyInfo.PropertyType.GenericTypeArguments[0])
            //    : null,
            //NestedObjectProperties = (documentPropertyAttribute.IsNestedObject || documentPropertyAttribute.IsNestedArray)
            //    ? GetNestedObjectProperties(nestedPropertiesDictionary as Dictionary<PropertyInfo, (ProjectionDocumentPropertyAttribute, object)>)
            //    : null
        };
    }
}