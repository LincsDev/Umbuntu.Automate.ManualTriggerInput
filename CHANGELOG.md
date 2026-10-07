# Changelog

All notable changes to this package are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/).

## [Unreleased]

### Changed
- The input size limit now lives with Automate's own settings, as `Umbraco:Automate:ManualTriggerInput:MaxInputBytes`.

### Deprecated
- `Umbuntu:ManualTriggerInput:MaxInputBytes`. It still works, but `Umbraco:Automate:ManualTriggerInput` takes precedence when both are set. It will be removed in 2.0.0.

## [1.0.0] - 2026-10-06

### Added
- **Manual Trigger (with Input)** for Umbraco Automate. Its input is available to steps as `${trigger.triggerInput}`.
- **Run with input…** backoffice action, shown only for published automations that use this trigger.
- Run modal with required-field validation, an input size counter, and <kbd>Ctrl</kbd>/<kbd>Cmd</kbd>+<kbd>Enter</kbd> to run.
- `POST …/automations/{id}/trigger-with-input` endpoint. It only starts automations that use this trigger.
- Configurable input size limit, `Umbuntu:ManualTriggerInput:MaxInputBytes`, defaulting to 1 MB.
- English localisation.
