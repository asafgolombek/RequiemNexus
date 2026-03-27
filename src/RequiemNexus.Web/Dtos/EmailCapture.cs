namespace RequiemNexus.Web.Dtos;

/// <summary>
/// A single message captured by <see cref="RequiemNexus.Web.Services.TestEmailSink"/> during the <c>Testing</c> environment.
/// </summary>
/// <param name="To">Recipient address.</param>
/// <param name="Subject">Message subject.</param>
/// <param name="HtmlBody">HTML body.</param>
public sealed record EmailCapture(string To, string Subject, string HtmlBody);
