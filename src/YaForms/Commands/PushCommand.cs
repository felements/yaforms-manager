using YaForms.Api;
using YaForms.Mapping;
using YaForms.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YaForms.Commands;

/// <summary>
/// Recreates a Yandex Form from a YAML spec file.
/// Creates the form, adds all questions, and places them on the correct pages in order.
/// </summary>
public static class PushCommand
{
    public static async Task ExecuteAsync(string inputPath, string token, string orgId, bool publish)
    {
        Console.WriteLine($"Reading spec from {inputPath}...");

        // 1. Deserialize YAML
        var yaml = await File.ReadAllTextAsync(inputPath);
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

        using var client = new YaFormsClient(token, orgId);

        // 2. Create form
        Console.Write("  Creating form... ");
        var createResp = await client.CreateFormAsync(new CreateSurveyRequest { Name = spec.Title });
        var surveyId = createResp.Id;
        Console.WriteLine($"id={surveyId}");

        // 3. Create questions and place them on pages
        string? previousSlug = null;

        for (var pageIdx = 0; pageIdx < spec.Pages.Count; pageIdx++)
        {
            var page = spec.Pages[pageIdx];
            Console.WriteLine($"  Page {pageIdx + 1}: {page.Title ?? "(untitled)"}");
            var isFirstOnPage = true;

            foreach (var q in page.Questions)
            {
                Console.Write($"    [{q.Type}] {q.Title}... ");

                // Create the question
                var createReq = FormMapper.ToCreateRequest(q);
                var qResp = await client.CreateQuestionAsync(surveyId, createReq);
                var createdSlug = qResp.Slug;

                // Move it to the correct page and position
                var moveReq = new MoveQuestionRequest();

                if (pageIdx == 0)
                {
                    // First page: questions go on page 0 (default page)
                    moveReq.Page = 0;
                }
                else if (isFirstOnPage)
                {
                    // First question on a new page: create a new page
                    moveReq.CreatePage = true;
                }
                else
                {
                    // Subsequent questions: place after previous on same page
                    moveReq.Page = pageIdx;
                }

                if (previousSlug is not null && !isFirstOnPage)
                {
                    moveReq.After = previousSlug;
                }

                await client.MoveQuestionAsync(surveyId, createdSlug, moveReq);
                Console.WriteLine($"slug={createdSlug}");

                previousSlug = createdSlug;
                isFirstOnPage = false;
            }
        }

        // 4. Optionally publish
        if (publish)
        {
            Console.Write("  Publishing... ");
            await client.PublishFormAsync(surveyId);
            Console.WriteLine("done!");
        }

        Console.WriteLine();
        Console.WriteLine($"  New form ID: {surveyId}");
        Console.WriteLine($"  Admin URL:   https://forms.yandex.ru/admin/{surveyId}/");
    }
}
