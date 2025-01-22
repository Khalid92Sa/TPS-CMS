using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace CMS.Application.ActionFilters
{
    public class LoggingActionFilter : IAsyncActionFilter
    {
        private readonly ILogger<LoggingActionFilter> _logger;
        private const string CorrelationIdHeader = "X-Correlation-ID";
        private string _traceId;

        public LoggingActionFilter(ILogger<LoggingActionFilter> logger) => _logger = logger;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            _traceId = context.HttpContext.TraceIdentifier;

            LogRequestAsync(context);

            Stream originalResponseBodyStream = context.HttpContext.Response.Body;
            using MemoryStream responseBodyMemoryStream = new();
            context.HttpContext.Response.Body = responseBodyMemoryStream;

            ActionExecutedContext resultContext = await next();

            LogResponseAsync(resultContext, responseBodyMemoryStream);

            responseBodyMemoryStream.Seek(0, SeekOrigin.Begin);
            await responseBodyMemoryStream.CopyToAsync(originalResponseBodyStream);
            context.HttpContext.Response.Body = originalResponseBodyStream;

        }

        private void LogRequestAsync(ActionExecutingContext context)
        {
            string requestLog = GenerateBodyRequestDetails(context);
            _logger.LogWarning(requestLog);
        }

        private string GenerateBodyRequestDetails(ActionExecutingContext context)
        {
            string arguments = JsonSerializer.Serialize(context.ActionArguments);
            string apiUrl = context.HttpContext.Request.GetDisplayUrl();
            IPAddress clientIp = context.HttpContext.Connection.RemoteIpAddress;

            string logDetails = $"[Request] - TraceId: {_traceId}, URL: {apiUrl}, IP: {clientIp}, Arguments: {arguments}";

            return logDetails;
        }

        private void LogResponseAsync(ActionExecutedContext context, MemoryStream responseBodyMemoryStream)
        {
            string method = context.HttpContext.Request.Method;
            string url = context.HttpContext.Request.GetDisplayUrl();

            if (context.Exception is null)
            {
                string responseBody = SerializeResponseBodyAsync(context, responseBodyMemoryStream);
                string logMessage = $"[Response] - TraceId: {_traceId}, URL: {url}, HTTP Method: {method}, Status Code: {context.HttpContext.Response.StatusCode}, Body: {responseBody}";

                _logger.LogWarning(logMessage);
            }
            else
            {
                string errorLogMessage = $"[Response] - TraceId: {_traceId}, URL: {url}, HTTP Method: {method} failed.";

                _logger.LogError(context.Exception, errorLogMessage);
            }
        }

        private static string SerializeResponseBodyAsync(ActionExecutedContext context, MemoryStream responseBodyMemoryStream)
        {
            responseBodyMemoryStream.Seek(0, SeekOrigin.Begin);
            string responseBody = new StreamReader(responseBodyMemoryStream).ReadToEnd();

            return context.Result switch
            {
                ObjectResult objectResult => JsonSerializer.Serialize(objectResult.Value),
                JsonResult jsonResult => JsonSerializer.Serialize(jsonResult.Value),
                _ => responseBody
            };
        }


        private static string ReadResponseBodyStreamAsync(MemoryStream responseBodyMemoryStream)
        {
            responseBodyMemoryStream.Seek(0, SeekOrigin.Begin);
            return new StreamReader(responseBodyMemoryStream).ReadToEnd();
        }
    }
}