using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace PushDashboard.Logging;

/// <summary>
/// AsyncLocal-based logging context for request-scoped log data
/// </summary>
public static class LoggingContext
{
    private static readonly AsyncLocal<LoggingContextData?> _current = new();

    public static LoggingContextData? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public static void Set(string correlationId, string? companyName, string? userId,
        string? companyId, string? requestPath, string? requestMethod)
    {
        _current.Value = new LoggingContextData
        {
            CorrelationId = correlationId,
            CompanyName = companyName,
            UserId = userId,
            CompanyId = companyId,
            RequestPath = requestPath,
            RequestMethod = requestMethod
        };
    }

    public static void Clear()
    {
        _current.Value = null;
    }
}

public class LoggingContextData
{
    public string? CorrelationId { get; set; }
    public string? CompanyName { get; set; }
    public string? UserId { get; set; }
    public string? CompanyId { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
}

public class JsonLogFormatter : ConsoleFormatter, IDisposable
{
    private readonly IDisposable? _optionsReloadToken;
    private JsonLogFormatterOptions _formatterOptions;
    private static readonly string _hostName = Environment.MachineName;
    private static readonly string _appName = "PushDashboard";

    public const string FormatterName = "custom-json";

    public JsonLogFormatter(IOptionsMonitor<JsonLogFormatterOptions> options)
        : base(FormatterName)
    {
        _formatterOptions = options.CurrentValue;
        _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
    }

    private void ReloadLoggerOptions(JsonLogFormatterOptions options)
    {
        _formatterOptions = options;
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (message is null)
            return;

        var now = DateTimeOffset.UtcNow;

        // Get context from AsyncLocal
        var context = LoggingContext.Current;

        var logObject = new
        {
            correlationId = context?.CorrelationId ?? Guid.NewGuid().ToString(),
            agentName = _formatterOptions.AgentName ?? "PushDashboard",
            severity = GetSeverity(logEntry.LogLevel),
            app = _appName,
            service = logEntry.Category,
            env = _formatterOptions.Environment ?? "Development",
            host = _hostName,
            message = message,
            stackTrace = logEntry.Exception?.StackTrace,
            date = now.ToString("yyyy-MM-dd"),
            timestamp = now.ToString("o"),
            companyName = context?.CompanyName,
            userId = context?.UserId,
            companyId = context?.CompanyId,
            requestPath = context?.RequestPath,
            requestMethod = context?.RequestMethod
        };

        var json = JsonSerializer.Serialize(logObject, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        textWriter.WriteLine(json);
    }

    private static string GetSeverity(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRITICAL",
            LogLevel.None => "NONE",
            _ => "UNKNOWN"
        };
    }

    public void Dispose()
    {
        _optionsReloadToken?.Dispose();
    }
}

public class JsonLogFormatterOptions : ConsoleFormatterOptions
{
    public string? AgentName { get; set; }
    public string? Environment { get; set; }
}

