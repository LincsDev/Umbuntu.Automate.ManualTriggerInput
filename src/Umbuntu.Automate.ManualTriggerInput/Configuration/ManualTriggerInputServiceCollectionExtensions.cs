using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Umbuntu.Automate.ManualTriggerInput.Configuration;

internal static class ManualTriggerInputServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="ManualTriggerInputOptions"/> from <see cref="ManualTriggerInputOptions.SectionName"/>
    /// and fails startup if the size limit isn't positive, so a typo can't silently disable it.
    /// </summary>
    public static OptionsBuilder<ManualTriggerInputOptions> AddManualTriggerInputOptions(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services.AddOptions<ManualTriggerInputOptions>()
            .Bind(configuration.GetSection(ManualTriggerInputOptions.SectionName))
            .Validate(o => o.MaxInputBytes > 0, $"{ManualTriggerInputOptions.SectionName}:MaxInputBytes must be greater than zero.")
            .ValidateOnStart();
}
