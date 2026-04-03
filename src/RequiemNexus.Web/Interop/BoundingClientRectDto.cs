namespace RequiemNexus.Web.Interop;

/// <summary>
/// Maps the return shape of <c>window.getBoundingClientRect</c> for Blazor JS interop (vitae drop click positioning).
/// </summary>
public sealed class BoundingClientRectDto
{
    /// <summary>Gets or sets the top edge of the element relative to the viewport.</summary>
    public double Top { get; set; }

    /// <summary>Gets or sets the left edge of the element relative to the viewport.</summary>
    public double Left { get; set; }

    /// <summary>Gets or sets the layout width of the element.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the layout height of the element.</summary>
    public double Height { get; set; }
}
