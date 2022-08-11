using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace PlatformerV2;

class Spring : Interactable, ISolid
{
    private const int height = 48;
    private const int heightOnStretch = 32;
    private const int heightStretch = height - heightOnStretch;
    private const int width = Map.TileUnit; //32
    private const float LaunchForce = 1100f;

    private static readonly Size2 activateSize = new(width-2, 2);
    private static readonly Size2 pressedActivateSize = new(width, 2);
    
    private static readonly Size2 launchSize = new(width,1);
    private static readonly Size2 solidSize = new(width,48);

    public static Texture2D springTexture;
    private bool ready;
    //private bool playerTouching;
    private bool playerTouchedLaunch;

    private readonly float pressedActivateX;
    private readonly float activateX;

    private readonly RectangleF launchHitbox;
    private readonly Point2 position;
    public RectangleF SolidHitbox { get; private set; }

    public Spring(Point2 pos)
        : base(RectangleF.Empty, springTexture)
    {
        position = pos;
        pressedActivateX = pos.X;
        activateX = pos.X + 1;
        
        float floor = (pos.Y + Map.TileUnit);
        float activateY = floor - height - activateSize.Height;
        float collisionY = floor - solidSize.Height;
        float launchY = floor - heightOnStretch - launchSize.Height;

        hitbox = new(pos with { Y = activateY, X=pos.X+1}, activateSize);   //Activation hitbox
        SolidHitbox = new(pos with {Y = collisionY}, solidSize);            //Collision hitbox
        launchHitbox = new(pos with {Y = launchY}, launchSize);             //Jump/Launch hitbox
    }

    protected override void Update(GameTime gameTime)
    {
        //if(!playerTouching) OnNotTouching();
        //playerTouching = false;
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        if(MainGame.DebugMode)
        {
            spriteBatch.DrawRectangle(SolidHitbox, Color.Green);
            spriteBatch.DrawRectangle(launchHitbox, Color.Yellow);
            spriteBatch.DrawRectangle(hitbox, Color.Red);
        }
    }

    protected override void OnNotTouching(Player player)
    {
        SolidHitbox = SolidHitbox with { Y = position.Y - heightStretch };
        SolidHitbox = SolidHitbox with { Height = height };

        hitbox.X = activateX;
        hitbox.Size = activateSize;

        ready = false;
    }

    protected override void OnPlayerTouch(Player player)
    {
        hitbox.X = pressedActivateX;
        hitbox.Size = pressedActivateSize;

        //playerTouching = true;

        SolidHitbox = SolidHitbox with { Y = position.Y };
        SolidHitbox = SolidHitbox with { Height = heightOnStretch };

        if(playerTouchedLaunch && player.isJumping && ready)
        {
            player.SpringBehavior(Spring.LaunchForce);
        }

        if(!player.isJumping) ready = true;
        playerTouchedLaunch = launchHitbox.Intersects(player.Hitbox);
    }
}

partial class Player
{
    public void SpringBehavior(float force) => velocity.Y += -force;
}