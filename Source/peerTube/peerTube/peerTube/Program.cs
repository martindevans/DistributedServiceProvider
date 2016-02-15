using System;
using System.Threading;
using DistributedServiceProvider.Base;
using System.Diagnostics;
using DistributedServiceProvider_RemoteLogger;

namespace peerTube
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                LoggerClient.Begin("localhost", true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

#if !DEBUG
            try
            {
#endif
                using (Game1 game = new Game1(int.Parse(args[0]), Identifier512.Parse(args[1])))
                {
                    game.Run();
                }
#if !DEBUG
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
#endif
        }
    }
#endif
}

