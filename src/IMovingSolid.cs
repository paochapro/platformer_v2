using Microsoft.Xna.Framework;

namespace PlatformerV2;

interface IMovingSolid : ISolid
{
    public Vector2 DeltaPosition { get; }
}