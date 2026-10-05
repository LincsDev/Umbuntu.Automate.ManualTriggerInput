using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Execution;
using Umbraco.Cms.Core.Security;
using Umbuntu.Automate.ManualTriggerInput.Configuration;
using Umbuntu.Automate.ManualTriggerInput.Controllers;
using Umbuntu.Automate.ManualTriggerInput.Triggers;

namespace Umbuntu.Automate.ManualTriggerInput.Tests;

public class RunAutomationWithInputControllerTests
{
    private static readonly Guid AutomationId = Guid.NewGuid();

    private readonly IAutomationService _automationService = Substitute.For<IAutomationService>();
    private readonly IAuthorizationService _authorizationService = Substitute.For<IAuthorizationService>();
    private readonly IAutomationExecutor _executor = Substitute.For<IAutomationExecutor>();
    private readonly ICircuitBreakerService _circuitBreaker = Substitute.For<ICircuitBreakerService>();
    private readonly ManualTriggerInputOptions _options = new() { MaxInputBytes = 16 };

    public RunAutomationWithInputControllerTests()
    {
        // Workspace access granted, whichever AuthorizeAsync overload the base controller uses.
        _authorizationService
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Success());
        _authorizationService
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(AuthorizationResult.Success());

        _circuitBreaker.IsRunAllowedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    [Fact]
    public async Task Unknown_automation_returns_404()
    {
        _automationService.GetAutomationAsync(AutomationId, Arg.Any<CancellationToken>())
            .Returns((Automation?)null);

        var result = await CreateController().TriggerWithInput(AutomationId, Input("hello"), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status404NotFound, StatusCodeOf(result));
        await AssertNotExecuted();
    }

    [Theory]
    [InlineData("umbraco.automate.manual")]
    [InlineData("umbraco.automate.webhook")]
    [InlineData(null)]
    public async Task Automation_with_another_trigger_returns_409(string? triggerAlias)
    {
        GivenAutomation(triggerAlias, AutomationStatus.Published);

        var result = await CreateController().TriggerWithInput(AutomationId, Input("hello"), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status409Conflict, StatusCodeOf(result));
        await AssertNotExecuted();
    }

    [Theory]
    [InlineData(AutomationStatus.Draft)]
    [InlineData(AutomationStatus.Unpublished)]
    public async Task Unpublished_automation_returns_409(AutomationStatus status)
    {
        GivenAutomation(RunWithInputTrigger.TriggerAlias, status);

        var result = await CreateController().TriggerWithInput(AutomationId, Input("hello"), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status409Conflict, StatusCodeOf(result));
        await AssertNotExecuted();
    }

    [Fact]
    public async Task Automation_disabled_by_circuit_breaker_returns_409()
    {
        GivenAutomation(RunWithInputTrigger.TriggerAlias, AutomationStatus.Published);
        _circuitBreaker.IsRunAllowedAsync(AutomationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateController().TriggerWithInput(AutomationId, Input("hello"), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status409Conflict, StatusCodeOf(result));
        await AssertNotExecuted();
    }

    [Theory]
    [InlineData("12345678901234567")] // 17 ASCII bytes
    [InlineData("éééééééée")]         // 9 characters, but 17 UTF-8 bytes
    public async Task Input_over_the_byte_limit_returns_413(string input)
    {
        GivenAutomation(RunWithInputTrigger.TriggerAlias, AutomationStatus.Published);

        var result = await CreateController().TriggerWithInput(AutomationId, Input(input), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, StatusCodeOf(result));
        await AssertNotExecuted();
    }

    [Fact]
    public async Task Input_exactly_at_the_limit_is_accepted()
    {
        GivenAutomation(RunWithInputTrigger.TriggerAlias, AutomationStatus.Published);

        var result = await CreateController().TriggerWithInput(AutomationId, Input("1234567890123456"), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status202Accepted, StatusCodeOf(result));
    }

    [Fact]
    public async Task Valid_request_runs_the_automation_with_the_input_and_returns_202()
    {
        var automation = GivenAutomation(RunWithInputTrigger.TriggerAlias, AutomationStatus.Published);

        var result = await CreateController().TriggerWithInput(AutomationId, Input("hello"), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status202Accepted, StatusCodeOf(result));
        await _executor.Received(1).ExecuteAsync(
            automation,
            "user",
            Arg.Any<string?>(),
            Arg.Is<Dictionary<string, object?>>(d => (string?)d["triggerInput"] == "hello"),
            Arg.Any<CancellationToken>(),
            Arg.Any<IReadOnlyList<Guid>?>());
    }

    [Fact]
    public async Task Null_input_is_passed_on_as_an_empty_string()
    {
        GivenAutomation(RunWithInputTrigger.TriggerAlias, AutomationStatus.Published);

        // What System.Text.Json produces for {"triggerInput": null} despite the non-nullable declaration.
        var result = await CreateController().TriggerWithInput(AutomationId, Input(null!), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status202Accepted, StatusCodeOf(result));
        await _executor.Received(1).ExecuteAsync(
            Arg.Any<Automation>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Is<Dictionary<string, object?>>(d => (string?)d["triggerInput"] == string.Empty),
            Arg.Any<CancellationToken>(),
            Arg.Any<IReadOnlyList<Guid>?>());
    }

    [Fact]
    public void Settings_endpoint_returns_the_configured_limit()
    {
        var result = Assert.IsType<OkObjectResult>(CreateController().GetSettings());

        var settings = Assert.IsType<RunWithInputSettingsResponse>(result.Value);
        Assert.Equal(16, settings.MaxInputBytes);
    }

    private RunAutomationWithInputController CreateController() =>
        new(
            _automationService,
            _authorizationService,
            _executor,
            _circuitBreaker,
            Substitute.For<IBackOfficeSecurityAccessor>(),
            Options.Create(_options))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity("Test")),
                },
            },
        };

    private Automation GivenAutomation(string? triggerAlias, AutomationStatus status)
    {
        var automation = new Automation
        {
            Id = AutomationId,
            Alias = "testAutomation",
            Name = "Test automation",
            Status = status,
            WorkspaceId = Guid.NewGuid(),
            Trigger = triggerAlias is null ? null : new TriggerConfiguration { TriggerAlias = triggerAlias },
        };
        _automationService.GetAutomationAsync(AutomationId, Arg.Any<CancellationToken>()).Returns(automation);
        return automation;
    }

    private static RunWithInputTriggerOutput Input(string value) => new() { TriggerInput = value };

    private static int? StatusCodeOf(IActionResult result) => result switch
    {
        IStatusCodeActionResult withStatus => withStatus.StatusCode,
        _ => null,
    };

    private Task AssertNotExecuted() =>
        _executor.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default!, default, default!, default, default);
}
