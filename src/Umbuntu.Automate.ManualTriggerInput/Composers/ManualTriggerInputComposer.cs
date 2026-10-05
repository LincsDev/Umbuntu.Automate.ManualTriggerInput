using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbuntu.Automate.ManualTriggerInput.Configuration;

namespace Umbuntu.Automate.ManualTriggerInput.Composers;

/// <summary>
/// Binds <see cref="ManualTriggerInputOptions"/> — the trigger and controller themselves are
/// discovered by Umbraco.Automate.Core and MVC respectively.
/// </summary>
public sealed class ManualTriggerInputComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<ManualTriggerInputOptions>()
            .Bind(builder.Config.GetSection(ManualTriggerInputOptions.SectionName))
            .Validate(o => o.MaxInputBytes > 0, $"{ManualTriggerInputOptions.SectionName}:MaxInputBytes must be greater than zero.")
            .ValidateOnStart();
    }
}
