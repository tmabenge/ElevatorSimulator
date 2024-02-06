namespace ElevatorSimulator.Utilities;


public class Logger : ILogger
{
    /// <summary>
    /// Logs a message with a timestamp.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Log(string message)
    {
        Console.WriteLine($"{DateTime.Now}: {message}");
    }

    /// <summary>
    /// Logs an exception with a timestamp and stack trace.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    public void Log(Exception ex)
    {
        Console.WriteLine($"{DateTime.Now}: Exception occurred: {ex.Message}\n{ex.StackTrace}");
    }
}


