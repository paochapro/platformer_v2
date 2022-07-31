using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;

namespace PlatformerV2;
using static Utils;

class Player : Entity
{
    private static readonly Size2 size = new(32,32);
    public static Texture2D PlayerTexture;

    private Point2 velocity;
    private const float gravity = 1200f;
    private const float acc = 800f;
    private const float maxVelocityX = 2000f;
    private const float maxVelocityY = 2000f;
    private const float friction = 0.9f;
    private const float minFriction = 0.5f;
    private const float jumpVel = 500f;
    private const float impactSpeed = 800f;
    
    private bool isTouchingGround = false;

    private Point spawn;
    bool shot = false;

    public Player(Point pos)
        : base(new RectangleF(pos, size), PlayerTexture)
    {
        spawn = pos;
    }

    public void Reset() => hitbox.Position = spawn;

    protected override void Update(GameTime gameTime)
    {
        Controls();
        CollisionAndMovement();
        CheckImpacts();
    }

    private void Controls()
    {
        int direction = -Convert.ToSByte(Input.IsKeyDown(Keys.A)) + Convert.ToSByte(Input.IsKeyDown(Keys.D));

        if (direction != 0)
        {
            velocity.X += direction * acc * Game.Delta;
        }
        else
        {
            velocity.X *= friction;
            if (Math.Abs(velocity.X) < minFriction)
                velocity.X = 0;
        }

        if (Input.KeyPressed(Keys.W) && isTouchingGround)
        {
            velocity.Y = -jumpVel;
            isTouchingGround = false;
        }

        
        if (Input.LBPressed() && !shot)
        {
            Vector2 mousePos = Input.Mouse.Position.ToVector2();
            Vector2 diff = mousePos - (Vector2)hitbox.Center;
            Vector2 bulletDir = diff.NormalizedCopy();
            
            Bullet.Bullets.Add(new Bullet(hitbox.Center, bulletDir));
        }
    }
    
    private void CollisionAndMovement()
    {
        isTouchingGround = false;

        velocity.Y += gravity * Game.Delta;
        velocity.Y = clamp(velocity.Y, -maxVelocityY, maxVelocityY);
        velocity.X = clamp(velocity.X, -maxVelocityX, maxVelocityX);

        RectangleF newHitbox = hitbox;
        newHitbox.X += velocity.X * Game.Delta;
        newHitbox.Y += velocity.Y * Game.Delta;

        void CheckRooms(RectangleF newHitbox)
        {
            for (int i = 0; i < Game.CurrentMap.Rooms.Length; ++i)
            {
                Room room = Game.CurrentMap.Rooms[i];
                
                if (newHitbox.Intersects(new Rectangle(room.Position * new Point(Map.TileUnit), room.Size * new Point(Map.TileUnit))))
                {
                    Game.CurrentMap.CurrentRoomIndex = i;
                }
            }
        }
        CheckRooms(newHitbox);

        //Collision
        Room room = Game.CurrentMap.CurrentRoom;

        hitbox.Y += velocity.Y * Game.Delta;

        for (int y = 0; y < room.Size.Height; ++y)
        for (int x = 0; x < room.Size.Width; ++x)
            if (room.Tiles[y, x])
            {
                Rectangle rect = new Rectangle( (new Point(x, y) + room.Position) * new Point(Map.TileUnit), new Point(Map.TileUnit));
                
                if(hitbox.Intersects(rect))
                {
                    hitbox.Y = (float)Math.Round(hitbox.Y);

                    if (velocity.Y > 0)
                    {
                        hitbox.Y = rect.Y - hitbox.Height;
                        isTouchingGround = true;
                        shot = false;
                    }
                    else
                    {
                        hitbox.Y = rect.Y + hitbox.Height;

                    }
                    
                    velocity.Y = 0;
                }
            }

        hitbox.X += velocity.X * Game.Delta;

        for (int y = 0; y < room.Size.Height; ++y)
        for (int x = 0; x < room.Size.Width; ++x)
            if (room.Tiles[y, x])
            {
                Rectangle rect = new Rectangle( (new Point(x, y) + room.Position) * new Point(Map.TileUnit), new Point(Map.TileUnit));
                
                if(hitbox.Intersects(rect))
                {
                    hitbox.X = (float)Math.Round(hitbox.X);
                    
                    hitbox.X = velocity.X > 0   
                        ? rect.X - hitbox.Width
                        : rect.X + hitbox.Width;
                    
                    velocity.X = 0;
                }
            }
    }

    private void CheckRooms()
    {
        
    }

    private void CheckImpacts()
    {
        BulletImpact.Impacts.Iterate(impact =>
        {
            float dist = Vector2.Distance(hitbox.Center, (Vector2)impact.Origin);
            if (dist < impact.Radius)
            {
                Vector2 diff = hitbox.Center - impact.Origin;
                Vector2 launchDirection = diff.NormalizedCopy();

                velocity = launchDirection * impactSpeed;
                
                shot = true;
            }
        });
    }
}