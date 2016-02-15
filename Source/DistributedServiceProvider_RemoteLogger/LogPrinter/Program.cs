using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider_RemoteLogger;
using ConcurrentPipes;
using LogPrinter.Commands;
using System.IO;
using ProtoBuf;
using LoggerMessages;

namespace LogPrinter
{
    public class Program
    {
        static void Main(string[] args)
        {
            string path = "Log " + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + " " + DateTime.Now.Hour + "-" + DateTime.Now.Minute + ".log";
            writer = new BufferedStream(File.Create(path));
            new Program().Run();
        }

        public Dictionary<string, Command> Commands = new Dictionary<string, Command>();
        public Command GetCommand(String keyword)
        {
            Command c = null;
            Commands.TryGetValue(keyword, out c);

            return c;
        }

        public static bool PrintPipesToConsole = true;
        public static bool PrintPipesToFile = true;

        public readonly Dictionary<string, ITypelessListener> Listeners = new Dictionary<string, ITypelessListener>()
            {
                { DRTConstructed.PIPE_NAME, (ITypelessListener)Pipes.RegisterListener(DRTConstructed.PIPE_NAME, new Listener<DRTConstructed>()) },
                { IterativeLookupRequest.PIPE_NAME, (ITypelessListener)Pipes.RegisterListener(IterativeLookupRequest.PIPE_NAME, new Listener<IterativeLookupRequest>()) },
                { IterativeLookupStep.PIPE_NAME, (ITypelessListener)Pipes.RegisterListener(IterativeLookupStep.PIPE_NAME, new Listener<IterativeLookupStep>()) },
                { GeneralMessage.PIPE_NAME, (ITypelessListener)Pipes.RegisterListener(GeneralMessage.PIPE_NAME, new Listener<GeneralMessage>()) },
                { BucketState.PIPE_NAME, (ITypelessListener)Pipes.RegisterListener(BucketState.PIPE_NAME, new Listener<BucketState>()) },
            };

        public bool ContinueLooping = true;
        private void Run()
        {
            AddCommands();

            LoggerServer.Begin();

            //Commands["mute"].Do(this, ("\"" + IterativeLookupRequest.PIPE_NAME + "\"" + "\"" + IterativeLookupStep.PIPE_NAME + "\"").Split(' '));
            //Commands["stfu"].Do(this, "/c /f".Split(' '));

            while (ContinueLooping)
            {
                string[] input = Console.ReadLine().Split(' ');
                try
                {
                    Commands[input[0]].Do(this, input.Skip(1));
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine("Unknown Command, type \"help\"");
                }
            }

            Flush();
            Close();
            LoggerServer.End();
        }

        private void AddCommands()
        {
            foreach (var item in typeof(Command).Assembly.GetTypes().Where(t => t.BaseType == typeof(Command)))
            {
                Command c = item.GetConstructor(Type.EmptyTypes).Invoke(null) as Command;
                if (c != null)
                    Commands.Add(c.Keyword, c);
            }
        }

        public interface ITypelessListener
        {
            bool Muted
            {
                get;
                set;
            }

            string Name
            {
                get;
            }
        }

        private class Listener<T>
            : PipeListener<T>, ITypelessListener
            where T : BaseMessage
        {
            public bool Muted
            {
                get;
                set;
            }

            public string Name
            {
                get;
                private set;
            }

            protected override void Register(string name)
            {
                Name = name;
                Muted = false;

                base.Register(name);
            }

            protected override void AddMessage(T data)
            {
                if (!Muted && Program.PrintPipesToConsole)
                    Console.WriteLine(data.ToString());
                if (Program.PrintPipesToFile)
                    WriteToFile(data);
            }
        }

        private static Stream writer;
        private static void WriteToFile(BaseMessage data)
        {
            lock (writer)
            {
                Serializer.SerializeWithLengthPrefix<BaseMessage>(writer, data, PrefixStyle.Base128);
            }
        }

        public static void Flush()
        {
            lock (writer)
            {
                writer.Flush();
            }
        }

        public static void Close()
        {
            lock (writer)
            {
                writer.Flush();
                writer.Close();
            }
        }
    }
}
