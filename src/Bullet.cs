using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace PlatformerV2;
using static Utils;

class Bullet : Entity
{
    private static List<Bullet> bullets = new();

    private static Size2 size = new(4,4);
    
    private const float speed = 600f;
    private const float maxDistance = 400f;
    private float distancePassed;
    private Vector2 direction;
    
    public Bullet(Point2 pos, Vector2 direction)
        : base(new RectangleF(pos, size), null)
    {
        this.direction = direction;

        PreDestroy += () => { bullets.Remove(this); };
        bullets.Add(this);
    }

    protected override void Update(GameTime gameTime)
    {
        while (distancePassed <= maxDistance)
        {
            if (Collision())
            {
                Entity.AddEntity(new BulletImpact(hitbox.Center));
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
        foreach (Rectangle wall in Game.CurrentMap.Walls)
            if (hitbox.Intersects(wall))
                return true;

        return false;
    }
}

class BulletImpact : Interactable
{
    private const float Lifetime = 0.1f;
    public const float Radius = 110f;
    private const float MaxPlayerDistance = Radius;
    private const float MinPlayerDistance = 20f;

    private const float MaxImpactSpeed = 1200f;
    private const float MinImpactSpeed = 1000f - Radius;

    public Point2 Origin { get; private set; }
    
    public BulletImpact(Point2 pos)
        : base(new RectangleF(pos, Point2.Zero), null)
    {
        Origin = pos;
        Event.Add(Destroy, Lifetime);
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        if (MainGame.DebugMode)
        {
            spriteBatch.DrawCircle(hitbox.Center, Radius, 32, Color.Red);
        }
    }

    protected override void Update(GameTime gameTime)
    {
    }

    public override void TouchesPlayer(Player player)
    {
        float dist = Vector2.Distance(player.Hitbox.Center, Origin);

        if(dist < Radius) OnPlayerTouch(player);
    }

    protected override void OnPlayerTouch(Player player)
    {
        Vector2 diff = player.Hitbox.Center - Origin;
        Vector2 launchDirection = diff.NormalizedCopy();

        float distance = diff.Length();

        float value = inverseLerp(MinPlayerDistance, MaxPlayerDistance, distance);

        float launchSpeed = lerp(MaxImpactSpeed, MinImpactSpeed, value);

        // Console.WriteLine("value:" + value);
        // Console.WriteLine("launchSpeed:" + launchSpeed);
        // Console.WriteLine("distance: " + distance);

        player.ImpactBehavior(launchDirection, launchSpeed);

        Destroy();
    }
}

partial class Player
{
    public void ImpactBehavior(Vector2 direction, float speed)
    {
        velocity.Y = 0;
        velocity += direction * speed;
    }
}