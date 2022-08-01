using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace PlatformerV2;
using static Utils;

class Bullet : Entity
{
    public static Group<Bullet> Bullets { get; private set; } = new();
    private static Size2 size = new(4,4);
    
    private const float speed = 600f;
    private const float maxDistance = 400f;
    private float distancePassed;
    private Vector2 direction;
    
    public Bullet(Point2 pos, Vector2 direction)
        : base(new RectangleF(pos, size), null)
    {
        this.direction = direction;
    }

    protected override void Update(GameTime gameTime)
    {
        while (distancePassed <= maxDistance)
        {
            if (Collision())
            {
                BulletImpact.Impacts.Add(new BulletImpact(hitbox.Center));
                break;
            }
            
            Vector2 move = direction * speed * Game.Delta;
            distancePassed += move.Length();
            hitbox.Offset(move);
        }
        
        Destroy();
    }

    private bool Collision()
    {
        Room room = Game.CurrentMap.CurrentRoom;

        for (int y = 0; y < room.Size.Height; ++y)
        {
            for (int x = 0; x < room.Size.Width; ++x)
            {
                if (!room.Tiles[y, x]) continue;
                
                Rectangle tileRect = new Rectangle((new Point(x,y) + room.Position) * new Point(Map.TileUnit), new Point(Map.TileUnit));

                if (hitbox.Intersects(tileRect))
                    return true;
            }
        }

        return false;
    }
}

class BulletImpact : Entity
{
    public static Group<BulletImpact> Impacts { get; private set; } = new();

    public float Radius { get; private set; }
    public Point2 Origin { get; private set; }
    
    private const float MaxRadius = 110f;
    private const float StartRadius = 10f;
    private const float Lifetime = 0.8f;
    private const float ExpandSpeed = 1200f;

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