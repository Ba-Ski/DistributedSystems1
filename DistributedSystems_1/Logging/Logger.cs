using System;

namespace DistributedSystems_1.Logging
{

    internal class Logger
    {
        public static ILogger LoggerInstance
        {
            get; set;
        }

        static Logger()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => LoggerInstance.Trace(e.ExceptionObject as Exception);
        }

        public static void Trace(Exception e)
        {
            LoggerInstance.Trace(e);
        }

        public static void Log(string format, params object[] parameters)
        {
            LoggerInstance.Log(format, parameters);
        }
    }
}
