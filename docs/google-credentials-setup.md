# Google Credentials Setup

How to get `credentials.json` for the YaForms CLI.

## 1. Create a GCP project

1. Go to https://console.cloud.google.com/
2. Click **Select a project → New Project**
3. Name it (e.g. `yaforms-cli`), click **Create**

## 2. Enable the Google Forms API

1. Go to **APIs & Services → Library**
2. Search **Google Forms API**, click it, click **Enable**

## 3. Configure OAuth consent screen

1. Go to **APIs & Services → OAuth consent screen**
2. Choose **External**, click **Create**
3. Fill in **App name** (e.g. `YaForms CLI`) and **User support email**
4. Click **Save and Continue** through all steps (skip scopes)
5. Under **Test users** → add your Google account email → **Save**

## 4. Create OAuth 2.0 credentials

1. Go to **APIs & Services → Credentials**
2. Click **Create Credentials → OAuth client ID**
3. Application type: **Desktop app**
4. Name it (e.g. `yaforms-cli`), click **Create**
5. Click **Download JSON** → save as `credentials.json`

## 5. First run

```
yaforms push --input form.yaml --credentials path/to/credentials.json
```

Browser opens for Google consent on first run. Token cached at:
- Windows: `%APPDATA%\yaforms\`
- Linux/macOS: `~/.config/yaforms/`

Subsequent runs are silent.
