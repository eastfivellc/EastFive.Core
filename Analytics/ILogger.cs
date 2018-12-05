using System;
using System.Diagnostics;

namespace EastFive.Analytics
{
    public interface ILogger
    {
        void LogInformation(string message);

        void LogTrace(string message);

        void LogWarning(string message);
    }

    public class DebugLogger : ILogger
    {
        public void LogInformation(string message)
        {
            Debug.WriteLine(message);
        }

        public void LogTrace(string message)
        {
            Debug.WriteLine(message);
        }

        public void LogWarning(string message)
        {
            Debug.WriteLine(message);
        }
    }

    public class ConsoleLogger : ILogger
    {
        public void LogInformation(string message)
        {
            Console.WriteLine(message);
        }

        public void LogTrace(string message)
        {
            Console.WriteLine(message);
        }

        public void LogWarning(string message)
        {
            Console.WriteLine(message);
        }
    }
}