namespace ElevatorSimulator.Utilities
{
    public interface ILogger
    {
        void Log(string message);
        void Log(Exception ex);
    }
}