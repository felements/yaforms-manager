# Contributing to YaForms

Thank you for considering a contribution! This document describes how to get involved.

## Code of Conduct

This project follows the [Contributor Covenant](https://www.contributor-covenant.org/) Code of Conduct.
By participating, you agree to uphold its standards.

## Ways to contribute

- **Bug reports** — Open an issue with a minimal reproduction case.
- **Feature requests** — Open an issue describing the problem you're trying to solve.
- **Pull requests** — Bug fixes, new features, documentation improvements, and tests are all welcome.

## Getting started

1. **Fork** the repository and clone your fork locally.
2. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
3. Build the project:

   ```bash
   dotnet build src/YaForms/YaForms.csproj
   ```

4. Run the tool locally against a real form to verify your changes:

   ```bash
   export YAFORMS_TOKEN=y0_AgAAAA...
   export YAFORMS_ORG_ID=12345678
   dotnet run --project src/YaForms -- pull <form-id>
   ```

## Pull request guidelines

- Keep PRs focused — one concern per PR.
- Write a clear description explaining *what* changed and *why*.
- Follow [Conventional Commits](https://www.conventionalcommits.org/) for commit messages:
  `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`
- Make sure `dotnet build` succeeds with no warnings before opening a PR.
- Add or update XML doc comments for any public API you touch.

## Reporting security vulnerabilities

Please **do not** open a public issue for security bugs.
Instead, e-mail the maintainers directly (see the repository contacts).
We will respond within 5 business days.

## License

By contributing you agree that your code will be released under the project's [MIT License](LICENSE).
