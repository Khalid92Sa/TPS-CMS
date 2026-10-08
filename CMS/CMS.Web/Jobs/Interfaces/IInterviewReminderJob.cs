using Hangfire;
using Hangfire.Server;
using System.Threading.Tasks;

namespace CMS.Web.Jobs.Interfaces;

public interface IInterviewReminderJob
{
    // Two overlapping runs (e.g. a manual trigger during the scheduled one) would email twice
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    Task SendReminderEmails(PerformContext performContext = null);
}
