using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TrucoServer.Utilities;

namespace TrucoServer.Data.Entities
{
    public static class EfHelpers
    {
        private const BindingFlags REFLECTIONFLAGS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

        public static T GetPropValue<T>(object entity, params string[] names)
        {
            if (entity == null)
            {
                return default;
            }

            var type = entity.GetType();

            foreach (var name in names)
            {
                var property = type.GetProperty(name, REFLECTIONFLAGS);

                if (property == null)
                {
                    continue;
                }

                var value = property.GetValue(entity);

                if (value == null)
                {
                    return default;
                }

                T result = ConvertValue<T>(value);

                if (!EqualityComparer<T>.Default.Equals(result, default))
                {
                    return result;
                }
            }

            return default;
        }

        public static object GetNavigation(object entity, params string[] navNames)
        {
            if (entity == null)
            {
                return null;
            }

            var type = entity.GetType();

            foreach (var name in navNames)
            {
                var property = type.GetProperty(name, REFLECTIONFLAGS);
                if (property != null)
                {
                    return property.GetValue(entity);
                }
            }
            return null;
        }

        private static T ConvertValue<T>(object value)
        {
            try
            {
                if (value == null)
                {
                    return default;
                }

                if (typeof(T) == typeof(string))
                {
                    if (value is byte[] bytes)
                    {
                        return (T)(object)Encoding.UTF8.GetString(bytes);
                    }
                    return (T)(object)value.ToString();
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(EfHelpers) + "." + nameof(ConvertValue));
                return default;
            }
        }
    }
}
