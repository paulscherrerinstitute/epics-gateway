using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GWLogger.Backend
{
    public class EqualityComparer<T> : IEqualityComparer<T>
    {
        public EqualityComparer(Func<T, T, bool> comparer)
        {
            this.comparer = comparer;
        }
        public bool Equals(T x, T y)
        {
            return comparer(x, y);
        }

        public int GetHashCode(T obj)
        {
            return 0;
        }

        public Func<T, T, bool> comparer { get; set; }
    }
}