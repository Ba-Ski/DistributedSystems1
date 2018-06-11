using System;

namespace DistributedSystems_1.Logging
{
    public interface ILogger
    {
        void Log(string message);
        void Log(string format, params object[] parameters);
        void Trace(Exception e);
        void Trace(string prefix, Exception exception);
    }
}
