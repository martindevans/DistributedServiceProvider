using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LoggerMessages;
using DistributedServiceProvider.Base;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace NetworkVisualiser
{
    public class Lookup
    {
        public readonly Identifier512 Start;
        public readonly Identifier512 Target;

        private List<Identifier512> contacted;
        private List<Identifier512> heap;

        public Lookup(IterativeLookupRequest request)
        {
            Start = request.LocalIdentifier;
            Target = request.target;
        }

        public void Draw(SortedDictionary<Identifier512, Peer> peers, SpriteBatch batch, Texture2D whitePixel)
        {
            var start = GetPosition(Start, peers);

            batch.DrawLine(whitePixel, start, GetPosition(Target, peers), 2, Color.Black);

            if (contacted != null)
                foreach (var c in contacted)
                    batch.DrawLine(whitePixel, start, GetPosition(c, peers), 2, Color.Yellow);

            if (heap != null)
                foreach (var h in heap)
                    batch.DrawLine(whitePixel, start, GetPosition(h, peers), 2, Color.Green);
        }

        private Vector2 GetPosition(Identifier512 id, SortedDictionary<Identifier512, Peer> peers)
        {
            Peer end;
            if (!peers.TryGetValue(id, out end))
                end = peers.Where(a => a.Key >= id).Select(a => a.Value).FirstOrDefault() ?? peers.Last().Value;
            return end.Position;
        }

        public void Step(IterativeLookupStep step)
        {
            contacted = step.Contacted;
            heap = step.Heap;
        }
    }
}
