using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Forcoft.Extensions.Enumerable
{
    public static class EnumerableHelpers
    {
        public static IEnumerable<T> JoinValues<T, T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second, Func<T1, T2, T> resultSelector)
        {
            var enum1 = first.GetEnumerator();
            var enum2 = second.GetEnumerator();
            while (enum1.MoveNext())
            {
                if (!enum2.MoveNext())
                {
                    throw new Exception("Input Enumerables hava not same elements count");
                }
                yield return resultSelector(enum1.Current, enum2.Current);
            }
            if (enum2.MoveNext())
                throw new Exception("Input Enumerables hava not same elements count");
            enum1.Dispose();
            enum2.Dispose();
        }
    }
}
