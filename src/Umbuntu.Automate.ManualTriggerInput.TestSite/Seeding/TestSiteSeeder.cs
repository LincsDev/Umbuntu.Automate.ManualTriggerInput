using Umbraco.Automate.Core;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbuntu.Automate.ManualTriggerInput.Triggers;
using Constants = Umbraco.Cms.Core.Constants;

namespace Umbuntu.Automate.ManualTriggerInput.TestSite.Seeding;

/// <summary>
/// Gives a fresh test site everything needed to try the trigger straight away: an API user, a
/// workspace that runs as it, and a published automation that logs the input it's run with.
/// Each item is looked up first, so restarts (or items deleted in the backoffice) are handled
/// by creating only what's missing.
/// </summary>
internal sealed class TestSiteSeeder(
    AutomateReadinessSignal readinessSignal,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<TestSiteSeeder> logger) : BackgroundService
{
    private const string ServiceAccountEmail = "automate-api@example.test";
    private const string WorkspaceAlias = "test";
    private const string AutomationAlias = "logTriggerInput";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Set once Automate's migrations have run, which on a first run is only after the
        // unattended install, so the admin user and Automate's tables both exist by then.
        if (!await readinessSignal.WaitUntilReadyAsync(stoppingToken))
        {
            logger.LogWarning("Automate's migrations failed, so the test site wasn't seeded.");
            return;
        }

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var services = scope.ServiceProvider;

            var serviceAccount = await EnsureServiceAccountAsync(services.GetRequiredService<IUserService>());
            var workspace = await EnsureWorkspaceAsync(services.GetRequiredService<IWorkspaceService>(), serviceAccount, stoppingToken);
            await EnsureAutomationAsync(services.GetRequiredService<IAutomationService>(), workspace, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Seeding is a convenience; a failure shouldn't take the site down with it.
            logger.LogError(ex, "Seeding the test site failed.");
        }
    }

    private async Task<IUser> EnsureServiceAccountAsync(IUserService userService)
    {
        if (userService.GetByEmail(ServiceAccountEmail) is { } existing)
            return existing;

        // Automate only accepts an API user as a workspace's service account. It's an admin so
        // automations aren't blocked by section access (Automate's migration grants the Admin
        // group its section).
        var result = await userService.CreateAsync(Constants.Security.SuperUserKey, new UserCreateModel
        {
            Email = ServiceAccountEmail,
            UserName = ServiceAccountEmail,
            Name = "Automate Service Account",
            Kind = UserKind.Api,
            UserGroupKeys = new HashSet<Guid> { Constants.Security.AdminGroupKey },
        });

        if (!result.Success || result.Result.CreatedUser is not { } created)
            throw new InvalidOperationException($"Couldn't create the Automate service account: {result.Status}.");

        logger.LogInformation("Seeded API user {Email} as the Automate service account.", ServiceAccountEmail);
        return created;
    }

    private async Task<Workspace> EnsureWorkspaceAsync(IWorkspaceService workspaceService, IUser serviceAccount, CancellationToken cancellationToken)
    {
        if (await workspaceService.GetWorkspaceByAliasAsync(WorkspaceAlias, cancellationToken) is { } existing)
            return existing;

        var workspace = await workspaceService.CreateWorkspaceAsync(new Workspace
        {
            Alias = WorkspaceAlias,
            Name = "Test",
            ServiceAccountKey = serviceAccount.Key,
            UserGroups = [Constants.Security.AdminGroupKey],
        }, Constants.Security.SuperUserKey, cancellationToken);

        logger.LogInformation("Seeded Automate workspace '{Alias}'.", WorkspaceAlias);
        return workspace;
    }

    private async Task EnsureAutomationAsync(IAutomationService automationService, Workspace workspace, CancellationToken cancellationToken)
    {
        if (await automationService.GetAutomationByAliasAsync(AutomationAlias, cancellationToken) is not null)
            return;

        var logStepId = Guid.NewGuid();
        var automation = await automationService.CreateAutomationAsync(new Automation
        {
            Alias = AutomationAlias,
            Name = "Log trigger input",
            Description = "Logs the text entered in Run with input…, as a quick check that the trigger works.",
            WorkspaceId = workspace.Id,
            Trigger = new TriggerConfiguration { TriggerAlias = RunWithInputTrigger.TriggerAlias },
            Steps =
            [
                new StepConfiguration
                {
                    Id = logStepId,
                    ActionAlias = "umbracoAutomate.logMessage",
                    Name = "Log Message",
                    Alias = "logMessage",
                    Settings = new Dictionary<string, object?>
                    {
                        ["message"] = "Run with input: ${ trigger.triggerInput }",
                        ["logLevel"] = "Information",
                    },
                    Position = new StepPosition { X = 250, Y = 230 },
                    ErrorBehavior = StepErrorBehavior.Terminate,
                },
            ],
            // Guid.Empty is the trigger, so this runs the log step first.
            Connections = [new StepConnection { SourceStepId = Guid.Empty, TargetStepId = logStepId }],
        }, Constants.Security.SuperUserKey, cancellationToken);

        await automationService.PublishAutomationAsync(automation.Id, Constants.Security.SuperUserKey, cancellationToken);
        logger.LogInformation("Seeded and published automation '{Alias}'.", AutomationAlias);
    }
}
