using System.Text.Json;
using System.Text.Json.Serialization;

namespace YaForms.Api;

// ──────────────────────────────────────────────
//  Response DTOs – GET /v1/surveys/{id}
// ──────────────────────────────────────────────

public sealed class ApiSurvey
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Raw JSON bag so we don't lose any fields we don't model.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

// ──────────────────────────────────────────────
//  Response DTOs – GET /v1/surveys/{id}/questions
//  Returns { pages: [ { id, page, questions: [...] } ] }
// ──────────────────────────────────────────────

public sealed class ApiQuestionsResponse
{
    [JsonPropertyName("pages")]
    public List<ApiQuestionPage> Pages { get; set; } = [];
}

public sealed class ApiQuestionPage
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("page")]
    public int PageNumber { get; set; }

    [JsonPropertyName("questions")]
    public List<ApiQuestion> Questions { get; set; } = [];
}

public sealed class ApiQuestion
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("question_type")]
    public string QuestionType { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("param_slug")]
    public string? ParamSlug { get; set; }

    /// <summary>Catch-all for type-specific params (data_type, options, etc.).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

// ──────────────────────────────────────────────
//  Request DTOs – POST /v1/surveys
// ──────────────────────────────────────────────

public sealed class CreateSurveyRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public sealed class CreateSurveyResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

// ──────────────────────────────────────────────
//  Request DTOs – POST /v1/surveys/{id}/questions
// ──────────────────────────────────────────────

public sealed class CreateQuestionRequest
{
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("question_type")]
    public string QuestionType { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    /// <summary>Catch-all for type-specific params.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class CreateQuestionResponse
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

// ──────────────────────────────────────────────
//  Request DTOs – PUT /v1/surveys/{id}/questions/{slug}/move
// ──────────────────────────────────────────────

public sealed class MoveQuestionRequest
{
    /// <summary>Target page ID. Mutually exclusive with page + create_page.</summary>
    [JsonPropertyName("page_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PageId { get; set; }

    /// <summary>Target page number (0-based).</summary>
    [JsonPropertyName("page")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Page { get; set; }

    /// <summary>If true, creates a new page.</summary>
    [JsonPropertyName("create_page")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool CreatePage { get; set; }

    /// <summary>Slug of the question to place after.</summary>
    [JsonPropertyName("after")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? After { get; set; }
}
