using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConcurrentPipes;

namespace LogPrinter.Commands
{
    class Stfu
        :Command
    {
        public override string Keyword
        {
            get { return "stfu"; }
        }

        public override void Do(Program instance, IEnumerable<string> input)
        {
            if (input.Count() == 0)
                input = new string[] { "/c" };

            foreach (var toggle in input)
            {
                switch (toggle.ToLower())
                {
                    case "/c":
                        Program.PrintPipesToConsole = !Program.PrintPipesToConsole;
                        Console.WriteLine(Program.PrintPipesToConsole ? "Printing pipeline data to console" : "Pausing printing pipeline data to console");
                        break;
                    case "/f":
                        Program.PrintPipesToFile = !Program.PrintPipesToFile;
                        Console.WriteLine(Program.PrintPipesToFile ? "Printing pipeline data to file" : "Pausing printing pipeline data to file");
                        break;
                    default:
                        break;
                }
            }
        }

        public override IEnumerable<string> Description
        {
            get { yield return "Toggle printing pipeline"; }
        }

        public override IEnumerable<string> DetailedHelp
        {
            get
            {
                yield return "Toggle printing pipeline information";
                yield return "\"stfu /c\" to toggle printing to console";
                yield return "\"stfu /f\" to toggle printing to file";
                yield return "Specifying no option defaults to /c";
            }
        }
    }
}
