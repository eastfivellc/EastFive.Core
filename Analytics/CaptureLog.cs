using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Analytics
{
    public class CaptureLog : ILogger
    {
        private ILogger passthrough;
        private Queue<string> logMessages;

        public CaptureLog(ILogger passthrough)
        {
            this.passthrough = passthrough;
            this.logMessages = new Queue<string>();
        }

        public void LogInformation(string message)
        {
            this.logMessages.Enqueue($"INFORMATION:{message}");
            passthrough.Information(message);
        }

        public void LogTrace(string message)
        {
            this.logMessages.Enqueue($"TRACE:{message}");
            passthrough.Trace(message);
        }

        public void LogWarning(string message)
        {
            this.logMessages.Enqueue($"WARNING:{message}");
            passthrough.Warning(message);
        }

        public string Dump()
        {
            return this.logMessages.Join("\n");
        }
    }
}
