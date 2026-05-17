using YamlDotNet.Serialization;

namespace YaForms.Models;

/// <summary>
/// Represents a single question within a page.
/// </summary>
public sealed class QuestionSpec
{
    /// <summary>
    /// Unique slug identifier for the question (from the API).
    /// </summary>
    [YamlMember(Alias = "slug")]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// The question title / label shown to the user.
    /// </summary>
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Simplified type name: string, boolean, integer, file, comment,
    /// date, date_range, payment, enum, suggest, matrix, series.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether this question is required.
    /// </summary>
    [YamlMember(Alias = "required")]
    public bool Required { get; set; }

    /// <summary>
    /// Type-specific parameters preserved as a dictionary for lossless round-tripping.
    /// For enum questions this includes data_type (radio/select/checkbox) and options.
    /// For string questions this includes data_type (short/long), etc.
    /// </summary>
    [YamlMember(Alias = "params")]
    public Dictionary<string, object>? Params { get; set; }
}
