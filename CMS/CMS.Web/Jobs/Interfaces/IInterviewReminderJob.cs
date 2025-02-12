using Hangfire.Server;
using System.Threading.Tasks;

namespace CMS.Web.Jobs.Interfaces;

public interface IInterviewReminderJob
{
    Task SendReminderEmails(PerformContext performContext = null);
}
