using Umbraco.Cms.Core.Composing;

namespace Umbuntu.Automate.ManualTriggerInput.TestSite.Seeding;

public sealed class TestSiteSeederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        builder.Services.AddHostedService<TestSiteSeeder>();
}
