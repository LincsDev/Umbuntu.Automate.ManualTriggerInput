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
    public void Compose(IUmbracoBuilder builder) =>
        builder.Services.AddManualTriggerInputOptions(builder.Config);
}
