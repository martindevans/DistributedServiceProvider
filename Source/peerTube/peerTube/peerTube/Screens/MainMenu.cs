using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using DistributedServiceProvider.Contacts;
using System.IO;
using System.Net;
using DistributedServiceProvider.Base;
using ProtoBuf;
using DistributedServiceProvider;

namespace peerTube.Screens
{
    public class MainMenu
        :IScreen
    {
        Rectangle receiveButton;
        Rectangle broadcastButton;

        MouseState previousMouse;
        Game1 game;

        float receiveLerp = 0;
        float broadcastLerp = 0;

        private const float lerpUp = 5;
        private const float lerpDown = 3;

        public MainMenu(Game1 game)
        {
            this.game = game;
            broadcastButton = new Rectangle(game.GraphicsDevice.Viewport.X, game.GraphicsDevice.Viewport.Y, game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Height);
            receiveButton = new Rectangle(game.GraphicsDevice.Viewport.X + game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Y, game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Height);
        }

        public void Update(GameTime time)
        {
            MouseState mState = Mouse.GetState();
            bool activateButton = mState.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed;
            if (receiveButton.Contains(mState.X, mState.Y))
            {
                receiveLerp += (float)time.ElapsedGameTime.TotalSeconds * lerpUp;

                if (activateButton)
                {
                    Serializer.PrepareSerializer<UdpProxy>();

                    SetupRoutingTable();

                    game.Screen = new BootstrapLoad(game, new BroadcastReceive(game), null, (complete, print) =>
                    {
                        var contacts = File.ReadAllLines("BootstrapContacts.txt").Select(s => s.Split(' ')).Select(s =>
                            {
                                try
                                {
                                    var ip = IPAddress.Parse(s[0]);
                                    int port = int.Parse(s[1]);
                                    Guid networkId = Game1.Network;
                                    Identifier512 id = Identifier512.Parse(s[2]);

                                    return Game1.UdpFactory.Construct(new IPEndPoint(ip, port), networkId, id);
                                }
                                catch (Exception e)
                                {
                                    print(e.ToString());
                                    return null;
                                }
                            })
                            .Where(a => a != null)
                            .ToList();

                        game.RoutingTable.Bootstrap(c =>
                            {
                                print(c.Identifier.ToString());
                            }, contacts);

                        complete();
                    });
                }
            }
            else
                receiveLerp -= (float)time.ElapsedGameTime.TotalSeconds * lerpDown;
            
            if (broadcastButton.Contains(mState.X, mState.Y))
            {
                broadcastLerp += (float)time.ElapsedGameTime.TotalSeconds * lerpUp;

                if (activateButton)
                {
                    SetupRoutingTable(game.Port);
                    game.Screen = new BroadcastCapture(game, (long)(1024 * 1024 * 0.25f));
                }
            }
            else
                broadcastLerp -= (float)time.ElapsedGameTime.TotalSeconds * lerpDown;

            broadcastLerp = MathHelper.Clamp(broadcastLerp, 0, 1);
            receiveLerp = MathHelper.Clamp(receiveLerp, 0, 1);

            previousMouse = mState;
        }

        private void SetupRoutingTable(int port = -1)
        {
            if (port == -1)
            {
                Random r = new Random();
                port = r.Next(2000, 10000);
            }
            Game1.UdpFactory = new UdpFactory(port);
            ProxyContact.RegisterFactory(Game1.UdpFactory);

            game.RoutingTable = new DistributedRoutingTable(Identifier512.NewIdentifier(), (a) =>
            {
                var addr = Dns.GetHostAddresses(Dns.GetHostName()).Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).First();

                return Game1.UdpFactory.Construct(new IPEndPoint(addr, port), a.NetworkId, a.LocalIdentifier);
            }, Game1.Network, new Configuration()
            {
                BucketRefreshPeriod = TimeSpan.FromSeconds(120),
            });

            Game1.UdpFactory.Begin(game.RoutingTable);
        }

        public void Draw(GameTime time)
        {
            game.SpriteBatch.Begin();

            Point mouse = new Point(Mouse.GetState().X, Mouse.GetState().Y);

            Color dark = new Color(12,12,12);

            game.SpriteBatch.Draw(game.Content.Load<Texture2D>("CroppedConnector"), receiveButton, Color.Lerp(dark, Color.WhiteSmoke, receiveLerp));
            game.SpriteBatch.Draw(game.Content.Load<Texture2D>("loq_airou_crop"), broadcastButton, Color.Lerp(dark, Color.WhiteSmoke, broadcastLerp));

            SpriteFont font = game.Content.Load<SpriteFont>("LargeFont");
            string receive = "Listen";
            game.SpriteBatch.DrawStringJitter(font, receive, receiveButton.Center.AsVector2() - font.MeasureString(receive) / 2, Color.GhostWhite, Color.Black);

            string broadcast = "Shout";
            game.SpriteBatch.DrawStringJitter(font, broadcast, broadcastButton.Center.AsVector2() - font.MeasureString(broadcast) / 2, Color.GhostWhite, Color.Black);

            game.SpriteBatch.End();
        }

        public void Stop()
        {
            
        }
    }
}
