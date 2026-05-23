// src/YaForms/Mapping/FormMapper.cs
using Google.Apis.Forms.v1.Data;
using YaForms.Models;

namespace YaForms.Mapping;

public static class FormMapper
{
    public static IList<Request> BuildRequests(FormSpec spec)
    {
        var requests = new List<Request>();
        var index = 0;

        for (var pageIdx = 0; pageIdx < spec.Pages.Count; pageIdx++)
        {
            var page = spec.Pages[pageIdx];

            if (pageIdx > 0)
            {
                requests.Add(new Request
                {
                    CreateItem = new CreateItemRequest
                    {
                        Item = new Item
                        {
                            Title = N(page.Title),
                            PageBreakItem = new PageBreakItem()
                        },
                        Location = new Location { Index = index++ }
                    }
                });
            }

            foreach (var q in page.Questions)
            {
                var item = ToGoogleItem(q);
                if (item is null)
                {
                    Console.Error.WriteLine($"  Warning: unknown question type '{q.Type}', skipped.");
                    continue;
                }

                requests.Add(new Request
                {
                    CreateItem = new CreateItemRequest
                    {
                        Item = item,
                        Location = new Location { Index = index++ }
                    }
                });
            }
        }

        return requests;
    }

    private static string N(string? s) =>
        (s ?? string.Empty).ReplaceLineEndings(" ").Trim();

    public static Item? ToGoogleItem(QuestionSpec q)
    {
        return q.Type switch
        {
            "info" => new Item
            {
                Title = N(q.Title),
                TextItem = new TextItem()
            },
            "short_answer" or "integer" => new Item
            {
                Title = N(q.Title),
                QuestionItem = new QuestionItem
                {
                    Question = new Question
                    {
                        Required = q.Required,
                        TextQuestion = new TextQuestion { Paragraph = false }
                    }
                }
            },
            "paragraph" => new Item
            {
                Title = N(q.Title),
                QuestionItem = new QuestionItem
                {
                    Question = new Question
                    {
                        Required = q.Required,
                        TextQuestion = new TextQuestion { Paragraph = true }
                    }
                }
            },
            "choice" => BuildChoiceItem(q),
            "date" => new Item
            {
                Title = N(q.Title),
                QuestionItem = new QuestionItem
                {
                    Question = new Question
                    {
                        Required = q.Required,
                        DateQuestion = new DateQuestion()
                    }
                }
            },
            "file" => new Item
            {
                Title = N(q.Title),
                QuestionItem = new QuestionItem
                {
                    Question = new Question
                    {
                        Required = q.Required,
                        FileUploadQuestion = new FileUploadQuestion()
                    }
                }
            },
            _ => null
        };
    }

    private static Item BuildChoiceItem(QuestionSpec q)
    {
        var choiceType = "RADIO";
        var options = new List<Option>();

        if (q.Params is not null)
        {
            if (q.Params.TryGetValue("type", out var typeObj))
            {
                choiceType = typeObj?.ToString() switch
                {
                    "radio"    => "RADIO",
                    "checkbox" => "CHECKBOX",
                    "dropdown" => "DROP_DOWN",
                    _          => "RADIO"
                };
            }

            if (q.Params.TryGetValue("options", out var optsObj) && optsObj is List<object> optList)
                options = optList.Select(o => new Option { Value = N(o.ToString()) }).ToList();
        }

        return new Item
        {
            Title = N(q.Title),
            QuestionItem = new QuestionItem
            {
                Question = new Question
                {
                    Required = q.Required,
                    ChoiceQuestion = new ChoiceQuestion
                    {
                        Type = choiceType,
                        Options = options
                    }
                }
            }
        };
    }
}
