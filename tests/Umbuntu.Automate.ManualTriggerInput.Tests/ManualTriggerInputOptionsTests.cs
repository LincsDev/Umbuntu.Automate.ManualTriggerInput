using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbuntu.Automate.ManualTriggerInput.Configuration;

namespace Umbuntu.Automate.ManualTriggerInput.Tests;

public class ManualTriggerInputOptionsTests
{
    [Fact]
    public void Defaults_to_1_MB_when_not_configured()
    {
        var options = Resolve(new Dictionary<string, string?>());

        Assert.Equal(1024 * 1024, options.MaxInputBytes);
    }

    [Fact]
    public void Binds_the_limit_from_configuration()
    {
        var options = Resolve(new Dictionary<string, string?>
        {
            ["Umbraco:Automate:ManualTriggerInput:MaxInputBytes"] = "2097152",
        });

        Assert.Equal(2097152, options.MaxInputBytes);
    }

    [Fact]
    public void Still_binds_the_limit_from_the_legacy_section()
    {
        var options = Resolve(new Dictionary<string, string?>
        {
            ["Umbuntu:ManualTriggerInput:MaxInputBytes"] = "2097152",
        });

        Assert.Equal(2097152, options.MaxInputBytes);
    }

    [Fact]
    public void Prefers_the_current_section_over_the_legacy_one()
    {
        var options = Resolve(new Dictionary<string, string?>
        {
            ["Umbuntu:ManualTriggerInput:MaxInputBytes"] = "2097152",
            ["Umbraco:Automate:ManualTriggerInput:MaxInputBytes"] = "3145728",
        });

        Assert.Equal(3145728, options.MaxInputBytes);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Rejects_a_limit_that_is_not_positive(string value)
    {
        var exception = Assert.Throws<OptionsValidationException>(() => Resolve(new Dictionary<string, string?>
        {
            ["Umbraco:Automate:ManualTriggerInput:MaxInputBytes"] = value,
        }));

        Assert.Contains("MaxInputBytes must be greater than zero", exception.Message);
    }

    private static ManualTriggerInputOptions Resolve(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddManualTriggerInputOptions(configuration);

        return services.BuildServiceProvider().GetRequiredService<IOptions<ManualTriggerInputOptions>>().Value;
    }
}
