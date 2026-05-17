using YamlDotNet.Serialization;

namespace YaForms.Models;

/// <summary>
/// Represents a single page within the form.
/// Pages group questions and control multi-step flow.
/// </summary>
public sealed class PageSpec
{
    [YamlMember(Alias = "title")]
    public string? Title { get; set; }

    [YamlMember(Alias = "questions")]
    public List<QuestionSpec> Questions { get; set; } = [];
}
