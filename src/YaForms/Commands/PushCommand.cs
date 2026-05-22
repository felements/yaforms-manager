using YaForms.Api;
using YaForms.Mapping;
using YaForms.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YaForms.Commands;

public static class PushCommand
{
    public static async Task ExecuteAsync(string inputPath, string credentialsPath, CancellationToken ct = default)
    {
        Console.WriteLine($"Reading spec from {inputPath}...");

        var yaml = await File.ReadAllTextAsync(inputPath, ct);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        var spec = deserializer.Deserialize<FormSpec>(yaml);

        if (spec is null || spec.Pages.Count == 0)
        {
            Console.Error.WriteLine("Error: spec is empty or has no pages.");
            return;
        }

        var totalQuestions = spec.Pages.Sum(p => p.Questions.Count);
        Console.WriteLine($"  Form:      {spec.Title}");
        Console.WriteLine($"  Pages:     {spec.Pages.Count}");
        Console.WriteLine($"  Questions: {totalQuestions}");
        Console.WriteLine();

        Console.WriteLine("Authenticating with Google...");
        using var client = await GoogleFormsClient.CreateAsync(credentialsPath, ct);

        Console.Write("Creating form... ");
        var formId = await client.CreateFormAsync(spec.Title, ct);
        Console.WriteLine($"id={formId}");

        Console.WriteLine("Adding items (batch)...");
        var requests = FormMapper.BuildRequests(spec);
        await client.BatchUpdateAsync(formId, requests, ct);
        Console.WriteLine($"  {requests.Count} items added.");

        Console.WriteLine();
        Console.WriteLine($"  Form ID:  {formId}");
        Console.WriteLine($"  Edit URL: https://docs.google.com/forms/d/{formId}/edit");
    }
}
