using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using peerTube.Screens;
using DistributedServiceProvider;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Contacts;
using System.Net;
using System.Threading;
using ProtoBuf;

namespace peerTube
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Game
    {
        public static readonly Guid Network = Guid.Parse("a678a8fa-6155-448e-b05d-fcc244f48427");

        public static UdpFactory UdpFactory
        {
            get;
            set;
        }

        public DistributedRoutingTable RoutingTable
        {
            get;
            set;
        }

        GraphicsDeviceManager graphics;
        public SpriteBatch SpriteBatch;

        private IScreen screen;
        public IScreen Screen
        {
            get
            {
                return screen;
            }
            set
            {
                if (screen != null)
                    screen.Stop();
                screen = value;
            }
        }

        new public ContentManager Content
        {
            get
            {
                return base.Content;
            }
        }

        public readonly int Port;

        public Game1(int port, Identifier512 id)
        {
            this.Port = port;

            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            this.Window.Title = "Port = " + Port;

            IsMouseVisible = true;
            IsFixedTimeStep = false;

            Screen = new MainMenu(this);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            UdpFactory.Close();
            Screen.Stop();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            Screen.Update(gameTime);

            //if (RoutingTable != null)
            //    ThreadPool.QueueUserWorkItem(a => RoutingTable.Refresh(false));
            if (UdpFactory != null)
                Window.Title = "Port = " + UdpFactory.ListenPort.ToString();

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            Screen.Draw(gameTime);

            base.Draw(gameTime);
        }
    }
}
