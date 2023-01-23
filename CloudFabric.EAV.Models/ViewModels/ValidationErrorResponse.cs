using System.Collections.Generic;

using CloudFabric.EAV.Domain.Enums;

using Microsoft.AspNetCore.Mvc;

namespace CloudFabric.EAV.Models.ViewModels;

public class ValidationErrorResponse : ValidationProblemDetails
{
    public new IDictionary<string, string[]> Errors { get; }

    public ValidationErrorResponse(IDictionary<string, string[]> errors)
    {
        Type = CommonErrorTypes.ValidationError;
        Title = "Validation errors occured";
        Errors = errors;
    }

    public ValidationErrorResponse(string fieldName, string errorMessage)
        : this(GetDictionary(fieldName, new string[] { errorMessage }))
    {
    }

    public ValidationErrorResponse(string fieldName, string[] errorMessages)
        : this(GetDictionary(fieldName, errorMessages))
    {
    }

    private static IDictionary<string, string[]> GetDictionary(string fieldName, string[] errorMessages)
    {
        fieldName = string.IsNullOrEmpty(fieldName) ? "unknownError" : fieldName;
        
        return new Dictionary<string, string[]>
        {
            { fieldName, errorMessages }
        };
    }
}