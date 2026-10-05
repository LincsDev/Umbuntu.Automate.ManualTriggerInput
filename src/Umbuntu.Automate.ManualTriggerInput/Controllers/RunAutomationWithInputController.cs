using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Execution;
using Umbraco.Automate.Web.Api.Management.Automation.Controllers;
using Umbraco.Cms.Api.Common.Builders;
using Umbraco.Cms.Core.Security;
using Umbuntu.Automate.ManualTriggerInput.Configuration;
using Umbuntu.Automate.ManualTriggerInput.Triggers;

namespace Umbuntu.Automate.ManualTriggerInput.Controllers;

// Sibling to the package's built-in "Run now" endpoint (TriggerAutomationController), which
// posts to {id:guid}/trigger with no body. That endpoint can't carry input, so any custom
// trigger built on ${trigger.*} (e.g. RunWithInputTrigger) needs its own entry point.
// This mirrors TriggerAutomationController's auth/status/circuit-breaker checks, additionally
// restricts runs to automations using RunWithInputTrigger, and forwards the request body as the
// trigger output data instead of passing null.
[ApiVersion("1.0")]
public sealed class RunAutomationWithInputController(
    IAutomationService automationService,
    IAuthorizationService authorizationService,
    IAutomationExecutor executor,
    ICircuitBreakerService circuitBreaker,
    IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
    IOptions<ManualTriggerInputOptions> options) : AutomationControllerBase
{
    // Lets the modal show the configured limit and validate before posting.
    [HttpGet("trigger-with-input/settings")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(RunWithInputSettingsResponse), 200)]
    public IActionResult GetSettings() => Ok(new RunWithInputSettingsResponse(options.Value.MaxInputBytes));

    [HttpPost("{id:guid}/trigger-with-input")]
    [MapToApiVersion("1.0")]
    [TypeFilter(typeof(TriggerInputRequestSizeFilter))]
    [ProducesResponseType(202)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    [ProducesResponseType(typeof(ProblemDetails), 413)]
    public async Task<IActionResult> TriggerWithInput(
        Guid id,
        [FromBody] RunWithInputTriggerOutput input,
        CancellationToken cancellationToken = default)
    {
        // System.Text.Json assigns an explicit `"triggerInput": null` despite the non-nullable
        // declaration, so normalise it rather than letting null reach ${trigger.triggerInput}.
        var triggerInput = input.TriggerInput ?? string.Empty;

        // The request filter only bounds the raw body; this is the exact limit on the input itself.
        if (Encoding.UTF8.GetByteCount(triggerInput) > options.Value.MaxInputBytes)
            return TriggerInputRequestSizeFilter.TooLarge(options.Value.MaxInputBytes);

        var automation = await automationService.GetAutomationAsync(id, cancellationToken);
        if (automation is null)
            return AutomationNotFound();

        var authResult = await AuthorizeWorkspaceAccessAsync(authorizationService, automation.WorkspaceId);
        if (authResult is not null)
            return authResult;

        // The client only offers "Run with input" for this trigger, but that's UI gating — without
        // this check the endpoint would start any published automation (content, schedule,
        // webhook…) with caller-supplied trigger data.
        if (automation.Trigger?.TriggerAlias != RunWithInputTrigger.TriggerAlias)
            return Conflict(new ProblemDetailsBuilder()
                .WithTitle("Unsupported trigger")
                .WithDetail("Only automations using the Manual Trigger (with Input) can be run with input.")
                .Build());

        if (automation.Status != AutomationStatus.Published)
            return Conflict(new ProblemDetailsBuilder()
                .WithTitle("Automation not active")
                .WithDetail("The automation must be published to be triggered.")
                .Build());

        if (!await circuitBreaker.IsRunAllowedAsync(id, "user", cancellationToken))
            return Conflict(new ProblemDetailsBuilder()
                .WithTitle("Automation auto-disabled")
                .WithDetail("This automation has been auto-disabled by the circuit breaker. Re-enable it to run.")
                .Build());

        // IAutomationExecutor.ExecuteAsync takes a generic Dictionary<string, object?> — that's
        // the framework's contract for every trigger, whatever its output shape, not something
        // specific to this one. Binding the request body to RunWithInputTriggerOutput (rather
        // than JsonElement/Dictionary<string, object?> directly) means TriggerInput here is
        // already a plain CLR string, not a System.Text.Json.JsonElement — which matters because
        // WorkflowCore persists this via Newtonsoft.Json, and Newtonsoft can't serialize a
        // JsonElement correctly.
        var triggerOutputData = new Dictionary<string, object?>
        {
            ["triggerInput"] = triggerInput,
        };

        var userId = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Key;
        await executor.ExecuteAsync(automation, "user", userId?.ToString(), triggerOutputData, cancellationToken);
        return Accepted();
    }
}

public sealed record RunWithInputSettingsResponse(long MaxInputBytes);
