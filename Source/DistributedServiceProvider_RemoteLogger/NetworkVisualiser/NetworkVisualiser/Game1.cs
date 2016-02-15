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
using DistributedServiceProvider_RemoteLogger;
using DistributedServiceProvider.Base;
using System.Collections.Concurrent;
using ConcurrentPipes;
using LoggerMessages;
using System.Numerics;
using System.Collections;
using System.Threading;

namespace NetworkVisualiser
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        int positionsLastCalculated = 0;
        ConcurrentQueue<DRTConstructed> constructionQueue = new ConcurrentQueue<DRTConstructed>();
        SortedDictionary<Identifier512, Peer> peers = new SortedDictionary<Identifier512, Peer>();

        ConcurrentQueue<IterativeLookupRequest> lookupStartQueue = new ConcurrentQueue<IterativeLookupRequest>();
        ConcurrentQueue<IterativeLookupComplete> lookupEndQueue = new ConcurrentQueue<IterativeLookupComplete>();
        ConcurrentQueue<IterativeLookupStep> lookupStepQueue = new ConcurrentQueue<IterativeLookupStep>();
        Dictionary<Guid, Lookup> lookups = new Dictionary<Guid, Lookup>();

        PipeListener<DRTConstructed> constructionListener;
        PipeListener<BucketState> bucketStateListener;
        PipeListener<GeneralMessage> generalMessageListener;
        PipeListener<IterativeLookupRequest> requestListener;
        PipeListener<IterativeLookupStep> stepListener;
        PipeListener<IterativeLookupComplete> requestCompleteListener;
        PipeListener<ClearQueries> clearListener;

        private ConcurrentQueue<string> messages = new ConcurrentQueue<string>();

        SpriteFont font;
        Texture2D whitePixel;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            LoggerServer.Begin();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            IsMouseVisible = true;
            IsFixedTimeStep = false;

            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
            graphics.ApplyChanges();

            SetupListeners();

            base.Initialize();
        }

        private void PrintMessage(string msg)
        {
            messages.Enqueue(msg);
            string s;
            while (messages.Count > 5 && messages.TryDequeue(out s)) { }
        }

        private void SetupListeners()
        {
            constructionListener = Pipes.RegisterListener<DRTConstructed>(DRTConstructed.PIPE_NAME, new Listener<DRTConstructed>(a => constructionQueue.Enqueue(a)));

            bucketStateListener = Pipes.RegisterListener<BucketState>(BucketState.PIPE_NAME, new Listener<BucketState>(a =>
            {
                Peer p;
                if (peers.TryGetValue(a.LocalId, out p))
                    p.UpdateBucket(a);
            }));

            generalMessageListener = Pipes.RegisterListener<GeneralMessage>(GeneralMessage.PIPE_NAME, new Listener<GeneralMessage>(a => PrintMessage(a.Message)));

            requestListener = Pipes.RegisterListener<IterativeLookupRequest>(IterativeLookupRequest.PIPE_NAME, new Listener<IterativeLookupRequest>(a => lookupStartQueue.Enqueue(a)));
            stepListener = Pipes.RegisterListener<IterativeLookupStep>(IterativeLookupStep.PIPE_NAME, new Listener<IterativeLookupStep>(a => lookupStepQueue.Enqueue(a)));
            requestCompleteListener = Pipes.RegisterListener<IterativeLookupComplete>(IterativeLookupComplete.PIPE_NAME, new Listener<IterativeLookupComplete>(a => lookupEndQueue.Enqueue(a)));
            clearListener = Pipes.RegisterListener<ClearQueries>(ClearQueries.PIPE_NAME, new Listener<ClearQueries>(a =>
                {
                    ClearQuerySet();
                }));
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            font = Content.Load<SpriteFont>("font");
            whitePixel = Content.Load<Texture2D>("WhitePixel");
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            ConstructPeers();
            ConstructLookups();

            if (peers.Count != positionsLastCalculated)
            {
                positionsLastCalculated = peers.Count;
                foreach (var peer in peers.Select((a, index) => new { a.Value, index, a.Key }))
                {
                    double angle = peer.index / (float)peers.Count * MathHelper.TwoPi;
                    Vector2 pos = new Vector2((float)Math.Cos(angle) * GraphicsDevice.Viewport.Width / 2f, (float)Math.Sin(angle) * GraphicsDevice.Viewport.Height / 2f);
                    pos += new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);

                    peer.Value.Position = pos;
                }
            }

            KeyboardState k = Keyboard.GetState();
            if (k.IsKeyDown(Keys.C))
            {
                ClearQuerySet();
            }

            base.Update(gameTime);
        }

        private void ClearQuerySet()
        {
            Interlocked.Exchange(ref lookupStartQueue, new ConcurrentQueue<IterativeLookupRequest>());
            Interlocked.Exchange(ref lookupStepQueue, new ConcurrentQueue<IterativeLookupStep>());
            Interlocked.Exchange(ref lookupEndQueue, new ConcurrentQueue<IterativeLookupComplete>());
            Interlocked.Exchange(ref lookups, new Dictionary<Guid, Lookup>());
        }

        private void ConstructLookups()
        {
            IterativeLookupRequest request;
            while (lookupStartQueue.TryDequeue(out request))
            {
                lookups[request.lookupId] = new Lookup(request);
                PrintMessage("Start " + request.lookupId);
            }

            Lookup lookup;
            IterativeLookupStep step;
            while (lookupStepQueue.TryDequeue(out step))
                if (lookups.TryGetValue(step.LookupId, out lookup))
                {
                    PrintMessage("step " + step.LookupId);
                    lookup.Step(step);
                }
                else
                {
                    PrintMessage("Not found: " + step.LookupId);
                }

            IterativeLookupComplete complete;
            while (lookupEndQueue.TryDequeue(out complete))
            {
                PrintMessage("Ended " + complete.lookupId);
                lookups.Remove(complete.lookupId);
            }
        }

        private void ConstructPeers()
        {
            DRTConstructed construction;
            while (constructionQueue.TryDequeue(out construction))
                peers[construction.localIdentifier] = new Peer(construction.localIdentifier);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            foreach (var peer in peers.Values)
            {
                //peer.DrawLinks(spriteBatch, Content, peers);
                peer.Draw(spriteBatch, whitePixel);
            }

            float y = 0;
            foreach (var msg in messages)
            {
                spriteBatch.DrawString(font, msg, new Vector2(0, y), Color.Black);
                y += font.MeasureString(msg).Y;
            }

            foreach (var lookup in lookups)
                lookup.Value.Draw(peers, spriteBatch, whitePixel);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
