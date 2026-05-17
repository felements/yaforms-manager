using System.Text.Json;
using YaForms.Api;
using YaForms.Mapping;
using YaForms.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YaForms.Commands;

/// <summary>
/// Scaffolds an existing Yandex Form into a YAML spec file.
/// Also saves the raw JSON responses as a backup.
/// </summary>
public static class PullCommand
{
    public static async Task ExecuteAsync(int formId, string outputPath, string token, string orgId)
    {
        Console.WriteLine($"Pulling form {formId}...");

        using var client = new YaFormsClient(token, orgId);

        // 1. Fetch raw JSON (backup)
        Console.Write("  Fetching raw form data... ");
        var rawForm = await client.GetFormRawAsync(formId);
        var rawQuestions = await client.GetQuestionsRawAsync(formId);

        var backupPath = Path.ChangeExtension(outputPath, ".raw.json");
        var backup = new
        {
            pulledAt = DateTime.UtcNow.ToString("o"),
            formId,
            form = JsonDocument.Parse(rawForm).RootElement,
            questions = JsonDocument.Parse(rawQuestions).RootElement,
        };
        await File.WriteAllTextAsync(
            backupPath,
            JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"saved to {backupPath}");

        // 2. Fetch typed data
        Console.Write("  Mapping to spec... ");
        var survey = await client.GetFormAsync(formId);
        var questions = await client.GetQuestionsAsync(formId);

        // 3. Map to spec
        var spec = FormMapper.ToSpec(survey, questions);

        // 4. Serialize to YAML
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .Build();

        var yaml = serializer.Serialize(spec);
        await File.WriteAllTextAsync(outputPath, yaml);
        Console.WriteLine("done!");

        // 5. Summary
        var totalQuestions = spec.Pages.Sum(p => p.Questions.Count);
        Console.WriteLine();
        Console.WriteLine($"  Form:      {spec.Title}");
        Console.WriteLine($"  Pages:     {spec.Pages.Count}");
        Console.WriteLine($"  Questions: {totalQuestions}");
        Console.WriteLine($"  Output:    {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"  Backup:    {Path.GetFullPath(backupPath)}");
    }
}
