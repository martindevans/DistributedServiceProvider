using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogPrinter.Commands
{
    public abstract class Command
    {
        public abstract string Keyword { get; }

        public abstract IEnumerable<string> Description { get; }

        public abstract IEnumerable<string> DetailedHelp { get; }

        public abstract void Do(Program instance, IEnumerable<string> input);

        protected Dictionary<string, string> ExtractSwitches(IEnumerable<string> input)
        {
            return input.Where((a, i) => i % 2 == 0).Zip(input.Where((t, i) => i % 2 == 1), (a, b) => new KeyValuePair<string, string>(a, b)).ToDictionary(a => a.Key, a => a.Value);
        }
    }
}
