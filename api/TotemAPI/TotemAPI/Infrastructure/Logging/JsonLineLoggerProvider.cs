using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TotemAPI.Infrastructure.Logging;

public sealed class JsonLineLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    public JsonLineLoggerProvider(Action<string> writeLine)
    {
        _writeLine = writeLine;
    }

    private readonly Action<string> _writeLine;
    private readonly ConcurrentDictionary<string, JsonLineLogger> _loggers = new();
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, c => new JsonLineLogger(c, _writeLine, () => _scopeProvider));
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public void Dispose() { }

    private sealed class JsonLineLogger : ILogger
    {
        public JsonLineLogger(string category, Action<string> write, Func<IExternalScopeProvider> getScopes)
        {
            _category = category;
            _write = write;
            _getScopes = getScopes;
        }

        private readonly string _category;
        private readonly Action<string> _write;
        private readonly Func<IExternalScopeProvider> _getScopes;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            if (!IsEnabled(logLevel)) return;

            var now = DateTimeOffset.UtcNow;
            var msg = formatter(state, exception);
            var activity = System.Diagnostics.Activity.Current;

            var fields = new Dictionary<string, object?>();
            var scopeFields = new Dictionary<string, object?>();

            _getScopes().ForEachScope<object?>(
                (scope, _) =>
                {
                    if (scope is IEnumerable<KeyValuePair<string, object?>> kvs)
                    {
                        foreach (var kv in kvs)
                        {
                            if (kv.Value is null) continue;
                            scopeFields[kv.Key] = Sanitize(kv.Value);
                        }
                    }
                },
                state: null
            );

            if (state is IEnumerable<KeyValuePair<string, object?>> stateKvs)
            {
                foreach (var kv in stateKvs)
                {
                    if (string.Equals(kv.Key, "{OriginalFormat}", StringComparison.OrdinalIgnoreCase)) continue;
                    if (kv.Value is null) continue;
                    fields[kv.Key] = Sanitize(kv.Value);
                }
            }

            var tenantId = scopeFields.TryGetValue("tenant_id", out var t) ? t?.ToString() : "-";
            var userId = scopeFields.TryGetValue("user_id", out var u) ? u?.ToString() : "-";

            var payload = new Dictionary<string, object?>
            {
                ["ts"] = now.ToString("O"),
                ["level"] = logLevel.ToString(),
                ["category"] = _category,
                ["eventId"] = eventId.Id,
                ["eventName"] = string.IsNullOrWhiteSpace(eventId.Name) ? null : eventId.Name,
                ["message"] = msg,
                ["tenant_id"] = tenantId,
                ["user_id"] = userId,
                ["traceId"] = activity?.TraceId.ToString(),
                ["spanId"] = activity?.SpanId.ToString(),
            };

            if (scopeFields.Count > 0) payload["scope"] = scopeFields;
            if (fields.Count > 0) payload["fields"] = fields;

            if (exception is not null)
            {
                payload["exception"] = new
                {
                    type = exception.GetType().FullName,
                    message = exception.Message,
                    stackTrace = exception.StackTrace
                };
            }

            _write(JsonSerializer.Serialize(payload));
        }

        private static object? Sanitize(object value)
        {
            return value switch
            {
                string => value,
                bool => value,
                byte => value,
                sbyte => value,
                short => value,
                ushort => value,
                int => value,
                uint => value,
                long => value,
                ulong => value,
                float => value,
                double => value,
                decimal => value,
                Guid => value.ToString(),
                DateTime => ((DateTime)value).ToString("O"),
                DateTimeOffset => ((DateTimeOffset)value).ToString("O"),
                Enum => value.ToString(),
                _ => value.ToString()
            };
        }
    }
}
