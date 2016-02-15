using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace peerTube
{
    public static class Extensions
    {
        public static string ToDataString(this long bytes)
        {
            return ((double)bytes).ToDataString();
        }

        public static string ToDataString(this double bytes)
        {
            int sign = Math.Sign(bytes);
            bytes *= sign;

            string prepend = sign < 0 ? "-" : "";

            if (bytes < 1024)
                return prepend + (int)bytes + "b";

            bytes /= 1024f;

            if (bytes < 1024)
                return prepend + bytes.ToString("0.##") + "Kb";

            bytes /= 1024f;

            if (bytes < 1024)
                return prepend + bytes.ToString("0.##") + "Mb";

            bytes /= 1024f;

            return prepend + bytes.ToString("0.##") + "Gb";
        }

        public static Vector2 AsVector2(this Point p)
        {
            return new Vector2(p.X, p.Y);
        }

        public static void DrawStringJitter(this SpriteBatch batch, SpriteFont font, string s, Vector2 position, Color c, Color shadow)
        {
            Vector2 jitterX = new Vector2(1, 0);
            Vector2 jitterY = new Vector2(0, 1);

            batch.DrawString(font, s, position + jitterX, shadow);
            batch.DrawString(font, s, position - jitterX, shadow);
            batch.DrawString(font, s, position + jitterY, shadow);
            batch.DrawString(font, s, position - jitterY, shadow);
            batch.DrawString(font, s, position + jitterX + jitterY, shadow);
            batch.DrawString(font, s, position + jitterX - jitterY, shadow);
            batch.DrawString(font, s, position - jitterX + jitterY, shadow);
            batch.DrawString(font, s, position - jitterX - jitterY, shadow);
            batch.DrawString(font, s, position, c);
        }
    }
}
