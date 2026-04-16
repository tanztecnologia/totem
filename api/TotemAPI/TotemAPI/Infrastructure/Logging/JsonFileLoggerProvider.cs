using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TotemAPI.Infrastructure.Logging;

public sealed class JsonFileLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    public JsonFileLoggerProvider(string filePath)
    {
        _filePath = filePath;
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
        _stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(_stream) { AutoFlush = true };
    }

    private readonly string _filePath;
    private readonly FileStream _stream;
    private readonly StreamWriter _writer;
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<string, JsonFileLogger> _loggers = new();
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, c => new JsonFileLogger(c, WriteLine, () => _scopeProvider));
    }

    private void WriteLine(string line)
    {
        lock (_lock)
        {
            _writer.WriteLine(line);
        }
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer.Dispose();
            _stream.Dispose();
        }
    }

    private sealed class JsonFileLogger : ILogger
    {
        public JsonFileLogger(string category, Action<string> write, Func<IExternalScopeProvider> getScopes)
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

            if (state is IEnumerable<KeyValuePair<string, object?>> kvs)
            {
                var fields = new Dictionary<string, object?>();
                foreach (var kv in kvs)
                {
                    if (string.Equals(kv.Key, "{OriginalFormat}", StringComparison.OrdinalIgnoreCase)) continue;
                    if (kv.Value is null) continue;
                    fields[kv.Key] = Sanitize(kv.Value);
                }
                if (fields.Count > 0) payload["fields"] = fields;
            }

            if (scopeFields.Count > 0) payload["scope"] = scopeFields;

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
