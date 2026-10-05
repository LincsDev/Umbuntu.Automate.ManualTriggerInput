using Umbraco.Automate.Core.Triggers;

namespace Umbuntu.Automate.ManualTriggerInput.Triggers;

// Fired by RunAutomationWithInputController ({id}/trigger-with-input), which the backoffice
// "Run with input…" action posts to. Automate exposes a trigger's raw output directly under the
// "trigger" key (not nested under an "output" sub-key), so steps bind the value as
// ${trigger.triggerInput}.
[Trigger(RunWithInputTrigger.TriggerAlias, "Manual Trigger (with Input)",
    Description = "Run manually with text you enter, available to steps as ${trigger.triggerInput}.",
    Group = "Third Party",
    Icon = "icon-hand-pointer")]
public sealed class RunWithInputTrigger(TriggerInfrastructure infrastructure)
    : TriggerBase<object, RunWithInputTriggerOutput>(infrastructure)
{
    // Mirrored as RUN_WITH_INPUT_TRIGGER_ALIAS in Client/src/run-with-input.condition.ts.
    public const string TriggerAlias = "umbuntu.manualWithInput";
}
