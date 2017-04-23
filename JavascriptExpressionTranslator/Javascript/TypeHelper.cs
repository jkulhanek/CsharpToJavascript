using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Forcoft.Javascript.Helpers
{
    public static class TypeHelper
    {
        public static string GetTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                string gg = type.GetGenericTypeDefinition().Name;
                if (gg.IndexOf("`") > -1)
                {
                    gg = gg.Substring(0, gg.IndexOf("`"));
                }
                return gg;
            }
            else
            {
                return type.Name;
            }
        }
        public static bool IsNullable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        public static Type NullableType(Type type)
        {
            return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) ? type.GetGenericArguments()[0] : null;
        }
        public static bool IsSimpleType(Type type)
        {
            if (IsNullable(type)) type = NullableType(type);
            return type == typeof(string) || type.IsPrimitive || type == typeof(char) || type == typeof(DateTime) || type == typeof(Guid) || type == typeof(TimeSpan) || type.IsEnum;
        }
        public static T To<T>(this IConvertible obj)
        {
            Type t = typeof(T);

            if (t.IsGenericType
                && (t.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                if (obj == null)
                {
                    return (T)(object)null;
                }
                else
                {
                    return (T)Convert.ChangeType(obj, Nullable.GetUnderlyingType(t));
                }
            }
            else
            {
                return (T)Convert.ChangeType(obj, t);
            }
        }

        public static object To(this IConvertible obj,Type t)
        {
            if (t.IsGenericType
                && (t.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                if (obj == null)
                {
                    return (object)null;
                }
                else
                {
                    return Convert.ChangeType(obj, Nullable.GetUnderlyingType(t));
                }
            }
            else
            {
                return Convert.ChangeType(obj, t);
            }
        }

        public static object ToType(this object obj, Type t)
        {
            //var ftype = typeof(Func<,>).MakeGenericType(new Type[]{obj.GetType(), t});
            return obj;
        }

        public static T ToOrDefault<T>
                     (this IConvertible obj)
        {
            try
            {
                return To<T>(obj);
            }
            catch
            {
                return default(T);
            }
        }
        public static object ToOrDefault
                     (this IConvertible obj,Type t)
        {
            try
            {
                return To(obj,t);
            }
            catch
            {
                return null;
            }
        }

        public static bool ToOrDefault<T>
                            (this IConvertible obj,
                             out T newObj)
        {
            try
            {
                newObj = To<T>(obj);
                return true;
            }
            catch
            {
                newObj = default(T);
                return false;
            }
        }

        public static T ToOrOther<T>
                               (this IConvertible obj,
                               T other)
        {
            try
            {
                return To<T>(obj);
            }
            catch
            {
                return other;
            }
        }

        public static bool ToOrOther<T>
                                 (this IConvertible obj,
                                 out T newObj,
                                 T other)
        {
            try
            {
                newObj = To<T>(obj);
                return true;
            }
            catch
            {
                newObj = other;
                return false;
            }
        }

        public static T ToOrNull<T>
                              (this IConvertible obj)
                              where T : class
        {
            try
            {
                return To<T>(obj);
            }
            catch
            {
                return null;
            }
        }

        public static bool ToOrNull<T>
                          (this IConvertible obj,
                          out T newObj)
                          where T : class
        {
            try
            {
                newObj = To<T>(obj);
                return true;
            }
            catch
            {
                newObj = null;
                return false;
            }
        }
    }
}
