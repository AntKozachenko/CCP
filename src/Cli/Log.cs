using Core.Configuration;

namespace Cli;

// Мінімальний журнал: рівень береться з Logging:MinimumLevel (Development = Debug, Production = Warning).
public sealed class Log(AppLogLevel minimum)
{
    public void Write(AppLogLevel level, string message)
    {
        if (level < minimum) return;
        var writer = level >= AppLogLevel.Warning ? Console.Error : Console.Out;
        writer.WriteLine($"[{level.ToString().ToUpperInvariant(),-5}] {message}");
    }

    public void Debug(string message) => Write(AppLogLevel.Debug, message);
    public void Info(string message) => Write(AppLogLevel.Information, message);
    public void Warn(string message) => Write(AppLogLevel.Warning, message);
}
