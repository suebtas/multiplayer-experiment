using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets
{
    public static class Extensions
    {
        public static T[] ToArr<T>(this IEnumerable<T> enumerable)
        {
            var list = enumerable as IList<T>;

            if (list != null)
            {
                var arr = new T[list.Count];
                for (var i = 0; i < list.Count; ++i)
                {
                    arr[i] = list[i];
                }
                return arr;
            }
            var j = 0;
            var en = enumerable.GetEnumerator();
            while (en.MoveNext())
            {
                ++j;
            }
            en = enumerable.GetEnumerator();
            var arr2 = new T[j];
            j = 0;
            while (en.MoveNext())
            {
                arr2[j++] = en.Current;
            }
            return arr2;
        }

        public static string JoinAsString<T>(this IEnumerable<T> enumerable, string separator)
        {
            return String.Join(separator, enumerable.Select(e => e == null ? "" : e.ToString()).ToArr());
        }
    }
}