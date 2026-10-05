namespace Umbuntu.Automate.ManualTriggerInput.Configuration;

/// <summary>
/// Settings for the "Run with input" endpoint, bound from <c>Umbuntu:ManualTriggerInput</c>
/// in appsettings.
/// </summary>
public sealed class ManualTriggerInputOptions
{
    public const string SectionName = "Umbuntu:ManualTriggerInput";

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
