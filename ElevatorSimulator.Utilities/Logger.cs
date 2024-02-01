namespace ElevatorSimulator.Utilities;


public static class Logger
{
    /// <summary>
    /// Logs a message with a timestamp.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Log(string message)
    {
        Console.WriteLine($"{DateTime.Now}: {message}");
    }

    /// <summary>
    /// Logs an exception with a timestamp and stack trace.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    public static void Log(Exception ex)
    {
        Console.WriteLine($"{DateTime.Now}: Exception occurred: {ex.Message}\n{ex.StackTrace}");
    }
}


