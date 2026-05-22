# Google Forms Push — Design Spec

**Date:** 2026-05-22  
**Scope:** Replace Yandex Forms backend with Google Forms API. Rework `push` command. Stub `pull`. Update YAML schema and `data/survey.yaml`.

---

## 1. Motivation

Yandex Forms support dropped. Google Forms is the new target. The existing YAML-first workflow (edit spec → push to create form) is preserved.

---

## 2. Google Cloud Credentials Setup

See [docs/google-credentials-setup.md](../google-credentials-setup.md).

---

## 3. YAML Schema

### 3.1 Changes from Yandex schema

| Field | Old (Yandex) | New (Google) |
|---|---|---|
| `slug` | Yandex question ID | Kept in model, **ignored on push** |
| option `id` | Yandex numeric ID | **Removed** |
| `data_type` inside params | `radio`/`checkbox`/`select` | Moved to `type` inside params |
| question types | see table below | see table below |

### 3.2 Question type mapping

| YAML `type` | Meaning | Google API |
|---|---|---|
| `info` | Display-only text block | `TEXT_ITEM` |
| `short_answer` | Single-line text input | `TextQuestion` paragraph=false |
| `paragraph` | Multi-line text input | `TextQuestion` paragraph=true |
| `integer` | Numeric input (mapped to text) | `TextQuestion` paragraph=false |
| `choice` | Multiple choice / checkboxes / dropdown | `ChoiceQuestion` |
| `date` | Date picker | `DateQuestion` |
| `file` | File upload | `FileUploadQuestion` |

Dropped (no Google equivalent): `boolean`, `matrix`, `series`, `suggest`, `payment`, `date_range`.  
Replace `boolean` in specs with `choice` (radio, two options).

### 3.3 `choice` params

```yaml
type: choice
required: true
params:
  type: radio        # radio | checkbox | dropdown
  options:
    - "Option A"
    - "Option B"
```

### 3.4 Pages → Sections

Yandex "pages" map to Google Form sections:
- Page 0: no separator — questions placed directly
- Pages 1+: a `PAGE_BREAK_ITEM` with the page title inserted before the first question of that page

### 3.5 Full schema example

```yaml
title: "My Form"
description: "Optional form description"
pages:
  - title: "Section 1"
    questions:
      - title: "Intro text shown to respondent"
        type: info
      - title: "Your name"
        type: short_answer
        required: true
      - title: "Your age"
        type: integer
        required: true
      - title: "Preferred option"
        type: choice
        required: true
        params:
          type: radio
          options:
            - "Option A"
            - "Option B"
  - title: "Section 2"
    questions:
      - title: "Additional comments"
        type: paragraph
        required: false
```

---

## 4. Architecture

### 4.1 Files deleted

- `src/YaForms/Api/YaFormsClient.cs`
- `src/YaForms/Api/ApiModels.cs`

### 4.2 Files added / replaced

```
src/YaForms/
  Api/
    GoogleFormsClient.cs     ← typed wrapper over FormsService
  Commands/
    PushCommand.cs           ← rewritten for Google Forms API
    PullCommand.cs           ← stubbed (TODO: implement pull from Google)
  Mapping/
    FormMapper.cs            ← updated type map + Google request builders
```

### 4.3 NuGet packages

| Package | Purpose |
|---|---|
| `Google.Apis.Forms.v1` | Typed Google Forms client + generated DTOs |
| `Google.Apis.Auth` | OAuth 2.0 desktop flow, token refresh (transitive) |

Remove: `YamlDotNet` stays. Remove nothing else.

---

## 5. Authentication Implementation

```csharp
// GoogleFormsClient.cs
var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
    GoogleClientSecrets.FromFile(credentialsPath).Secrets,
    [FormsService.Scope.Forms],
    "user",
    CancellationToken.None,
    new FileDataStore(tokenCacheDir, fullPath: true));

_service = new FormsService(new BaseClientService.Initializer
{
    HttpClientInitializer = credential,
    ApplicationName = "YaForms CLI",
});
```

Token cache directory: `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "yaforms")` — resolves to `~/.config/yaforms` on Linux/Mac, `%APPDATA%\yaforms` on Windows.

---

## 6. Push Flow

```
1. Deserialize YAML → FormSpec
2. Validate: spec non-empty, all choice questions have options
3. Authenticate → FormsService
4. POST /v1/forms  { info: { title } }  → formId
5. Build batchUpdate requests (all items, in order):
     for each page[i]:
       if i > 0: append CreateItemRequest(PAGE_BREAK, title=page.title, index=cursor++)
       for each question in page:
         append CreateItemRequest(mapped item, index=cursor++)
6. POST /v1/forms/{formId}:batchUpdate  (single call)
7. Print:
     Form ID:  {formId}
     Edit URL: https://docs.google.com/forms/d/{formId}/edit
```

Single batchUpdate call replaces Yandex's N sequential create+move calls — simpler and faster.

---

## 7. CLI Interface

### Removed options
- `--token` / `YAFORMS_TOKEN`
- `--org-id` / `YAFORMS_ORG_ID`

### Added options
- `--credentials <path>` / `GOOGLE_CREDENTIALS_PATH` — path to `credentials.json` from GCP
- `--publish` option removed — Google Forms are always accessible via link after creation, no separate publish step

### Commands
```
yaforms push [--input form.yaml] [--credentials credentials.json]
yaforms pull  # → "Not implemented yet." and exit 1
```

---

## 8. FormMapper updates

`FormMapper.ToGoogleItem(QuestionSpec q, int index)` returns a Google `Item`:

```
type = "info"         → Item { Title=q.Title, TextItem={} }
type = "short_answer" → Item { Title=q.Title, QuestionItem={ Question={ TextQuestion={ Paragraph=false }, Required=q.Required } } }
type = "paragraph"    → same with Paragraph=true
type = "integer"      → same as short_answer
type = "choice"       → Item { QuestionItem={ Question={ ChoiceQuestion={ Type=RADIO|CHECKBOX|DROP_DOWN, Options=[...] }, Required } } }
type = "date"         → Item { QuestionItem={ Question={ DateQuestion={} }, Required } }
type = "file"         → Item { QuestionItem={ Question={ FileUploadQuestion={} }, Required } }
unknown               → log warning, skip
```

---

## 9. `data/survey.yaml` Migration

Changes applied to the existing file:
- All `type: static_text` → `type: info`
- All `type: enum` → `type: choice`
- All `params.data_type: radio` → `params.type: radio`
- All `params.data_type: checkbox` → `params.type: checkbox`
- All option `id:` fields removed (each option becomes a plain string)
- All `type: integer` → unchanged (already valid)
- `slug` fields → kept as-is (ignored on push, harmless)

---

## 10. Out of Scope

- `pull` command implementation
- Google Forms response/answer retrieval
- Form editing (update existing form)
- Conditional logic / branching between sections
