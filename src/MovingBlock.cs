using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace PlatformerV2;

class MovingBlock : Entity, ISolid
{
    private static List<MovingBlock> list = new();
    public static IEnumerable<MovingBlock> All => list;

    public RectangleF SolidHitbox => hitbox;
    public Vector2 DeltaPos => velocity;
    private Vector2 velocity;

    public MovingBlock(Point2 pos, Size size)
        : base(new RectangleF(pos, new Size2(size.Width, size.Height)), null)
    {
        velocity = Vector2.UnitX;
        
        list.Add(this);
        PreDestroy += (() => list.Remove(this));
    }
    
    protected override void Update(GameTime gameTime)
    {
        hitbox.Offset(velocity);
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.FillRectangle(hitbox, Color.Black);
    }
}