using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace PlatformerV2;

interface ISolid
{
    public RectangleF SolidHitbox { get; }
}