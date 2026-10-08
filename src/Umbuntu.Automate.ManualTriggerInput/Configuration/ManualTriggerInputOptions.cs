namespace Umbuntu.Automate.ManualTriggerInput.Configuration;

/// <summary>
/// Settings for the "Run with input" endpoint, bound from <c>Umbraco:Automate:ManualTriggerInput</c>
/// in appsettings, alongside Automate's own settings.
/// </summary>
public sealed class ManualTriggerInputOptions
{
    public const string SectionName = "Umbraco:Automate:ManualTriggerInput";

    /// <summary>
    /// The section 1.0.0 read from. Still bound, beneath <see cref="SectionName"/>, so existing
    /// settings keep working.
    /// </summary>
    [Obsolete("Use SectionName (Umbraco:Automate:ManualTriggerInput). Scheduled for removal in 2.0.0.")]
    public const string LegacySectionName = "Umbuntu:ManualTriggerInput";

    /// <summary>
    /// Maximum size of the trigger input, in UTF-8 bytes. The input is persisted with every run,
    /// so this guards the workflow tables against oversized payloads. Defaults to 1 MB — far
    /// more than a pasted JSON object normally needs.
    /// </summary>
    public long MaxInputBytes { get; set; } = 1024 * 1024;

    /// <summary>
    /// The raw request body limit. The body is the input wrapped in a JSON string, and escaping
    /// (quotes, newlines, backslashes) can grow it, so allow headroom above <see cref="MaxInputBytes"/>.
    /// The exact limit is enforced on the decoded input.
    /// </summary>
    internal long MaxRequestBodyBytes => (MaxInputBytes * 2) + 4096;
}
