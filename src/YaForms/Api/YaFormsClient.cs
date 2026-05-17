using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace YaForms.Api;

/// <summary>
/// Typed HTTP client for the Yandex Forms API v1.
/// Base URL: https://api.forms.yandex.net
/// </summary>
public sealed class YaFormsClient : IDisposable
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public YaFormsClient(string oauthToken, string orgId)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri("https://api.forms.yandex.net"),
        };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", oauthToken);
        _http.DefaultRequestHeaders.Add("X-Org-Id", orgId);
    }

    // ─── Read ────────────────────────────────────────

    /// <summary>Get form metadata.</summary>
    public async Task<ApiSurvey> GetFormAsync(int surveyId, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"/v1/surveys/{surveyId}/", ct);
        await EnsureSuccess(resp);
        return (await resp.Content.ReadFromJsonAsync<ApiSurvey>(JsonOptions, ct))!;
    }

    /// <summary>Get all questions grouped by pages.</summary>
    public async Task<ApiQuestionsResponse> GetQuestionsAsync(int surveyId, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"/v1/surveys/{surveyId}/questions/", ct);
        await EnsureSuccess(resp);
        return (await resp.Content.ReadFromJsonAsync<ApiQuestionsResponse>(JsonOptions, ct))!;
    }

    /// <summary>Get raw JSON for the questions endpoint (for backup).</summary>
    public async Task<string> GetQuestionsRawAsync(int surveyId, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"/v1/surveys/{surveyId}/questions/", ct);
        await EnsureSuccess(resp);
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>Get raw JSON for the form endpoint (for backup).</summary>
    public async Task<string> GetFormRawAsync(int surveyId, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"/v1/surveys/{surveyId}/", ct);
        await EnsureSuccess(resp);
        return await resp.Content.ReadAsStringAsync(ct);
    }

    // ─── Write ───────────────────────────────────────

    /// <summary>Create a new form.</summary>
    public async Task<CreateSurveyResponse> CreateFormAsync(CreateSurveyRequest request, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/v1/surveys/", request, JsonOptions, ct);
        await EnsureSuccess(resp);
        return (await resp.Content.ReadFromJsonAsync<CreateSurveyResponse>(JsonOptions, ct))!;
    }

    /// <summary>Create a question in the given form.</summary>
    public async Task<CreateQuestionResponse> CreateQuestionAsync(
        int surveyId,
        CreateQuestionRequest request,
        CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"/v1/surveys/{surveyId}/questions/", request, JsonOptions, ct);
        await EnsureSuccess(resp);
        return (await resp.Content.ReadFromJsonAsync<CreateQuestionResponse>(JsonOptions, ct))!;
    }

    /// <summary>Move a question to a specific page and position.</summary>
    public async Task MoveQuestionAsync(
        int surveyId,
        string slug,
        MoveQuestionRequest request,
        CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync(
            $"/v1/surveys/{surveyId}/questions/{slug}/move/", request, JsonOptions, ct);
        await EnsureSuccess(resp);
    }

    /// <summary>Publish the form.</summary>
    public async Task PublishFormAsync(int surveyId, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"/v1/surveys/{surveyId}/publish/", null, ct);
        await EnsureSuccess(resp);
    }

    // ─── Helpers ─────────────────────────────────────

    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Yandex Forms API error {(int)response.StatusCode} {response.StatusCode}: {body}");
        }
    }

    public void Dispose() => _http.Dispose();
}
