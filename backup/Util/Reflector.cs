using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CustomSocialSort.Util
{
    internal static class Reflector
    {
        public static T? GetField<T>(object instance, string fieldName)
        {
            var f = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f != null && typeof(T).IsAssignableFrom(f.FieldType))
                return (T?)f.GetValue(instance);
            return default;
        }

        public static bool TrySetField<T>(object instance, string fieldName, T value)
        {
            var f = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f == null) return false;
            if (!f.FieldType.IsAssignableFrom(typeof(T))) return false;
            f.SetValue(instance, value);
            return true;
        }

        public static T? GetFirstFieldOfType<T>(object instance)
        {
            var f = instance.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(fi => typeof(T).IsAssignableFrom(fi.FieldType));
            return f != null ? (T?)f.GetValue(instance) : default;
        }

        public static object? CallMethod(object instance, string methodName, params object?[] args)
        {
            var m = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return m?.Invoke(instance, args);
        }

        public static IEnumerable<T> GetEnumerableField<T>(object instance, string fieldName)
        {
            var obj = GetField<object>(instance, fieldName);
            if (obj is IEnumerable<T> enumerable) return enumerable;
            return Enumerable.Empty<T>();
        }
    }
}