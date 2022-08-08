using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;

namespace PlatformerV2;
using static Utils;

partial class Player : Entity
{
    private static readonly Size2 size = new(32,32);
    public static Texture2D PlayerTexture;

    public Vector2 velocity;

    private Color currentColor;
    private static readonly Color noShotColor = Color.Blue;
    private static readonly Color haveShotColor = Color.White;

    private const float acc             = maxWalkSpeed * 3;
    private const float gravity         = 2000;
    private const float maxVelocityY    = 1000f;
    private const float maxWalkSpeed    = 400f;
    private const float jumpVel         = 600f;
    private const float minVelocity     = 0.2f;
    private const float friction        = 0.75f;
    private const float airdrag        = 0.98f;

    private readonly Vector2 wallJumpVelocity = new(jumpVel * 0.7f, -jumpVel * 0.8f);

    public bool isTouchingGround { get; private set; }
    public bool isTouchingWall { get; private set; }
    public bool isJumping { get; private set; }
    private int touchingWallNormal;

    private int oldDirection;
    private int direction;
    private bool shot = false;

    private Point spawn;

    public Player(Point pos)
        : base(new RectangleF(pos, size), PlayerTexture)
    {
        spawn = pos;
    }

    public void Death() //should be private
    {
        velocity = Vector2.Zero;
        hitbox.Position = spawn;
        Game.Reset();
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(texture, (Rectangle)hitbox, currentColor);
    }

    protected override void Update(GameTime gameTime)
    {
        Controls();
        CollisionAndMovement();
        CheckInteractables();

        currentColor = shot ? noShotColor : haveShotColor;

        if (hitbox.Y > 1000) Death();
    }

    private void Controls()
    {
        direction = -Convert.ToSByte(Input.IsKeyDown(Keys.A)) + Convert.ToSByte(Input.IsKeyDown(Keys.D));
        oldDirection = direction;

        if (Input.KeyPressed(Keys.W))
        {
            if(isTouchingGround)
            {
                velocity.Y = -jumpVel;
                isTouchingGround = false;
                isJumping = true;
            }

            if(isTouchingWall)
            {
                isTouchingWall = false;
                isJumping = true;
                velocity = wallJumpVelocity * new Vector2(touchingWallNormal, 1);
            }
        }

        if (Input.LBPressed() && !shot)
        {
            Vector2 mousePos = Input.Mouse.Position.ToVector2() + Game.camera.Position;
            Vector2 diff = mousePos - (Vector2)hitbox.Center;
            Vector2 bulletDir = diff.NormalizedCopy();
            
            Entity.AddEntity(new Bullet(hitbox.Center, bulletDir));

            shot = true;
        }

        if (Input.RBPressed())
        {
            hitbox.Position = (Input.Mouse.Position + Game.CurrentMap.CurrentRoomRectangle.Location).ToVector2() * Game.camera.Zoom;
        }
    }

    private void Movement()
    {
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
            velocity.X *= friction;
        else
            velocity.X *= airdrag;

        if (Math.Abs(velocity.X) <= minVelocity)
            velocity.X = 0;
    }

    private void CollisionAndMovement()
    {
        isTouchingGround = false;
        isTouchingWall = false;
        touchingWallNormal = 0;

        velocity.Y += gravity * Game.Delta;
        velocity.Y = clamp(velocity.Y, -maxVelocityY, maxVelocityY);

        Map map = Game.CurrentMap;

        //Room transition
        RectangleF newHitbox = hitbox;
        newHitbox.Position += velocity * Game.Delta;
        
        void CheckRooms(RectangleF newHitbox)
        {
            Map map = Game.CurrentMap;
            int index = 0;
            
            foreach (Rectangle roomRectangle in map.RoomRectangles)
            {
                if (roomRectangle.Contains(hitbox.Center))
                {
                    map.LoadRoom(index);
                }
                ++index;
            }
        }
        CheckRooms(newHitbox);

        void collisionY(RectangleF rect)
        {
            if(hitbox.Intersects(rect))
            {
                hitbox.Y = (float)Math.Round(hitbox.Y);

                if (velocity.Y > 0)
                {
                    hitbox.Y = rect.Y - hitbox.Height;
                    isTouchingGround = true;
                    isJumping = false;
                    shot = false;
                }
                else
                {
                    hitbox.Y = rect.Y + hitbox.Height;
                }
                
                velocity.Y = 0;
            }
        }

        void collisionX(RectangleF rect)
        {
            if(hitbox.Intersects(rect))
            {
                hitbox.X = (float)Math.Round(hitbox.X);
                
                hitbox.X = velocity.X > 0   
                    ? rect.X - hitbox.Width
                    : rect.X + hitbox.Width;
                
                velocity.X = 0;

                isTouchingWall = true;
                touchingWallNormal = -Math.Sign(velocity.X);
            }
        }

        //Collision Y
        hitbox.Y += velocity.Y * Game.Delta;

        foreach (Rectangle wall in map.Walls)
            collisionY(wall);
        
        foreach(ISolid solid in map.Solids)
            if(hitbox.Intersects(solid.SolidHitbox))
                collisionY(solid.SolidHitbox);

        //Movement
        Movement();
        
        //Collision X
        hitbox.X += velocity.X * Game.Delta;

        foreach (Rectangle wall in map.Walls)
            collisionX(wall);

        foreach(ISolid solid in map.Solids)
            if(hitbox.Intersects(solid.SolidHitbox))
                collisionX(solid.SolidHitbox);

        foreach (Rectangle wall in map.Walls)
        {
            RectangleF rightHitbox = hitbox with { X = hitbox.X + 1};
            RectangleF leftHitbox = hitbox with { X = hitbox.X - 1};

            if(rightHitbox.Intersects(wall))
            {
                touchingWallNormal = -1;
                isTouchingWall = true;
            }
            if(leftHitbox.Intersects(wall))
            {
                touchingWallNormal = 1;
                isTouchingWall = true;
            }
        }
    }
    
    private void CheckInteractables()
    {
        Interactable.Iterate(i => i.TouchesPlayer(this));
    }
}