using System.Linq;
using System.Text.Json;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Xunit;

namespace RequiemNexus.E2E.Tests.Helpers;

/// <summary>
/// Wraps axe-core analysis for WCAG 2.1 AA gates (Track 2).
/// </summary>
public static class A11yHelper
{
    /// <summary>
    /// Runs axe and fails the test when violations are present.
    /// </summary>
    /// <param name="page">Page to scan.</param>
    /// <param name="tags">Axe rule tags (defaults to WCAG 2.0/2.1 A and AA).</param>
    public static async Task AssertNoViolationsAsync(IPage page, string[]? tags = null)
    {
        string[] effectiveTags = tags ?? ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"];
        var options = new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = [.. effectiveTags] },
        };

        AxeResult results = await page.RunAxe(options);
        bool clean = results.Violations == null || !results.Violations.Any();
        Assert.True(clean, JsonSerializer.Serialize(results.Violations));
    }
}
