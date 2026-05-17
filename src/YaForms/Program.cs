using System.CommandLine;
using YaForms.Commands;

// ─── Global options ────────────────────────────────

var tokenOption = new Option<string?>("--token") { Description = "OAuth token (or set YAFORMS_TOKEN env var)" };
var orgIdOption = new Option<string?>("--org-id") { Description = "Organization ID (or set YAFORMS_ORG_ID env var)" };

// ─── Pull command ──────────────────────────────────

var pullFormIdArg = new Argument<int>("form-id") { Description = "The survey ID of the form to scaffold" };

var pullOutputOption = new Option<string>("--output") { Description = "Output YAML file path", DefaultValueFactory = _ => "form.yaml" };

var pullCommand = new Command("pull", "Scaffold an existing Yandex Form into a YAML spec");
pullCommand.Arguments.Add(pullFormIdArg);
pullCommand.Options.Add(pullOutputOption);

pullCommand.SetAction(async (parseResult, ct) =>
{
    var formId = parseResult.GetValue(pullFormIdArg);
    var output = parseResult.GetValue(pullOutputOption)!;
    var token = parseResult.GetValue(tokenOption);
    var orgId = parseResult.GetValue(orgIdOption);

    var resolvedToken = ResolveRequired(token, "YAFORMS_TOKEN", "OAuth token (--token or YAFORMS_TOKEN)");
    var resolvedOrgId = ResolveRequired(orgId, "YAFORMS_ORG_ID", "Org ID (--org-id or YAFORMS_ORG_ID)");
    await PullCommand.ExecuteAsync(formId, output, resolvedToken, resolvedOrgId);
});

// ─── Push command ──────────────────────────────────

var pushInputOption = new Option<string>("--input") { Description = "Input YAML file path", DefaultValueFactory = _ => "form.yaml" };
var pushPublishOption = new Option<bool>("--publish") { Description = "Automatically publish the form after creation" };

var pushCommand = new Command("push", "Recreate a Yandex Form from a YAML spec");
pushCommand.Options.Add(pushInputOption);
pushCommand.Options.Add(pushPublishOption);

pushCommand.SetAction(async (parseResult, ct) =>
{
    var input = parseResult.GetValue(pushInputOption)!;
    var publish = parseResult.GetValue(pushPublishOption);
    var token = parseResult.GetValue(tokenOption);
    var orgId = parseResult.GetValue(orgIdOption);

    var resolvedToken = ResolveRequired(token, "YAFORMS_TOKEN", "OAuth token (--token or YAFORMS_TOKEN)");
    var resolvedOrgId = ResolveRequired(orgId, "YAFORMS_ORG_ID", "Org ID (--org-id or YAFORMS_ORG_ID)");
    await PushCommand.ExecuteAsync(input, resolvedToken, resolvedOrgId, publish);
});

// ─── Root command ──────────────────────────────────

var rootCommand = new RootCommand("YaForms CLI — spec-first Yandex Forms management");
rootCommand.Options.Add(tokenOption);
rootCommand.Options.Add(orgIdOption);
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
    return null!; // unreachable
}
