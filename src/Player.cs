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

    private Vector2 velocity;

    private const float gravity         = 2400;
    private const float maxVelocityY    = 1000f;
    private const float maxWalkSpeed    = 400f;
    private const float acc             = maxWalkSpeed * 3;
    private const float impactSpeed     = 800f;
    private const float jumpVel         = 600f;
    private const float minVelocity     = 0.2f;
    private const float friction        = 0.75f;
    
    private bool isTouchingGround = false;
    private int oldDirection;
    private int direction;
    bool shot = false;

    private Point spawn;

    public Player(Point pos)
        : base(new RectangleF(pos, size), PlayerTexture)
    {
        spawn = pos;
    }

    public void Reset()
    {
        velocity = Vector2.Zero;
        hitbox.Position = spawn;
    }
    
    protected override void Update(GameTime gameTime)
    {
        Controls();
        CollisionAndMovement();
        CheckImpacts();
        
        if (hitbox.Y > 1000) Reset();
    }

    private void Controls()
    {
        direction = -Convert.ToSByte(Input.IsKeyDown(Keys.A)) + Convert.ToSByte(Input.IsKeyDown(Keys.D));

        oldDirection = direction;

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

            shot = true;
        }
    }

    private void CollisionAndMovement()
    {
        isTouchingGround = false;

        velocity.Y += gravity * Game.Delta;
        velocity.Y = clamp(velocity.Y, -maxVelocityY, maxVelocityY);

        /*Room transition
        RectangleF newHitbox = hitbox;
        newHitbox.X += walkVelocity * Game.Delta;
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
        */

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


        bool maxWalkSpeedExceed() => Math.Abs(velocity.X) > maxWalkSpeed;
        bool moving = (direction != 0);

        if (moving)
        {
            if(!maxWalkSpeedExceed() || oldDirection != direction)
                velocity.X += direction * acc * Game.Delta;

            if (maxWalkSpeedExceed() && isTouchingGround)
                velocity.X = maxWalkSpeed * Math.Sign(velocity.X);
        }
        
        if (isTouchingGround && (maxWalkSpeedExceed() || !moving))
        {
            //Debug info
            /*Console.WriteLine("friction gonna be applied:");
            Console.WriteLine("{");
            Console.WriteLine("\tmaxWalkSpeedExceed:" + maxWalkSpeedExceed());
            Console.WriteLine("\tmoving:" + moving);
            Console.WriteLine("\tisTouchingGround:" + isTouchingGround);
            Console.WriteLine("\tdirection:" + direction);
            Console.WriteLine("}");*/
            
            velocity.X *= friction;
        }

        if (Math.Abs(velocity.X) <= minVelocity)
            velocity.X = 0;
        
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
                
                impact.Destroy();
                
                shot = true;
            }
        });
    }
}