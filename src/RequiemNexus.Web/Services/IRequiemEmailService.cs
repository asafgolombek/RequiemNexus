using Microsoft.AspNetCore.Identity;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Services;

public interface IRequiemEmailService : IEmailSender<ApplicationUser>
{
    Task SendEmailChangeLinkAsync(ApplicationUser user, string newEmail, string changeLink);
}
