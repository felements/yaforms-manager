using System.Text.Json;
using YaForms.Api;
using YaForms.Models;

namespace YaForms.Mapping;

/// <summary>
/// Maps between raw API DTOs and the clean YAML spec models.
/// </summary>
public static class FormMapper
{
    // ──────────────────────────────────────────────
    //  API type name ↔ simplified YAML type
    // ──────────────────────────────────────────────

    private static readonly Dictionary<string, string> ApiToYamlType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["QuestionStringIn"] = "string",
        ["QuestionStringOut"] = "string",
        ["QuestionBooleanIn"] = "boolean",
        ["QuestionBooleanOut"] = "boolean",
        ["QuestionIntegerIn"] = "integer",
        ["QuestionIntegerOut"] = "integer",
        ["QuestionFileIn"] = "file",
        ["QuestionFileOut"] = "file",
        ["QuestionCommentIn"] = "comment",
        ["QuestionCommentOut"] = "comment",
        ["QuestionDateIn"] = "date",
        ["QuestionDateOut"] = "date",
        ["QuestionDateRangeIn"] = "date_range",
        ["QuestionDateRangeOut"] = "date_range",
        ["QuestionPaymentIn"] = "payment",
        ["QuestionPaymentOut"] = "payment",
        ["QuestionEnumIn"] = "enum",
        ["QuestionEnumOut"] = "enum",
        ["QuestionSuggestIn"] = "suggest",
        ["QuestionSuggestOut"] = "suggest",
        ["QuestionMatrixIn"] = "matrix",
        ["QuestionMatrixOut"] = "matrix",
        ["QuestionSeriesIn"] = "series",
        ["QuestionSeriesOut"] = "series",
    };

    private static readonly Dictionary<string, string> YamlToApiType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["string"] = "QuestionStringIn",
        ["boolean"] = "QuestionBooleanIn",
        ["integer"] = "QuestionIntegerIn",
        ["file"] = "QuestionFileIn",
        ["comment"] = "QuestionCommentIn",
        ["date"] = "QuestionDateIn",
        ["date_range"] = "QuestionDateRangeIn",
        ["payment"] = "QuestionPaymentIn",
        ["enum"] = "QuestionEnumIn",
        ["suggest"] = "QuestionSuggestIn",
        ["matrix"] = "QuestionMatrixIn",
        ["series"] = "QuestionSeriesIn",
    };

    // ──────────────────────────────────────────────
    //  API → Spec
    // ──────────────────────────────────────────────

    /// <summary>
    /// Converts the API response into a YAML-serializable FormSpec.
    /// </summary>
    public static FormSpec ToSpec(ApiSurvey survey, ApiQuestionsResponse questions)
    {
        var spec = new FormSpec
        {
            Title = survey.Title ?? survey.Name,
        };

        foreach (var apiPage in questions.Pages.OrderBy(p => p.PageNumber))
        {
            var page = new PageSpec
            {
                Title = $"Page {apiPage.PageNumber + 1}",
            };

            foreach (var q in apiPage.Questions)
            {
                var question = new QuestionSpec
                {
                    Slug = q.Slug,
                    Title = q.Label ?? string.Empty,
                    Type = MapApiType(q.QuestionType),
                    Required = q.Required,
                    Params = ExtractParams(q),
                };
                page.Questions.Add(question);
            }

            spec.Pages.Add(page);
        }

        return spec;
    }

    // ──────────────────────────────────────────────
    //  Spec → API create requests
    // ──────────────────────────────────────────────

    /// <summary>
    /// Builds the CreateQuestionRequest from a YAML QuestionSpec.
    /// </summary>
    public static CreateQuestionRequest ToCreateRequest(QuestionSpec q)
    {
        var request = new CreateQuestionRequest
        {
            Slug = q.Slug,
            Label = q.Title,
            QuestionType = MapYamlType(q.Type),
            Required = q.Required,
        };

        // Pack params back into extension data
        if (q.Params is { Count: > 0 })
        {
            request.ExtensionData = new Dictionary<string, JsonElement>();
            foreach (var (key, value) in q.Params)
            {
                var json = JsonSerializer.SerializeToElement(value);
                request.ExtensionData[key] = json;
            }
        }

        return request;
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private static string MapApiType(string apiType)
    {
        return ApiToYamlType.TryGetValue(apiType, out var yaml) ? yaml : apiType;
    }

    private static string MapYamlType(string yamlType)
    {
        return YamlToApiType.TryGetValue(yamlType, out var api) ? api : yamlType;
    }

    /// <summary>
    /// Extracts type-specific params from the extension data of an API question.
    /// Known structural fields (slug, label, question_type, required) are excluded.
    /// </summary>
    private static Dictionary<string, object>? ExtractParams(ApiQuestion q)
    {
        if (q.ExtensionData is null || q.ExtensionData.Count == 0)
            return null;

        var result = new Dictionary<string, object>();
        foreach (var (key, element) in q.ExtensionData)
        {
            result[key] = ConvertJsonElement(element);
        }

        return result.Count > 0 ? result : null;
    }

    /// <summary>
    /// Recursively converts a JsonElement to a plain CLR object tree
    /// suitable for YAML serialization.
    /// </summary>
    private static object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => "null",
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertJsonElement)
                .ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => element.GetRawText(),
        };
    }
}
