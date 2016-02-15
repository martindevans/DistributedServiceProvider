using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogPrinter.Commands
{
    public class ExitCommand
        :Command
    {
        public override string Keyword
        {
            get { return "exit"; }
        }

        public override IEnumerable<string> Description
        {
            get { yield return "Close the program"; }
        }

        public override IEnumerable<string> DetailedHelp
        {
            get { yield return "Close the program, network connections and other streams will be gracefully closed"; }
        }

        public override void Do(Program instance, IEnumerable<string> input)
        {
            instance.ContinueLooping = false;
        }
    }
}
