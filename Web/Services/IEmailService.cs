using System.Threading.Tasks;

namespace Web.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string message);
    }
}