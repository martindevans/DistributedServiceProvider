using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Base;
using LoggerMessages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Collections.Concurrent;

namespace NetworkVisualiser
{
    public class Peer
    {
        public readonly Identifier512 Identifier;

        Identifier512[][] buckets = new Identifier512[512][];

        public Peer(Identifier512 id)
        {
            Identifier = id;
        }

        internal void UpdateBucket(BucketState a)
        {
            buckets[a.Index] = a.Identifiers;
        }

        public Vector2 Position;

        public void Draw(SpriteBatch batch, Texture2D whitePixel)
        {
            batch.Draw(whitePixel, new Rectangle((int)Position.X - 1, (int)Position.Y - 1, 2, 2), Color.Black);
        }

        public void DrawLinks(SpriteBatch batch, ContentManager content, IDictionary<Identifier512, Peer> peers)
        {
            for (int i = 0; i < buckets.Length; i++)
            {
                if (buckets[i] != null)
                {
                    for (int j = 0; j < buckets[i].Length; j++)
                    {
                        Peer otherPeer;
                        if (peers.TryGetValue(buckets[i][j], out otherPeer) && otherPeer.Position != Vector2.Zero)
                        {
                            batch.DrawLine(content.Load<Texture2D>("WhitePixel"), Position, otherPeer.Position, 1, Color.White);
                        }
                    }
                }
            }
        }
    }
}
