using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    static class EfHelpers
    {
        public static T GetPropValue<T>(object o, params string[] names)
        {
            if (o == null) return default(T);

            var t = o.GetType();
            foreach (var name in names)
            {
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                {
                    var val = p.GetValue(o);
                    if (val == null) return default(T);

                    try
                    {
                        if (typeof(T) == typeof(string))
                        {
                            if (val is byte[] b)
                                return (T)(object)Encoding.UTF8.GetString(b);
                            return (T)(object)val.ToString();
                        }

                        return (T)Convert.ChangeType(val, typeof(T));
                    }
                    catch
                    {
                        return default(T);
                    }
                }
            }
            return default(T);
        }

        public static object GetNavigation(object entity, params string[] navNames)
        {
            if (entity == null) return null;
            var t = entity.GetType();
            foreach (var n in navNames)
            {
                var prop = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null)
                    return prop.GetValue(entity);
            }
            return null;
        }
    }
}
