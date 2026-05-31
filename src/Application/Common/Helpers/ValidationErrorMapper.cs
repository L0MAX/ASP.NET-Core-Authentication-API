using System.Text.Json;
using FluentValidation.Results;

namespace Application.Common.Helpers;

public static class ValidationErrorMapper
{
    public static IReadOnlyDictionary<string, string[]> Map(IEnumerable<ValidationFailure> failures)
    {
        return failures
            .Where(failure => !string.IsNullOrWhiteSpace(failure.ErrorMessage))
            .GroupBy(failure => ToCamelCase(failure.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());
    }

    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return "_general";
        }

        return JsonNamingPolicy.CamelCase.ConvertName(propertyName);
    }
}
