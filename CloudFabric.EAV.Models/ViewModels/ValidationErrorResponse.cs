using CloudFabric.EAV.Enums;

using Microsoft.AspNetCore.Mvc;

namespace CloudFabric.EAV.Models.ViewModels;

public class ValidationErrorResponse : ValidationProblemDetails
{
    public ValidationErrorResponse(IDictionary<string, string[]> errors)
    {
        Type = CommonErrorTypes.VALIDATION_ERROR;
        Title = "Validation errors occured";
        Errors = errors;
    }

    public ValidationErrorResponse(string fieldName, string errorMessage)
        : this(GetDictionary(fieldName, new[] { errorMessage }))
    {
    }

    public ValidationErrorResponse(string fieldName, string[] errorMessages)
        : this(GetDictionary(fieldName, errorMessages))
    {
    }

    public new IDictionary<string, string[]> Errors { get; }

    private static IDictionary<string, string[]> GetDictionary(string fieldName, string[] errorMessages)
    {
        fieldName = string.IsNullOrEmpty(fieldName) ? "unknownError" : fieldName;

        return new Dictionary<string, string[]> { { fieldName, errorMessages } };
    }
}
