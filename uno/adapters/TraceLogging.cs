// Fork-owned, not an Uno workaround: the Uno Calculator ships no telemetry, so these
// are no-ops that exist purely so upstream's TraceLogger.cs compiles unmodified.

namespace TraceLogging;

using Windows.Foundation.Diagnostics;

internal sealed class TraceLoggingCommon
{
    private static readonly TraceLoggingCommon s_instance = new();

    public static TraceLoggingCommon GetInstance() => s_instance;

    public void LogLevel1Event(string eventName, LoggingFields fields) { }

    public void LogLevel2Event(string eventName, LoggingFields fields) { }

    public void LogLevel3Event(string eventName, LoggingFields fields) { }
}
