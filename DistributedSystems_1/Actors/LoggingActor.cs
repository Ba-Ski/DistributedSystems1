using System;
using Akka.Actor;
using DistributedSystems_1.Logging;

namespace DistributedSystems_1
{
    public class LoggingActor : ReceiveActor
    {
        public static string Path = "/user/Logger";

        private readonly ILogger _logger;

        public LoggingActor(ILogger logger)
        {
            _logger = logger;

            Receive<Log>(message => HandleLog(message));
            Receive<TraceError>(message => HandleTraceError(message));
        }

        private void HandleLog(Log message)
        {
            var prefix = $"[{Sender?.Path}] ";

            _logger.Log(prefix + message.Message, message.Args);
        }

        private void HandleTraceError(TraceError message)
        {
            var prefix = $"[{Sender?.Path}] ";

            _logger.Trace(prefix, message.Exception);
        }

        #region Messages

        public class Log
        {
            public Log(string message, object[] args = null)
            {
                Message = message;
                Args = args;
            }

            public string Message { get; }
            public object[] Args { get; }
        }
        public class TraceError
        {
            public TraceError(Exception exception)
            {
                Exception = exception;
            }

            public Exception Exception { get; }
        }

        #endregion
    }
}