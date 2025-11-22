using log4net;
using System;
using System.Reflection;

namespace TrucoServer
{
    public static class LogManager
    {
        private static readonly ILog logger = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static LogManager()
        {
            log4net.Config.XmlConfigurator.Configure();
        }

        public static void LogFatal(Exception exception, string methodName)
        {
            string formattedMessage = string.Format("Critical error in the system when running {0}.", methodName);

            logger.Fatal(formattedMessage, exception);
        }

        public static void LogError(Exception exception, string methodName)
        {
            string formattedMessage = string.Format("Operational or business error when executing {0}.", methodName);

            logger.Error(formattedMessage, exception);
        }

        public static void LogWarn(string message, string methodName)
        {
            string formattedMessage = string.Format("Warning on {0}: {1}", methodName, message);

            logger.Warn(formattedMessage);
        }
    }
}
