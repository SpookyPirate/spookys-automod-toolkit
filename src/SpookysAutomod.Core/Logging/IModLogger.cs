namespace SpookysAutomod.Core.Logging;

/// <summary>
/// Simple logging interface for toolkit operations.
/// </summary>
public interface IModLogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
    void Error(string message, Exception ex);
}

/// <summary>
/// Console logger implementation.
/// </summary>
public class ConsoleLogger : IModLogger
{
    private readonly bool _verbose;
    private readonly bool _jsonOutput;

    public ConsoleLogger(bool verbose = false, bool jsonOutput = false)
    {
        _verbose = verbose;
        _jsonOutput = jsonOutput;
    }

    public void Debug(string message)
    {
        if (_verbose && !_jsonOutput)
            Console.WriteLine($"[DEBUG] {message}");
    }

    public void Info(string message)
    {
        if (!_jsonOutput)
            Console.WriteLine(message);
    }

    public void Warning(string message)
    {
        if (!_jsonOutput)
            Console.WriteLine($"[WARN] {message}");
    }

    public void Error(string message)
    {
        if (!_jsonOutput)
            Console.Error.WriteLine($"[ERROR] {message}");
    }

    public void Error(string message, Exception ex)
    {
        if (!_jsonOutput)
        {
            Console.Error.WriteLine($"[ERROR] {message}");
            Console.Error.WriteLine(ex.ToString());
        }
    }
}

/// <summary>
/// Silent logger that suppresses all output (for JSON mode).
/// </summary>
public class SilentLogger : IModLogger
{
    public void Debug(string message) { }
    public void Info(string message) { }
    public void Warning(string message) { }
    public void Error(string message) { }
    public void Error(string message, Exception ex) { }
}
