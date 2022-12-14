using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

using Lib;
using static Lib.Utils;
using PlatformerV2.Base;

namespace PlatformerV2.Main;

class Bullet : Entity
{
    private static Size2 size = new(4,4);
    
    private const float speed = 600f;
    private const float maxDistance = 400f;
    private float distancePassed;
    private Vector2 direction;
    
    public Bullet(Point2 pos, Vector2 direction)
        : base(new RectangleF(pos, size), null)
    {
        this.direction = direction;
        
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
    }

    protected override void Update(GameTime gameTime) => Destroy();
    
    private bool Collision()
    {
        foreach (RectangleF solid in Platformer.CurrentMap.Solids)
            if (hitbox.Intersects(solid))
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

    private const float MaxImpactSpeed = 1000f; //1200
    private const float MinImpactSpeed = 800f - Radius; //1000
    
    //should be private
    public static readonly Vector2[] launchDirections =
    {
        //90
        -Vector2.UnitY, //Up
        Vector2.UnitY, //Down
        -Vector2.UnitX, //Left
        Vector2.UnitX, //Right
        //45
        new(-1, -1), //Up-Left
        new(1, -1), //Up-Right
        new(-1, 1), //Down-Left
        new(1, 1), //Down-Right
        //22,5 - Top, Bottom
        new(-0.5f, -1), //Up-Left
        new(0.5f, -1), //Up-Right
        new(-0.5f, 1), //Down-Left
        new(0.5f, 1), //Down-Right
        //22,5 - Left, Right
        new(-1, -0.5f), //Up-Left
        new(-1, 0.5f), //Up-Right
        new(1, -0.5f), //Down-Left
        new(1, 0.5f), //Down-Right
    };

    static BulletImpact()
    {
        launchDirections = launchDirections.Select(dir => dir.NormalizedCopy()).ToArray();
    }

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

    protected override void Update(GameTime gameTime) { }

    public override void TouchesPlayer(Player player)
    {
        float dist = Vector2.Distance(player.Hitbox.Center, Origin);

        if(dist < Radius) OnPlayerTouch(player);
    }

    protected override void OnPlayerTouch(Player player)
    {
        Vector2 launchDirection = Vector2.Zero;
        Vector2 diff = (player.Hitbox.Center - Origin).NormalizedCopy();
        
        var dotProducts = launchDirections.Select(dir => Vector2.Dot(dir, diff));
        var pairs = launchDirections.Zip(dotProducts);
        
        float max = dotProducts.Max();
        foreach (var pair in pairs)
            if (pair.Second == max)
                launchDirection = pair.First;

        Console.WriteLine("launchDirection: " + launchDirection);

        //launch speed
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