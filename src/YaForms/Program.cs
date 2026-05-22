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
