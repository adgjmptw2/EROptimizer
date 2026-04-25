namespace EROptimizer.Core;

public sealed class ErLog : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();
    private readonly bool _echoInfoToConsole;

    public ErLog(string logFilePath, bool echoInfoToConsole = false)
    {
        LogFilePath = logFilePath;
        _echoInfoToConsole = echoInfoToConsole;
        var dir = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        _writer = new StreamWriter(new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true
        };
    }

    public string LogFilePath { get; }

    public static ErLog Create(string baseDirectory, string sessionId, bool echoInfoToConsole = false) =>
        new(Path.Combine(baseDirectory, "logs", $"optimizer_{sessionId}.log"), echoInfoToConsole);

    public void Info(string message) => Write("INFO", message, toConsole: _echoInfoToConsole);
    public void Warn(string message) => Write("WARN", message, toConsole: true);
    public void Error(string message) => Write("ERROR", message, toConsole: true);

    private void Write(string level, string message, bool toConsole)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
        lock (_lock)
        {
            if (toConsole)
                Console.WriteLine(line);
            _writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
        }
    }

    public void Dispose() => _writer.Dispose();
}
