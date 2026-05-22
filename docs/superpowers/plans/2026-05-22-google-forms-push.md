# Google Forms Push — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Yandex Forms backend with Google Forms API — rework `push`, stub `pull`, update YAML schema, migrate `data/survey.yaml`.

**Architecture:** Delete `YaFormsClient`/`ApiModels`. Add `GoogleFormsClient` wrapping `Google.Apis.Forms.v1.FormsService`. `FormMapper.BuildRequests` converts a `FormSpec` into one `batchUpdate` payload. Push creates the form in two API calls: one to create the form shell, one `batchUpdate` with all items. Auth uses OAuth 2.0 desktop flow via `Google.Apis.Auth`, token cached on disk.

**Tech Stack:** .NET 10, `Google.Apis.Forms.v1`, `YamlDotNet`, `System.CommandLine`, xUnit 2.9 (tests)

---

## File Map

| Action | Path | Responsibility |
|---|---|---|
| Delete | `src/YaForms/Api/YaFormsClient.cs` | Yandex HTTP client — gone |
| Delete | `src/YaForms/Api/ApiModels.cs` | Yandex DTOs — gone |
| Create | `src/YaForms/Api/GoogleFormsClient.cs` | Auth + `CreateFormAsync` + `BatchUpdateAsync` |
| Replace | `src/YaForms/Mapping/FormMapper.cs` | `BuildRequests(FormSpec)` + `ToGoogleItem(QuestionSpec)` |
| Replace | `src/YaForms/Commands/PushCommand.cs` | Orchestrates YAML → Google Forms |
| Replace | `src/YaForms/Commands/PullCommand.cs` | Stub only — prints "not implemented" |
| Replace | `src/YaForms/Program.cs` | New CLI surface (`--credentials`, drop `--token`/`--org-id`) |
| Replace | `src/YaForms/YaForms.csproj` | Add `Google.Apis.Forms.v1` package |
| Create | `tests/YaForms.Tests/YaForms.Tests.csproj` | xUnit test project |
| Create | `tests/YaForms.Tests/FormMapperTests.cs` | Unit tests for `FormMapper` |
| Replace | `data/survey.yaml` | Migrated to new YAML schema |

---

## Task 1: Add NuGet package, remove Yandex files

**Files:**
- Modify: `src/YaForms/YaForms.csproj`
- Delete: `src/YaForms/Api/YaFormsClient.cs`
- Delete: `src/YaForms/Api/ApiModels.cs`

- [ ] **Step 1: Add Google Forms NuGet package**

```powershell
cd c:\projects\yaforms-manager
dotnet add src/YaForms/YaForms.csproj package Google.Apis.Forms.v1
```

Expected: package added, `YaForms.csproj` updated with `<PackageReference Include="Google.Apis.Forms.v1" .../>`.

- [ ] **Step 2: Delete Yandex API files**

```powershell
Remove-Item src/YaForms/Api/YaFormsClient.cs
Remove-Item src/YaForms/Api/ApiModels.cs
```

- [ ] **Step 3: Verify build fails (expected — references broken)**

```powershell
dotnet build src/YaForms/YaForms.csproj 2>&1 | Select-String "error"
```

Expected: errors about missing `YaFormsClient`, `ApiModels` types. This is correct — we'll fix in later tasks.

- [ ] **Step 4: Commit**

```powershell
git add src/YaForms/YaForms.csproj
git add src/YaForms/Api/YaFormsClient.cs
git add src/YaForms/Api/ApiModels.cs
git commit -m "chore: add Google.Apis.Forms.v1, remove Yandex API files"
```

---

## Task 2: Set up test project

**Files:**
- Create: `tests/YaForms.Tests/YaForms.Tests.csproj`
- Create: `tests/YaForms.Tests/FormMapperTests.cs` (placeholder only in this task)

- [ ] **Step 1: Scaffold xUnit project**

```powershell
dotnet new xunit -n YaForms.Tests -o tests/YaForms.Tests --framework net10.0
```

- [ ] **Step 2: Reference main project and add Google package**

```powershell
dotnet add tests/YaForms.Tests/YaForms.Tests.csproj reference src/YaForms/YaForms.csproj
dotnet add tests/YaForms.Tests/YaForms.Tests.csproj package Google.Apis.Forms.v1
```

- [ ] **Step 3: Add test project to solution**

`YaForms.slnx` is simple XML. Replace its content with:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/YaForms/YaForms.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/YaForms.Tests/YaForms.Tests.csproj" />
  </Folder>
</Solution>
```

- [ ] **Step 4: Verify test scaffold builds**

```powershell
dotnet build tests/YaForms.Tests/YaForms.Tests.csproj
```

Expected: build succeeds (scaffold has a passing `UnitTest1` placeholder).

- [ ] **Step 5: Delete placeholder test file**

```powershell
Remove-Item tests/YaForms.Tests/UnitTest1.cs
```

- [ ] **Step 6: Commit**

```powershell
git add tests/
git add YaForms.slnx
git commit -m "chore: add YaForms.Tests xUnit project"
```

---

## Task 3: Write failing FormMapper tests

**Files:**
- Create: `tests/YaForms.Tests/FormMapperTests.cs`

At this point `FormMapper.cs` still has old Yandex code — tests will fail to compile. That's expected (red phase).

- [ ] **Step 1: Write test file**

```csharp
// tests/YaForms.Tests/FormMapperTests.cs
using Google.Apis.Forms.v1.Data;
using YaForms.Mapping;
using YaForms.Models;

namespace YaForms.Tests;

public class FormMapperTests
{
    [Fact]
    public void ToGoogleItem_Info_ReturnsTextItem()
    {
        var q = new QuestionSpec { Title = "Intro", Type = "info" };
        var item = FormMapper.ToGoogleItem(q);
        Assert.NotNull(item);
        Assert.Equal("Intro", item.Title);
        Assert.NotNull(item.TextItem);
        Assert.Null(item.QuestionItem);
    }

    [Fact]
    public void ToGoogleItem_ShortAnswer_ReturnsNonParagraphTextQuestion()
    {
        var q = new QuestionSpec { Title = "Name", Type = "short_answer", Required = true };
        var item = FormMapper.ToGoogleItem(q);
        Assert.NotNull(item?.QuestionItem?.Question?.TextQuestion);
        Assert.False(item.QuestionItem.Question.TextQuestion.Paragraph);
        Assert.True(item.QuestionItem.Question.Required);
    }

    [Fact]
    public void ToGoogleItem_Integer_ReturnsNonParagraphTextQuestion()
    {
        var q = new QuestionSpec { Title = "Age", Type = "integer", Required = true };
        var item = FormMapper.ToGoogleItem(q);
        Assert.NotNull(item?.QuestionItem?.Question?.TextQuestion);
        Assert.False(item.QuestionItem.Question.TextQuestion.Paragraph);
    }

    [Fact]
    public void ToGoogleItem_Paragraph_ReturnsParagraphTextQuestion()
    {
        var q = new QuestionSpec { Title = "Comments", Type = "paragraph", Required = false };
        var item = FormMapper.ToGoogleItem(q);
        Assert.NotNull(item?.QuestionItem?.Question?.TextQuestion);
        Assert.True(item.QuestionItem.Question.TextQuestion.Paragraph);
        Assert.False(item.QuestionItem.Question.Required);
    }

    [Fact]
    public void ToGoogleItem_ChoiceRadio_ReturnsRadioChoiceQuestion()
    {
        var q = new QuestionSpec
        {
            Title = "Gender",
            Type = "choice",
            Required = true,
            Params = new Dictionary<string, object>
            {
                ["type"] = "radio",
                ["options"] = new List<object> { "Male", "Female" }
            }
        };
        var item = FormMapper.ToGoogleItem(q);
        var choice = item?.QuestionItem?.Question?.ChoiceQuestion;
        Assert.NotNull(choice);
        Assert.Equal("RADIO", choice.Type);
        Assert.Equal(2, choice.Options.Count);
        Assert.Contains(choice.Options, o => o.Value == "Male");
        Assert.Contains(choice.Options, o => o.Value == "Female");
    }

    [Fact]
    public void ToGoogleItem_ChoiceCheckbox_ReturnsCheckboxChoiceQuestion()
    {
        var q = new QuestionSpec
        {
            Title = "Interests",
            Type = "choice",
            Required = false,
            Params = new Dictionary<string, object>
            {
                ["type"] = "checkbox",
                ["options"] = new List<object> { "A", "B", "C" }
            }
        };
        var item = FormMapper.ToGoogleItem(q);
        Assert.Equal("CHECKBOX", item?.QuestionItem?.Question?.ChoiceQuestion?.Type);
        Assert.Equal(3, item?.QuestionItem?.Question?.ChoiceQuestion?.Options.Count);
    }

    [Fact]
    public void ToGoogleItem_ChoiceDropdown_ReturnsDropDownChoiceQuestion()
    {
        var q = new QuestionSpec
        {
            Title = "Country",
            Type = "choice",
            Required = true,
            Params = new Dictionary<string, object>
            {
                ["type"] = "dropdown",
                ["options"] = new List<object> { "RU", "US" }
            }
        };
        var item = FormMapper.ToGoogleItem(q);
        Assert.Equal("DROP_DOWN", item?.QuestionItem?.Question?.ChoiceQuestion?.Type);
    }

    [Fact]
    public void ToGoogleItem_Date_ReturnsDateQuestion()
    {
        var q = new QuestionSpec { Title = "Birth date", Type = "date", Required = true };
        var item = FormMapper.ToGoogleItem(q);
        Assert.NotNull(item?.QuestionItem?.Question?.DateQuestion);
        Assert.True(item.QuestionItem.Question.Required);
    }

    [Fact]
    public void ToGoogleItem_UnknownType_ReturnsNull()
    {
        var q = new QuestionSpec { Title = "X", Type = "matrix" };
        var item = FormMapper.ToGoogleItem(q);
        Assert.Null(item);
    }

    [Fact]
    public void BuildRequests_SinglePage_NoPageBreak()
    {
        var spec = new FormSpec
        {
            Title = "Test",
            Pages =
            [
                new PageSpec
                {
                    Title = "Page 1",
                    Questions = [new QuestionSpec { Title = "Q1", Type = "short_answer" }]
                }
            ]
        };
        var requests = FormMapper.BuildRequests(spec);
        Assert.Single(requests);
        Assert.NotNull(requests[0].CreateItem.Item.QuestionItem);
    }

    [Fact]
    public void BuildRequests_MultiPage_InsertsPageBreakBeforeEachNonFirstPage()
    {
        var spec = new FormSpec
        {
            Title = "Test",
            Pages =
            [
                new PageSpec
                {
                    Title = "Page 1",
                    Questions = [new QuestionSpec { Title = "Q1", Type = "short_answer" }]
                },
                new PageSpec
                {
                    Title = "Page 2",
                    Questions = [new QuestionSpec { Title = "Q2", Type = "short_answer" }]
                }
            ]
        };
        var requests = FormMapper.BuildRequests(spec);
        // Q1, PAGE_BREAK(Page 2), Q2 = 3 items
        Assert.Equal(3, requests.Count);
        Assert.NotNull(requests[0].CreateItem.Item.QuestionItem);       // Q1
        Assert.NotNull(requests[1].CreateItem.Item.PageBreakItem);      // break
        Assert.Equal("Page 2", requests[1].CreateItem.Item.Title);
        Assert.NotNull(requests[2].CreateItem.Item.QuestionItem);       // Q2
    }

    [Fact]
    public void BuildRequests_ItemsHaveCorrectAscendingIndexes()
    {
        var spec = new FormSpec
        {
            Title = "Test",
            Pages =
            [
                new PageSpec
                {
                    Title = "P1",
                    Questions =
                    [
                        new QuestionSpec { Title = "Q1", Type = "short_answer" },
                        new QuestionSpec { Title = "Q2", Type = "short_answer" }
                    ]
                },
                new PageSpec
                {
                    Title = "P2",
                    Questions = [new QuestionSpec { Title = "Q3", Type = "short_answer" }]
                }
            ]
        };
        var requests = FormMapper.BuildRequests(spec);
        // Q1=0, Q2=1, BREAK=2, Q3=3
        Assert.Equal(4, requests.Count);
        for (var i = 0; i < requests.Count; i++)
            Assert.Equal(i, requests[i].CreateItem.Location.Index);
    }
}
```

- [ ] **Step 2: Confirm tests fail to compile (FormMapper API mismatch)**

```powershell
dotnet build tests/YaForms.Tests/YaForms.Tests.csproj 2>&1 | Select-String "error"
```

Expected: compile errors — `FormMapper` has no `ToGoogleItem` or `BuildRequests`. Correct.

---

## Task 4: Replace FormMapper

**Files:**
- Replace: `src/YaForms/Mapping/FormMapper.cs`

- [ ] **Step 1: Overwrite FormMapper.cs**

```csharp
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
                            Title = page.Title,
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

    public static Item? ToGoogleItem(QuestionSpec q)
    {
        return q.Type switch
        {
            "info" => new Item
            {
                Title = q.Title,
                TextItem = new TextItem()
            },
            "short_answer" or "integer" => new Item
            {
                Title = q.Title,
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
                Title = q.Title,
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
                Title = q.Title,
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
                Title = q.Title,
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
                options = optList.Select(o => new Option { Value = o.ToString() }).ToList();
        }

        return new Item
        {
            Title = q.Title,
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
```

- [ ] **Step 2: Run tests — verify they pass**

```powershell
dotnet test tests/YaForms.Tests/YaForms.Tests.csproj --logger "console;verbosity=normal"
```

Expected: all 12 tests PASS.

- [ ] **Step 3: Commit**

```powershell
git add src/YaForms/Mapping/FormMapper.cs tests/YaForms.Tests/FormMapperTests.cs
git commit -m "feat: replace FormMapper with Google Forms item builder (TDD)"
```

---

## Task 5: Create GoogleFormsClient

**Files:**
- Create: `src/YaForms/Api/GoogleFormsClient.cs`

- [ ] **Step 1: Create the file**

```csharp
// src/YaForms/Api/GoogleFormsClient.cs
using Google.Apis.Auth.OAuth2;
using Google.Apis.Forms.v1;
using Google.Apis.Forms.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace YaForms.Api;

public sealed class GoogleFormsClient : IDisposable
{
    private readonly FormsService _service;

    private GoogleFormsClient(FormsService service) => _service = service;

    public static async Task<GoogleFormsClient> CreateAsync(string credentialsPath, CancellationToken ct = default)
    {
        var tokenDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "yaforms");

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromFile(credentialsPath).Secrets,
            [FormsService.Scope.Forms],
            "user",
            ct,
            new FileDataStore(tokenDir, fullPath: true));

        var service = new FormsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "YaForms CLI",
        });

        return new GoogleFormsClient(service);
    }

    public async Task<string> CreateFormAsync(string title, CancellationToken ct = default)
    {
        var form = await _service.Forms
            .Create(new Form { Info = new Info { Title = title } })
            .ExecuteAsync(ct);
        return form.FormId;
    }

    public async Task BatchUpdateAsync(string formId, IList<Request> requests, CancellationToken ct = default)
    {
        await _service.Forms
            .BatchUpdate(new BatchUpdateFormRequest { Requests = requests }, formId)
            .ExecuteAsync(ct);
    }

    public void Dispose() => _service.Dispose();
}
```

- [ ] **Step 2: Verify it compiles**

```powershell
dotnet build src/YaForms/YaForms.csproj 2>&1 | Select-String "error"
```

Expected: no errors (PushCommand/Program still have old code — they'll be broken, but `GoogleFormsClient.cs` itself should compile).

---

## Task 6: Rewrite PushCommand

**Files:**
- Replace: `src/YaForms/Commands/PushCommand.cs`

- [ ] **Step 1: Overwrite PushCommand.cs**

```csharp
// src/YaForms/Commands/PushCommand.cs
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
```

---

## Task 7: Stub PullCommand and update Program.cs

**Files:**
- Replace: `src/YaForms/Commands/PullCommand.cs`
- Replace: `src/YaForms/Program.cs`

- [ ] **Step 1: Overwrite PullCommand.cs**

```csharp
// src/YaForms/Commands/PullCommand.cs
namespace YaForms.Commands;

public static class PullCommand
{
    public static Task<int> ExecuteAsync()
    {
        Console.Error.WriteLine("Error: pull command is not implemented yet.");
        return Task.FromResult(1);
    }
}
```

- [ ] **Step 2: Overwrite Program.cs**

```csharp
// src/YaForms/Program.cs
using System.CommandLine;
using YaForms.Commands;

var credentialsOption = new Option<string?>("--credentials")
{
    Description = "Path to credentials.json (or set GOOGLE_CREDENTIALS_PATH env var)"
};

// ─── Pull command ──────────────────────────────────

var pullCommand = new Command("pull", "Scaffold an existing Google Form into a YAML spec [not implemented]");
pullCommand.SetAction((_, _) => PullCommand.ExecuteAsync());

// ─── Push command ──────────────────────────────────

var pushInputOption = new Option<string>("--input")
{
    Description = "Input YAML spec file path",
    DefaultValueFactory = _ => "form.yaml"
};

var pushCommand = new Command("push", "Create a Google Form from a YAML spec");
pushCommand.Options.Add(pushInputOption);

pushCommand.SetAction(async (parseResult, ct) =>
{
    var input       = parseResult.GetValue(pushInputOption)!;
    var credentials = parseResult.GetValue(credentialsOption);

    var resolvedCredentials = ResolveRequired(
        credentials,
        "GOOGLE_CREDENTIALS_PATH",
        "credentials.json path (--credentials or GOOGLE_CREDENTIALS_PATH)");

    await PushCommand.ExecuteAsync(input, resolvedCredentials, ct);
});

// ─── Root command ──────────────────────────────────

var rootCommand = new RootCommand("YaForms CLI — spec-first Google Forms management");
rootCommand.Options.Add(credentialsOption);
rootCommand.Subcommands.Add(pullCommand);
rootCommand.Subcommands.Add(pushCommand);

return await rootCommand.Parse(args).InvokeAsync();

// ─── Helper ────────────────────────────────────────

static string ResolveRequired(string? cliValue, string envVar, string displayName)
{
    if (!string.IsNullOrWhiteSpace(cliValue))
        return cliValue;

    var envValue = Environment.GetEnvironmentVariable(envVar);
    if (!string.IsNullOrWhiteSpace(envValue))
        return envValue;

    Console.Error.WriteLine($"Error: {displayName} is required.");
    Console.Error.WriteLine($"  Provide via CLI option or set the {envVar} environment variable.");
    Environment.Exit(1);
    return null!;
}
```

- [ ] **Step 3: Verify clean build**

```powershell
dotnet build src/YaForms/YaForms.csproj
```

Expected: **0 errors, 0 warnings** (or only nullable warnings).

- [ ] **Step 4: Run all tests again**

```powershell
dotnet test tests/YaForms.Tests/YaForms.Tests.csproj --logger "console;verbosity=normal"
```

Expected: all 12 tests PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/YaForms/Api/GoogleFormsClient.cs
git add src/YaForms/Commands/PushCommand.cs
git add src/YaForms/Commands/PullCommand.cs
git add src/YaForms/Program.cs
git commit -m "feat: rewrite push command for Google Forms API"
```

---

## Task 8: Migrate data/survey.yaml

**Files:**
- Replace: `data/survey.yaml`

The YAML has ~1558 lines with Yandex-specific structure. Apply transformations via PowerShell.

- [ ] **Step 1: Run migration script**

```powershell
$path = "data/survey.yaml"
$content = Get-Content $path -Raw

# Question types
$content = $content -replace 'type: static_text', 'type: info'
$content = $content -replace 'type: enum',        'type: choice'

# params.data_type → params.type
$content = $content -replace 'data_type: radio',    'type: radio'
$content = $content -replace 'data_type: checkbox', 'type: checkbox'
$content = $content -replace 'data_type: select',   'type: dropdown'

# Options: collapse two-line "- id: NNN\n  text: VALUE" into one line "- VALUE"
# The pattern matches indented "- id: DIGITS" followed by "text: VALUE" on next line
$content = $content -replace '(?m)^(\s*)- id: \d+\r?\n\s+text: (.+)$', '$1- $2'

Set-Content $path $content -Encoding UTF8 -NoNewline
```

- [ ] **Step 2: Verify spot-check — no Yandex artifacts remain**

```powershell
Select-String -Path data/survey.yaml -Pattern "static_text|data_type|^\s+- id: \d"
```

Expected: **no matches**.

- [ ] **Step 3: Verify YAML is parseable**

```powershell
dotnet run --project src/YaForms -- push --input data/survey.yaml --credentials nonexistent.json 2>&1 | Select-String "Form:|Pages:|Questions:|Error"
```

Expected output shows form title + page/question counts before hitting the credentials error:
```
Reading spec from data/survey.yaml...
  Form:      Исследование особенностей волевой регуляции
  Pages:     5
  Questions: ...
...
Error: credentials.json path ...
```

This confirms the YAML deserializes correctly under the new schema.

- [ ] **Step 4: Commit**

```powershell
git add data/survey.yaml
git commit -m "chore: migrate survey.yaml to Google Forms schema"
```

---

## Task 9: Smoke test (manual)

This task requires a real `credentials.json` obtained from GCP (see design spec section 2 for setup instructions).

- [ ] **Step 1: Set credentials env var**

```powershell
$env:GOOGLE_CREDENTIALS_PATH = "C:\path\to\credentials.json"
```

- [ ] **Step 2: Run push with the survey spec**

```powershell
dotnet run --project src/YaForms -- push --input data/survey.yaml
```

Expected flow:
1. Browser opens for Google OAuth consent (first run only)
2. Console prints form title, page count, question count
3. "Creating form... id=XXXX"
4. "Adding items (batch)... N items added."
5. Edit URL printed

- [ ] **Step 3: Verify form in Google**

Open the printed Edit URL. Confirm:
- Form title matches `survey.yaml` title
- 5 sections (page breaks between pages 2–5)
- Questions appear in correct order with correct types (radio choices, checkboxes, text inputs, etc.)
- Required fields are marked required

- [ ] **Step 4: Final commit**

```powershell
git commit --allow-empty -m "chore: smoke test passed — Google Forms push working"
```
