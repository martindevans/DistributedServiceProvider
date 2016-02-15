using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoggerMessages
{
    public static class Extensions
    {
        public static string ToJsonString<T>(this IEnumerable<T> l)
        {
            if (l.Count() == 0)
                return "[]";

            return "[" + l.Select(a => "\"" + a.ToString() +"\"").Aggregate((a, b) => a + b) + "]";
        }
    }
}
