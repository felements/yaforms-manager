# Security Policy

## Supported versions

| Version | Supported |
|---------|-----------|
| Latest  | ✅        |

## Reporting a vulnerability

**Please do not open a public GitHub issue for security vulnerabilities.**

If you discover a security issue, please report it privately so it can be addressed before public disclosure.

1. E-mail the maintainers directly (see repository contacts / GitHub profile).
2. Include a description of the vulnerability, steps to reproduce, and any relevant context.
3. We aim to acknowledge receipt within **2 business days** and provide a resolution timeline within **5 business days**.

We follow [responsible disclosure](https://en.wikipedia.org/wiki/Responsible_disclosure): once a fix is available we will coordinate a public announcement with you.

## Credential handling

YaForms never stores or logs your OAuth token or Organization ID.
Credentials are read from CLI flags or environment variables and are only passed as HTTP `Authorization` headers to the official Yandex Forms API.

To minimize exposure:
- Prefer environment variables (`YAFORMS_TOKEN`, `YAFORMS_ORG_ID`) over inline CLI flags so credentials don't appear in your shell history.
- Rotate OAuth tokens regularly via the [Yandex OAuth console](https://oauth.yandex.ru/).
