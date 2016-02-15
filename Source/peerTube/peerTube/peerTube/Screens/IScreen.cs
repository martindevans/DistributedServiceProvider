using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace peerTube.Screens
{
    public interface IScreen
    {
        void Update(GameTime time);

        void Draw(GameTime time);

        void Stop();
    }
}
