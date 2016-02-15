using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoggerMessages
{
    static class MessageStringBuilder
    {
        public static string BuildMessageString(string name, IEnumerable<KeyValuePair<string, object>> values)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(name);
            builder.Append("\n{");
            builder.Append(string.Join(",", values.Select(a => "\n\t\"" + a.Key + "\":\"" + a.Value + "\"")));
            builder.Append("\n}");

            return builder.ToString();
        }
    }
}
