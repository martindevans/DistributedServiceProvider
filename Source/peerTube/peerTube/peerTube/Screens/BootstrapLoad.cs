using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Threading;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework.Graphics;

namespace peerTube.Screens
{
    public class BootstrapLoad
        :IScreen
    {
        public IScreen Next;
        public IScreen Error;

        volatile bool started = false;
        volatile bool complete = false;
        Thread loadingThread;

        Game1 game;

        ConcurrentStack<String> strings = new ConcurrentStack<string>();

        public BootstrapLoad(Game1 game, IScreen nextScreen, IScreen errorScreen, Action<Action, Action<String>> load)
        {
            Next = nextScreen;
            this.game = game;

            loadingThread = new Thread(a => load(() => complete = true, s => strings.Push(s)));
        }

        public void Update(GameTime time)
        {
            if (!started)
            {
                loadingThread.Start();
                started = true;
            }
            else if (complete)
                game.Screen = Next;
        }

        public void Draw(GameTime time)
        {
            game.SpriteBatch.Begin();

            game.SpriteBatch.Draw(game.Content.Load<Texture2D>("verse_logo"), game.GraphicsDevice.Viewport.Bounds, Color.RoyalBlue);

            SpriteFont font = game.Content.Load<SpriteFont>("Font");

            float middle = game.GraphicsDevice.Viewport.Width / 2f;
            float y = game.GraphicsDevice.Viewport.Height / 2f;
            foreach (var s in strings)
            {
                Vector2 size = font.MeasureString(s);
                game.SpriteBatch.DrawStringJitter(font, s, new Vector2(middle, y) - size / 2f, Color.White, Color.Black);
                y -= size.Y;
            }

            game.SpriteBatch.End();
        }

        public void Stop()
        {
            if (!complete)
                loadingThread.Abort();
        }
    }
}
