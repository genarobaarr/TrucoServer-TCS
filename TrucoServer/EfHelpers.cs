using System;
using System.Reflection;
using System.Text;

namespace TrucoServer
{
    static class EfHelpers //El static se cambia a public para poder probarlo y viceversa para correr el server
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
                if (property != null)
                {
                    var value = property.GetValue(entity);
                    if (value == null)
                    {
                        return default;
                    }

                    try
                    {
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
                    catch
                    {
                        return default(T);
                    }
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
    }
}
