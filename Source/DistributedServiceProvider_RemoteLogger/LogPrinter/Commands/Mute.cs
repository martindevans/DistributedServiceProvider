using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogPrinter.Commands
{
    public class Mute
        :Command
    {
        public override string Keyword
        {
            get { return "mute"; }
        }

        public override IEnumerable<string> Description
        {
            get { yield return "Provides per pipe muting"; }
        }

        public override IEnumerable<string> DetailedHelp
        {
            get
            {
                yield return "Provides per pipe muting";
                yield return "\"mute \"PIPE NAME HERE\" \"PIPE 2 NAME HERE\" ...\" will toggle all listed pipes printing to the console";
                yield return "\"mute\" with no arguments will list pipes and their mute status";
            }
        }

        public override void Do(Program instance, IEnumerable<string> input)
        {
            if (input.Count() == 0)
            {
                Console.WriteLine("Muted:");
                foreach (KeyValuePair<string, LogPrinter.Program.ITypelessListener> item in instance.Listeners.Where(a => a.Value.Muted))
                    Console.WriteLine("\t" + item.Key);

                Console.WriteLine("Not Muted:");
                foreach (KeyValuePair<string, LogPrinter.Program.ITypelessListener> item in instance.Listeners.Where(a => !a.Value.Muted))
                    Console.WriteLine("\t" + item.Key);
            }
            else
            {
                string arguments = input.Aggregate((a, b) => a + " " + b);

                List<string> pipes = new List<string>();
                while (arguments.Length > 0)
                {
                    int open = arguments.IndexOf("\"");
                    int close = arguments.Substring(open + 1).IndexOf("\"") + open;

                    if (open < 0 || close < 0)
                        break;

                    string n = arguments.Substring(open + 1, close - open);
                    arguments = arguments.Remove(open, (close - open) + 2);

                    pipes.Add(n);
                }

                foreach (var item in pipes)
                {
                    Program.ITypelessListener l;
                    if (instance.Listeners.TryGetValue(item, out l))
                    {
                        l.Muted = !l.Muted;
                        Console.WriteLine(item + " is " + (l.Muted ? "muted" : "unmuted"));
                    }
                    else
                    {
                        Console.WriteLine("Unknown : \"" + item + "\"");
                    }
                }
            }
        }
    }
}
