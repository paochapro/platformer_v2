using Microsoft.Xna.Framework;

namespace PlatformerV2.Main;

interface IMovingSolid : ISolid
{
    public Vector2 DeltaPosition { get; }
}