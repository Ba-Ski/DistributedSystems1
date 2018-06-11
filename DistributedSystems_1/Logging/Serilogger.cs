using System;
using Serilog;

namespace DistributedSystems_1.Logging
{
    internal class SerilogFileLogger : ILogger
    {
        private readonly Serilog.Core.Logger _logger;

        public SerilogFileLogger()
        {
            _logger = ConfigureLogger();
        }

        public Serilog.Core.Logger Instance
            => _logger;

        public void Log(string message)
        {
            _logger.Information(message);
        }

        public void Log(string format, params object[] parameters)
        {
            _logger.Information(format, parameters);
        }

        public void Trace(Exception e)
        {
            _logger.Error(e, "Something went wrong:");
        }

        public void Trace(string prefix, Exception exception)
        {
            _logger.Error(exception, prefix);
        }

        private Serilog.Core.Logger ConfigureLogger()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            return new LoggerConfiguration()
                .WriteTo.Async(i => i.File(baseDirectory + "Logs/log.txt"))
                .WriteTo.Console()
                .CreateLogger();
        }
    }
}
