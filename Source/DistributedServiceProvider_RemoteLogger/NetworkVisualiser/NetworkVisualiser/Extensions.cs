using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace NetworkVisualiser
{
    public static class Extensions
    {
        public static void DrawLine(this SpriteBatch batch, Texture2D tex, Vector2 start, Vector2 end, int width, Color c)
        {
            float length = (end - start).Length();
            Vector2 mid = end * 0.5f + start * 0.5f;

            float rotation = (float)Math.Acos(Vector2.Dot((end - start) / length, new Vector2(1, 0)));

            if (end.Y < start.Y)
                rotation = MathHelper.TwoPi - rotation;

            batch.Draw(tex, new Rectangle((int)start.X, (int)(start.Y - width / 2f), (int)length, width), null, c, rotation, Vector2.Zero, SpriteEffects.None, 0.5f);
        }
    }
}
