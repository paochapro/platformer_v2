using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace PlatformerV2;
using static Utils;

class Bullet : Entity
{
    public static Group<Bullet> Bullets { get; private set; } = new();
    private static Size2 size = new(4,8);
    public static Texture2D BulletTexture;
    
    private Vector2 direction;
    private const float speed = 600f;
    
    public Bullet(Point2 pos, Vector2 direction)
        : base(new RectangleF(pos, size), BulletTexture)
    {
        this.direction = direction;
    }

    protected override void Update(GameTime gameTime)
    {
        hitbox.Offset(direction * speed * Game.Delta);
        Collision();
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        RectangleF dest = hitbox with { Position = hitbox.Center };
        spriteBatch.Draw(texture, (Rectangle)dest, null, Color.White, direction.ToAngle(), texture.Bounds.Size.ToVector2() / 2, SpriteEffects.None, 0);

        if (MainGame.DebugMode)
        {
            //spriteBatch.DrawRectangle(hitbox, Color.Red);
        }
    }

    private void Collision()
    {
        Room room = Game.CurrentMap.CurrentRoom;

        for (int y = 0; y < room.Size.Height; ++y)
        {
            for (int x = 0; x < room.Size.Width; ++x)
            {
                if (!room.Tiles[y, x]) continue;
                
                Rectangle tileRect = new Rectangle((new Point(x,y) + room.Position) * new Point(Map.TileUnit), new Point(Map.TileUnit));

                if (hitbox.Intersects(tileRect))
                {
                    BulletImpact.Impacts.Add(new BulletImpact(hitbox.Center));
                    Destroy();
                }
            }
        }
    }
}

class BulletImpact : Entity
{
    public static Group<BulletImpact> Impacts { get; private set; } = new();

    public float Radius { get; private set; }
    public Point2 Origin { get; private set; }
    
    private const float MaxRadius = 100f;
    private const float StartRadius = 10f;
    private const float Lifetime = 0.8f;
    private const float ExpandSpeed = 600f;

    public BulletImpact(Point2 pos)
        : base(new RectangleF(pos, Point2.Zero), null)
    {
        Origin = pos;
        Radius = StartRadius;
        Event.Add(Destroy, Lifetime);
    }

    private void Expand()
    {
        Radius += ExpandSpeed * Game.Delta;
        Radius = clamp(Radius, 0, MaxRadius);
    }
    
    protected override void Update(GameTime gameTime)
    {
        Expand();
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        if (MainGame.DebugMode)
        {
            spriteBatch.DrawCircle(hitbox.Center, Radius, 32, Color.Red);
        }
    }
}