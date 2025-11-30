using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Utilities
{
    public static class ServerException
    {
        private static readonly Dictionary<Type, Action<Exception, string>> exceptionHandlers = new Dictionary<Type, Action<Exception, string>>
        {
            {
                typeof(SqlException), HandleSqlException
            },
            {
                typeof(DbUpdateException), HandleDbUpdateException
            },
            {
                typeof(ArgumentException), HandleArgumentException
            },
            {
                typeof(DbEntityValidationException), HandleDbEntityValidationException
            },
            {
                typeof(FaultException), HandleFaultException
            },
            {
                typeof(CommunicationException), HandleCommunicationException
            },
            {
                typeof(TimeoutException), HandleTimeoutException
            },
            {
                typeof(SmtpException), HandleSmtpException
            },
            {
                typeof(JsonException), HandleJsonException
            },
            {
                typeof(InvalidOperationException), HandleInvalidOperationException
            },
            {
                typeof(FileNotFoundException), HandleFileNotFoundException
            },
            {
                typeof(IOException), HandleIOException
            },
            {
                typeof(IndexOutOfRangeException), HandleIndexOutOfRangeException
            },
            {
                typeof(InvalidCastException), HandleInvalidCastException
            },
            {
                typeof(FormatException), HandleFormatException
            },
            {
                typeof(OutOfMemoryException), HandleOutOfMemoryException
            },
            {
                typeof(ArgumentOutOfRangeException), HandleArgumentOutOfRangeException
            },
            {
                typeof(KeyNotFoundException), HandleKeyNotFoundException
            },
            {
                typeof(CryptographicException), HandleCryptographicException
            },
            {
                typeof(SmtpFailedRecipientException), HandleSmtpFailedRecipientException
            },
            {
                typeof(ConfigurationErrorsException), HandleConfigurationErrorsException
            }
        };

        

        public static void HandleException(Exception ex, string methodName)
        {
            var exceptionType = ex.GetType();

            if (exceptionHandlers.TryGetValue(exceptionType, out var handler))
            {
                handler(ex, methodName);
            }
            else
            {
                HandleGenericException(ex, methodName);
            }
        }

        private static void HandleSqlException(Exception ex, string methodName)
        {
            var sqlEx = (SqlException)ex;
            LogManager.LogError(sqlEx, $"{methodName} - SQL server critical error (Number: {sqlEx.Number})");
        }

        private static void HandleDbUpdateException(Exception ex, string methodName)
        {
            LogManager.LogError(ex, $"{methodName} - Database update error");
        }

        private static void HandleDbEntityValidationException(Exception ex, string methodName)
        {
            LogManager.LogError(ex, $"{methodName} - Entity validation failed");
        }

        private static void HandleArgumentException(Exception ex, string methodName)
        {
            LogManager.LogWarn(ex.Message, $"{methodName} - Invalid argument");
        }

        private static void HandleFaultException(Exception ex, string methodName)
        {
            LogManager.LogWarn($"Fault generated in {methodName}: {ex.Message}", methodName);
        }

        private static void HandleCommunicationException(Exception ex, string methodName)
        {
            LogManager.LogWarn($"Communication lost in {methodName}: {ex.Message}", methodName);
        }

        private static void HandleTimeoutException(Exception ex, string methodName)
        {
            LogManager.LogWarn($"Operation timed out in {methodName}", methodName);
        }

        private static void HandleIndexOutOfRangeException(Exception ex, string methodName)
        {
            LogManager.LogWarn(ex.Message, $"{methodName} - Index out of the range");
        }

        private static void HandleSmtpException(Exception ex, string methodName)
        {
            LogManager.LogError(ex, $"{methodName} - Email service error");
        }

        private static void HandleJsonException(Exception ex, string methodName)
        {
            LogManager.LogFatal(ex, $"{methodName} - JSON parsing fatal error");
        }

        private static void HandleInvalidOperationException(Exception ex, string methodName)
        {
            LogManager.LogError(ex, $"{methodName} - Invalid operation");
        }

        private static void HandleIOException(Exception exception, string arg2)
        {
            throw new NotImplementedException();
        }

        private static void HandleFileNotFoundException(Exception ex, string methodName)
        {
            LogManager.LogFatal(ex, $"{methodName} - Not found file");
        }

        private static void HandleFormatException(Exception ex, string methodName)
        {
            LogManager.LogError(ex, $"{methodName} - Invalid format");
        }

        private static void HandleInvalidCastException(Exception ex, string methodName)
        {
            LogManager.LogError(ex, $"{methodName} - Invalid cast");
        }

        private static void HandleOutOfMemoryException(Exception ex, string methodName)
        {
            LogManager.LogError(ex, $"{methodName} - Out of memory");
        }

        private static void HandleArgumentOutOfRangeException(Exception ex, string methodName)
        {
            LogManager.LogError(ex, $"{methodName} - Argument out of range");
        }

        private static void HandleKeyNotFoundException(Exception ex, string methodName)
        {
            LogManager.LogWarn(ex.Message, $"{methodName} - Key not found");
        }

        private static void HandleCryptographicException(Exception ex, string methodName)
        {
            LogManager.LogError(ex, $"{methodName} - Cryptographic provider error");
        }

        private static void HandleSmtpFailedRecipientException(Exception ex, string methodName)
        {
            LogManager.LogError(ex, $"{methodName} - Smtp recipient error");
        }

        private static void HandleConfigurationErrorsException(Exception ex, string methodName)
        {
            LogManager.LogError(ex, $"{methodName} - Configuration error");
        }

        private static void HandleGenericException(Exception ex, string methodName)
        {
            LogManager.LogFatal(ex, $"{methodName} - Unhandled exception");
        }
    }
}
