using System;

namespace RequiemNexus.Web.Services;

public record ToastItem(Guid Id, string Title, string Message, ToastType Type, int DurationMs, DateTime CreatedAt);
