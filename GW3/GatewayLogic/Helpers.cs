using System.Collections.Concurrent;

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
    }
}
