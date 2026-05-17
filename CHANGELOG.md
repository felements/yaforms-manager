# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `pull` command — scaffold an existing Yandex Form into a YAML spec file.
- `push` command — recreate a Yandex Form from a YAML spec, with optional `--publish` flag.
- Support for all Yandex Forms question types: `string`, `boolean`, `integer`, `file`, `comment`, `date`, `date_range`, `payment`, `enum`, `suggest`, `matrix`, `series`.
- Credential resolution from CLI flags (`--token`, `--org-id`) or environment variables (`YAFORMS_TOKEN`, `YAFORMS_ORG_ID`).
- Lossless round-tripping via the `params` block in the YAML spec.
