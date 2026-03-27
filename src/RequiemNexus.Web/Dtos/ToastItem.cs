using RequiemNexus.Web.Enums;

namespace RequiemNexus.Web.Dtos;

public record ToastItem(Guid Id, string Title, string Message, ToastType Type, int DurationMs, DateTime CreatedAt);
