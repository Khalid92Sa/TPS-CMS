using Serilog.Core;
using Serilog.Events;

namespace CMS.Application.CustomLogsEnricher
{
    public class LevelEmojiEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            string emoji = logEvent.Level switch
            {
                LogEventLevel.Debug => "⏳",
                LogEventLevel.Information => "ℹ️",
                LogEventLevel.Warning => "⚠️",
                LogEventLevel.Error => "❌",
                _ => "🔍"
            };
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("LevelEmoji", emoji));
        }
    }
}