using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogPrinter.Commands
{
    public class HelpCmd
        :Command
    {
        public override string Keyword
        {
            get { return "help"; }
        }

        public override IEnumerable<string> Description
        {
            get { return DetailedHelp; }
        }

        public override IEnumerable<string> DetailedHelp
        {
            get
            {
                yield return "Shows a list of commands with descriptions";
                yield return "Specify a specific command \"help foo\" for detailed information";
            }
        }

        public override void Do(Program instance, IEnumerable<string> input)
        {
            if (input.Count() == 0)
            {
                foreach (var item in instance.Commands)
                {
                    Console.WriteLine(item.Key);
                    foreach (var l in item.Value.Description)
                        Console.WriteLine("\t" + l);
                }
            }
            else
            {
                string cmd = input.First();
                Command c = instance.GetCommand(cmd);

                if (c == null)
                    Console.WriteLine("Unknown command \"" + cmd + "\"");
                else
                {
                    Console.WriteLine(c.Keyword);
                    foreach (var l in c.DetailedHelp)
                        Console.WriteLine("\t" + l);
                }
            }
        }
    }
}
