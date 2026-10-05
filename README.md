# Manual Trigger (with Input) for Umbraco Automate

[![NuGet](https://img.shields.io/nuget/v/Umbuntu.Automate.ManualTriggerInput.svg)](https://www.nuget.org/packages/Umbuntu.Automate.ManualTriggerInput)
[![Umbraco Marketplace](https://img.shields.io/badge/umbraco-marketplace-3544b1)](https://marketplace.umbraco.com/package/umbuntu.automate.manualtriggerinput)

Run an [Umbraco Automate](https://www.nuget.org/packages/Umbraco.Automate) automation by hand **and give it some input**.

Automate's built-in Manual Trigger starts an automation with no data. This package adds a **Manual Trigger (with Input)**. Editors start the automation from a **Run with input…** action in the backoffice and type or paste text, such as a sentence, a URL or a JSON object. Every step in the automation can then use that text as `${trigger.triggerInput}`.

## Requirements

- Umbraco CMS 18
- Umbraco Automate 18.5 or later

## Installation

```bash
dotnet add package Umbuntu.Automate.ManualTriggerInput
```

There is nothing to register. The trigger, the API endpoint and the backoffice action are all picked up automatically when the site starts.

## Usage

1. Create an automation and choose **Manual Trigger (with Input)** as its trigger.
2. In any step, use `${trigger.triggerInput}` where you want the input. For example, use it as the message of a *Log Message* step.
3. Publish the automation.
4. In the automations tree, open the automation's actions menu and choose **Run with input…**.
5. Enter the input and click **Run**, or press <kbd>Ctrl</kbd>/<kbd>Cmd</kbd>+<kbd>Enter</kbd>.

**Run with input…** only appears for published automations that use this trigger.

The input is always passed to steps as **text**. To pass structured data, paste JSON and parse it in the step that needs it.

## Configuration

The input is stored with every run, so its size is limited. The default limit is **1 MB**. To change it, set the value in bytes in `appsettings.json`:

```json
{
  "Umbuntu": {
    "ManualTriggerInput": {
      "MaxInputBytes": 2097152
    }
  }
}
```

The run modal shows how much of the limit has been used, and blocks input that is too large. The server enforces the same limit and returns `413 Payload Too Large` if it is exceeded. A value of zero or less stops the site from starting.

## Security

- **Who can run it.** Running an automation requires the same workspace access as Automate's own "Run now".
- **Which automations can run.** The endpoint only starts published automations that use this trigger. Automations with any other trigger, such as content events, schedules or webhooks, are rejected with `409 Conflict`.
- **Size.** Input is limited as described under [Configuration](#configuration).
- **Treat the input as untrusted** in your steps. If a step turns it into HTML (for example via Markdown), puts it in an AI prompt, or sends it to an external service, sanitise or validate it first, as you would any user-supplied text.

## API

Other code can start runs through the backoffice Management API. These calls need a backoffice bearer token with workspace access to the automation.

| Method | Path | Body | Responses |
|---|---|---|---|
| `POST` | `/umbraco/automate/management/api/v1/automations/{id}/trigger-with-input` | `{ "triggerInput": "…" }` | `202`, `404`, `409`, `413` |
| `GET` | `/umbraco/automate/management/api/v1/automations/trigger-with-input/settings` | | `200 { "maxInputBytes": 1048576 }` |

## Contributing

Issues and pull requests are welcome at [github.com/LincsDev/Umbuntu.Automate.ManualTriggerInput](https://github.com/LincsDev/Umbuntu.Automate.ManualTriggerInput).

The backoffice client lives in `src/Umbuntu.Automate.ManualTriggerInput/Client`. `dotnet build` builds it automatically, which needs Node.js 22 or later. Run `npm run watch` in that folder for development builds with source maps.

## Licence

[MIT](LICENSE)
