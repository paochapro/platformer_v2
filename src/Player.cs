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
    private static readonly Color nobulletColor = Color.Blue;
    private static readonly Color havebulletColor = Color.White;

    //Movement
    private const float acc             = maxWalkSpeed * 3;
    private const float gravity         = 2000;
    private const float wallSlide       = 270;
    private const float maxVelocityY    = 1000f;
    private const float maxWalkSpeed    = 400f;
    private const float jumpVel         = 600f;
    private const float minVelocity     = 0.2f;
    private const float friction        = 0.75f;
    private const float airdrag         = 0.98f;
    
    //Other
    private readonly Vector2 wallJumpVelocity = new(jumpVel * 0.7f, -jumpVel * 0.8f);
    private const float preJumpLimit = 0.15f;
    private float preJumpTimer;
    private RectangleF oldHitbox;

    public bool isTouchingGround { get; private set; }
    public bool isTouchingWall { get; private set; }
    public bool isJumping { get; private set; }
    private int touchingWallNormal;

    private int oldDirection;
    private int direction;
    private bool bullet;
    private bool shooting;
    
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
        
        if (MainGame.DebugMode)
        {
            Vector2 endVec = (new Vector2(direction, 0) * (hitbox.Width / 2)) + hitbox.Center;
            spriteBatch.DrawLine(hitbox.Center, endVec, Color.Red);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        oldHitbox = hitbox;
        
        Controls();
        CollisionAndMovement();
        CheckInteractables();
        CheckRooms();
        CameraScroll();
        
        currentColor = bullet ? havebulletColor : nobulletColor;

        //if (hitbox.Y > 1000) Death();
    }
    
    private bool stillPressingJump = false;
    private void Controls()
    {
        direction = -Convert.ToSByte(Input.IsKeyDown(Keys.A)) + Convert.ToSByte(Input.IsKeyDown(Keys.D));
        oldDirection = direction;
        shooting = false;
        
        if (Input.IsKeyDown(Keys.W))
        {
            void jumping()
            {
                if (isTouchingGround || isTouchingWall)
                {
                    stillPressingJump = true;
                    isJumping = true;

                    if (isTouchingGround)
                    {
                        velocity.Y = -jumpVel;
                        isTouchingGround = false;
                    }
                    if (isTouchingWall)
                    {
                        velocity = wallJumpVelocity * new Vector2(touchingWallNormal, 1);
                        isTouchingWall = false;
                    }
                }
            }

            if (preJumpTimer < preJumpLimit && !stillPressingJump)
            {
                jumping();
            }

            preJumpTimer += Game.Delta;
        }
        else
        {
            preJumpTimer = 0;
            stillPressingJump = false;
        }

        if (Input.LBPressed() && bullet)
        {
            Vector2 mousePos = Input.Mouse.Position.ToVector2() + Game.camera.Position;
            Vector2 diff = mousePos - (Vector2)hitbox.Center;
            Vector2 bulletDir = diff.NormalizedCopy();
            
            Entity.AddEntity(new Bullet(hitbox.Center, bulletDir));
            
            bullet = false;
            shooting = true;
        }

        if (MainGame.DebugMode && Input.RBPressed())
        {
            hitbox.Position = Input.Mouse.Position.ToVector2() + Game.camera.Position;
        }
    }

    //Collision and movement
    private void CollisionAndMovement()
    {
        Map map = Game.CurrentMap;
        var solids = map.Solids;
        var semiSolids = map.SemiSolids;

        //Y
        MovementY();
        
        isTouchingGround = false;
        hitbox.Y += velocity.Y * Game.Delta;

        foreach (RectangleF solid in solids) CollisionY(solid);
        foreach (RectangleF semiSolid in semiSolids) SemiSolidCollision(semiSolid);
        
        //X
        MovementX();
        
        isTouchingWall = false;
        touchingWallNormal = 0;
        hitbox.X += velocity.X * Game.Delta;

        foreach (RectangleF solid in solids) CollisionX(solid);
    }

    private void MovementY()
    {
        if (isTouchingWall && direction == -touchingWallNormal && velocity.Y > wallSlide) 
            velocity.Y = wallSlide;
        else
            velocity.Y += gravity * Game.Delta;
        
        velocity.Y = clamp(velocity.Y, -maxVelocityY, maxVelocityY);
    }
    
    private void MovementX()
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
        
        //TODO: little rework
        if (isTouchingGround && (maxWalkSpeedExceed() || !moving))
            velocity.X *= friction;
        else if(!isTouchingGround)
            velocity.X *= airdrag;
        
        if (Math.Abs(velocity.X) <= minVelocity)
            velocity.X = 0;
    }
    
    private bool CollisionY(RectangleF rect)
    {
        if(hitbox.Intersects(rect))
        {
            hitbox.Y = (float)Math.Round(hitbox.Y);

            if (velocity.Y > 0)
            {
                hitbox.Y = rect.Y - hitbox.Height;
                isTouchingGround = true;
                isJumping = false;

                if(!shooting) 
                    bullet = true;
            }
            else
            {
                hitbox.Y = rect.Y + hitbox.Height;
            }
            
            velocity.Y = 0;

            return true;
        }

        return false;
    }
    
    private bool CollisionX(RectangleF rect)
    {
        bool collided = false;
        
        //Collisions
        if(hitbox.Intersects(rect))
        {
            hitbox.X = (float)Math.Round(hitbox.X);
            
            hitbox.X = velocity.X > 0   
                ? rect.X - hitbox.Width
                : rect.X + hitbox.Width;
            
            velocity.X = 0;

            isTouchingWall = true;
            touchingWallNormal = -Math.Sign(velocity.X);

            collided = true;
        }
        
        //Wall jumping
        RectangleF rightHitbox = hitbox with { X = hitbox.X + 1};
        RectangleF leftHitbox = hitbox with { X = hitbox.X - 1};
        bool leftCollision = false;
        bool rightCollision = false;

        if(rightHitbox.Intersects(rect))
        {
            rightCollision = true;
            touchingWallNormal = -1;
            isTouchingWall = true;
        }
        if(leftHitbox.Intersects(rect))
        {
            leftCollision = true;
            touchingWallNormal = 1;
            isTouchingWall = true;
        }
        if (leftCollision && rightCollision)
        {
            touchingWallNormal = -direction;
        }

        return collided;
    }
    
    private bool SemiSolidCollision(RectangleF rect)
    {
        if(hitbox.Intersects(rect) && oldHitbox.Bottom <= rect.Y)
        {
            hitbox.Y = rect.Y - hitbox.Height;
            isTouchingGround = true;
            isJumping = false;
            velocity.Y = 0;
            if(!shooting) bullet = true;

            return true;
        }

        return false;
    }

    //Other
    private void CheckRooms()
    {
        Map map = Game.CurrentMap;
        int index = 0;
            
        foreach (Rectangle roomRectangle in map.RoomRectangles)
        {
            if (roomRectangle.Contains(hitbox.Center))
                map.LoadRoom(index);
            
            ++index;
        }
    }

    private void CheckInteractables()
    {
        Interactable.Iterate(i => i.TouchesPlayer(this));
    }

    private void CameraScroll()
    {
        //X
        /*int cameraHalfWidth = (int)Math.Round(Game.camera.BoundingRectangle.Width) / 2;
        int centerX = ((Rectangle)hitbox).Center.X;
        
        Rectangle roomRect = Game.CurrentMap.CurrentRoomRectangle;
        int leftBorder = roomRect.Left + cameraHalfWidth;
        int rightBorder = roomRect.Right - cameraHalfWidth;
        
        int cameraCenter = clamp(centerX, leftBorder, rightBorder);
        
        Game.camera.Position = Game.camera.Position with { X = cameraCenter - cameraHalfWidth };*/

        Size2 cameraSize = Game.camera.BoundingRectangle.Size;
        Size cameraHalfSize = new((int)Math.Round(cameraSize.Width) / 2, (int)Math.Round(cameraSize.Height) / 2);
        
        Point center = ((Rectangle)hitbox).Center;
        
        Rectangle roomRect = Game.CurrentMap.CurrentRoomRectangle;
        int leftBorder = roomRect.Left + cameraHalfSize.Width;
        int rightBorder = roomRect.Right - cameraHalfSize.Width;
        int topBorder = roomRect.Top + cameraHalfSize.Height;
        int bottomBorder = roomRect.Bottom - cameraHalfSize.Height;

        Point cameraCenter = Point.Zero;
        cameraCenter.X = clamp(center.X, leftBorder, rightBorder);
        cameraCenter.Y = clamp(center.Y, topBorder, bottomBorder);

        Game.camera.Position = (cameraCenter - (Point)cameraHalfSize).ToVector2();
    }
}