using YamlDotNet.Serialization;

namespace YaForms.Models;

/// <summary>
/// Root YAML model representing a Yandex Form specification.
/// </summary>
public sealed class FormSpec
{
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "pages")]
    public List<PageSpec> Pages { get; set; } = [];
}
