using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace PlatformerV2;

class MovingBlock : Entity, IMovingSolid
{
    public RectangleF SolidHitbox => hitbox;
    public Vector2 DeltaPosition => velocity;
    private RectangleF oldHitbox;
    private Vector2 velocity;

    public MovingBlock(Point2 pos, Size size)
        : base(new RectangleF(pos, new Size2(size.Width, size.Height)), null)
    {
        velocity = Vector2.UnitX * 3;
    }
    
    protected override void Update(GameTime gameTime)
    {
        oldHitbox = hitbox;
        hitbox.Offset(velocity);
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.FillRectangle(hitbox, Color.Black);
    }
}