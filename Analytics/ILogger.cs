namespace EastFive.Analytics
{
    public interface ILogger
    {
        void LogInformation(string message);

        void LogTrace(string message);
    }
}