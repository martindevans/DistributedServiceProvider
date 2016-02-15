using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using peerTube.Multicast;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using System.Threading;
using System.Diagnostics;

namespace peerTube.Screens
{
    public class BroadcastReceive
        :IScreen
    {
        Game1 game;
        BroadcastPeer receiver;

        Texture2D texture;

        int failedFrames = 0;

        int attempts = 0;
        bool connected = false;
        bool connecting = false;

        public BroadcastReceive(Game1 game)
        {
            this.game = game;

            texture = game.Content.Load<Texture2D>("purple1");

            receiver = new BroadcastPeer(game.RoutingTable.LocalIdentifier, false);
            game.RoutingTable.RegisterConsumer(receiver);
        }

        public void Update(GameTime time)
        {
            try
            {
                Connect();
                GetNextFrame();
            }
            catch (Exception e)
            {
                WriteException(e);
            }
        }

        private void Connect()
        {
            if (!connected && !connecting)
            {
                connecting = true;
                ThreadPool.QueueUserWorkItem(a =>
                {
                    try
                    {
                        Interlocked.Increment(ref attempts);

                        var result = receiver.Connect(1000);
                        connected |= result;
                    }
                    catch (Exception e)
                    {
                        WriteException(e);
                    }

                    connecting = false;
                });
            }
        }

        private void GetNextFrame()
        {
            VideoFrame top;
            if (receiver.VideoFrames.TryDequeue(out top))
            {
                if (top.JpegData != null)
                {
                    var t = Texture2D.FromStream(game.GraphicsDevice, new MemoryStream(top.JpegData));
                    if (!t.IsDisposed && t != null)
                        texture = t;
                }

                failedFrames = 0;
                connected = true;
            }
            else
                failedFrames++;

            if (failedFrames > 150)
                connected = false;
        }

        public void Draw(GameTime time)
        {
            try
            {
                game.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

                game.SpriteBatch.Draw(texture, game.GraphicsDevice.Viewport.Bounds, Color.White);

                if (connecting)
                {
                    SpriteFont font = game.Content.Load<SpriteFont>("LargeFont");
                    var dots = "...".Take(time.TotalGameTime.Seconds % 3 + 1).Select(a => a.ToString()).Aggregate("", (a, b) => a + b);
                    string s = "Connecting";
                    var s2 = attempts.ToString();

                    game.SpriteBatch.DrawStringJitter(font, s + dots, new Vector2(game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Height / 2) - font.MeasureString(s) / 2, Color.White, Color.Black);
                    game.SpriteBatch.DrawStringJitter(font, s2, new Vector2(game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Height / 2 + 40) - font.MeasureString(s2) / 2, Color.White, Color.Black);
                }

                float y = -20;
                //game.SpriteBatch.DrawStringJitter(game.Content.Load<SpriteFont>("Font"), "Queue = " + this.receiver.VideoFrames.Count, new Vector2(0, y = y + 20), Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Black);
                //game.SpriteBatch.DrawStringJitter(game.Content.Load<SpriteFont>("Font"), "failed frames = " + failedFrames, new Vector2(0, y = y + 20), Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Black);
                game.SpriteBatch.DrawStringJitter(game.Content.Load<SpriteFont>("Font"), "children = " + receiver.ChildrenCount, new Vector2(0, y = y + 20), Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Black);
                //game.SpriteBatch.DrawStringJitter(game.Content.Load<SpriteFont>("Font"), "finger table = " + receiver.RoutingTable.ContactCount, new Vector2(0, y = y + 20), Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Black);

                game.SpriteBatch.End();
            }
            catch (Exception e)
            {
                WriteException(e);
            }
        }

        private void WriteException(Exception e)
        {
            using (var f = File.Create(Guid.NewGuid().ToString() + ".txt"))
            {
                using (StreamWriter w = new StreamWriter(f))
                {
                    w.WriteLine(e.Message);
                    w.WriteLine(e.TargetSite);
                    w.WriteLine(e.Source);
                    w.WriteLine(e.StackTrace);
                }
            }
        }

        public void Stop()
        {
            
        }
    }
}
