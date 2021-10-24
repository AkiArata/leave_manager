using System.Threading.Tasks;

namespace LeaveManager.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}