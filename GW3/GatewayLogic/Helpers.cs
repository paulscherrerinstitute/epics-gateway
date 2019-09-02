using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace GatewayLogic
{
    public static class Helpers
    {
        public static bool Remove<TA, TB>(this ConcurrentDictionary<TA, TB> src, TA key)
        {
            TB outVal;
            return src.TryRemove(key, out outVal);
        }

        public static bool Add<TA, TB>(this ConcurrentDictionary<TA, TB> src, TA key, TB val)
        {
            return src.TryAdd(key, val);
        }

        public static TB Get<TA, TB>(this ConcurrentDictionary<TA, TB> src, TA key)
        {
            TB result;
            src.TryGetValue(key, out result);
            return result;
        }

        public static void RemoveEvents<T>(this T target, string eventName)
        {
            if (ReferenceEquals(target, null)) throw new NullReferenceException("Argument \"target\" may not be null.");
            FieldInfo fieldInfo = typeof(T).GetField(eventName, BindingFlags.Static | BindingFlags.NonPublic);
            if (ReferenceEquals(fieldInfo, null)) throw new ArgumentException(
                string.Concat("The control ", typeof(T).Name, " does not have a property with the name \"", eventName, "\""), nameof(eventName));
            object eventInstance = fieldInfo.GetValue(target);
            PropertyInfo propInfo = typeof(T).GetProperty("Events", BindingFlags.NonPublic | BindingFlags.Instance);
            EventHandlerList list = (EventHandlerList)propInfo.GetValue(target, null);
            list.RemoveHandler(eventInstance, list[eventInstance]);
        }
    }
}
