using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    public static class LogManager
    {
        private static readonly ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static LogManager()
        {
            log4net.Config.XmlConfigurator.Configure();
        }
        public static void LogFatal(Exception exception, string methodName)
        {
            Log.Fatal($"Error crítico en el sistema al ejecutar {methodName}.", exception);
        }

        public static void LogError(Exception exception, string methodName)
        {
            Log.Error($"Error operativo o de negocio al ejecutar {methodName}.", exception);
        }

        public static void LogWarn(string message, string methodName)
        {
            Log.Warn($"Advertencia en {methodName}: {message}");
        }
    }
}
