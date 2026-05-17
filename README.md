# YaForms CLI

> **Spec-first Yandex Forms management from the command line.**

YaForms is a .NET 10 CLI tool that lets you manage [Yandex Forms](https://forms.yandex.ru/) through a human-readable YAML specification — bypassing the restrictive web editor for complex or repetitive form work.

## Features

- **`pull`** — Scaffold any existing form into a versioned YAML file, preserving page structure, question order, and all type-specific parameters.
- **`push`** — Recreate a form from a YAML spec: creates the form, adds every question, places them on the correct pages, and optionally publishes it in one command.
- **Round-trip safe** — `params` block preserves all unknown API fields so nothing is lost between pull and push.
- **12-factor friendly** — credentials can be supplied via CLI flags or environment variables (`YAFORMS_TOKEN`, `YAFORMS_ORG_ID`).

## Supported question types

| YAML type   | Yandex Forms widget         |
|-------------|-----------------------------|
| `string`    | Short / long text input     |
| `boolean`   | Yes / No toggle             |
| `integer`   | Number input                |
| `file`      | File upload                 |
| `comment`   | Read-only comment block     |
| `date`      | Date picker                 |
| `date_range`| Date range picker           |
| `payment`   | Payment field               |
| `enum`      | Radio / Select / Checkbox   |
| `suggest`   | Suggest / autocomplete      |
| `matrix`    | Matrix / grid question      |
| `series`    | Series / repeating group    |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A Yandex OAuth token with `forms:write` scope
- Your Yandex Organization ID

## Installation

### Build from source

```bash
git clone https://github.com/<your-org>/yaforms.git
cd yaforms
dotnet build src/YaForms/YaForms.csproj -c Release
```

Run directly:

```bash
dotnet run --project src/YaForms -- --help
```

Or publish a self-contained binary:

```bash
dotnet publish src/YaForms/YaForms.csproj \
  -c Release -r linux-x64 --self-contained \
  -o ./publish
```

## Quick start

### 1. Obtain credentials

| Credential | How to get it |
|---|---|
| OAuth token | [Create an app](https://oauth.yandex.ru/) and request the `forms:write` scope, then exchange the code for a token |
| Organization ID | Found in the Yandex 360 admin console URL: `org.yandex.ru/XXXXXXXX/` |

Export them so you don't have to type them on every command:

```bash
export YAFORMS_TOKEN=y0_AgAAAA...
export YAFORMS_ORG_ID=12345678
```

### 2. Pull an existing form

```bash
yaforms pull 42 --output my-form.yaml
```

Produces a file like:

```yaml
title: Customer Satisfaction Survey
pages:
  - title: Page 1
    questions:
      - slug: q1
        title: How did you hear about us?
        type: enum
        required: true
        params:
          data_type: radio
          options:
            - value: "Social media"
            - value: "Friend"
            - value: "Search engine"
      - slug: q2
        title: Any additional comments?
        type: string
        required: false
        params:
          data_type: long
```

### 3. Edit and push

Modify the YAML, then recreate the form:

```bash
yaforms push --input my-form.yaml --publish
```

The CLI will print the new form ID and its admin URL.

## Usage reference

```
USAGE:
  yaforms [options] [command]

OPTIONS:
  --token <token>      OAuth token (or set YAFORMS_TOKEN env var)
  --org-id <org-id>   Organization ID (or set YAFORMS_ORG_ID env var)
  --version            Show version information
  -?, -h, --help       Show help and usage information

COMMANDS:
  pull <form-id>       Scaffold an existing Yandex Form into a YAML spec
  push                 Recreate a Yandex Form from a YAML spec

PULL OPTIONS:
  --output <path>      Output YAML file path [default: form.yaml]

PUSH OPTIONS:
  --input <path>       Input YAML file path [default: form.yaml]
  --publish            Automatically publish the form after creation
```

## Project structure

```
src/
└── YaForms/
    ├── Program.cs            # CLI entry point (System.CommandLine)
    ├── Commands/
    │   ├── PullCommand.cs    # pull — scaffold existing form → YAML
    │   └── PushCommand.cs    # push — YAML → new form via API
    ├── Api/
    │   ├── YaFormsClient.cs  # HTTP client wrapping the Yandex Forms API
    │   └── ApiModels.cs      # Raw API DTOs
    ├── Mapping/
    │   └── FormMapper.cs     # API DTOs ↔ YAML spec conversion
    └── Models/
        ├── FormSpec.cs       # Top-level YAML spec model
        ├── PageSpec.cs       # Page within a form
        └── QuestionSpec.cs   # Single question
```

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'feat: add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

## License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.
