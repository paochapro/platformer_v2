using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;
using MonoGame.Extended.Screens;

namespace PlatformerV2;
using static Utils;

class Platformer : GameScreen
{
    public Platformer(MainGame game) : base(game) {}
    public new MainGame Game => base.Game as MainGame;
    
    //Main
    public Map CurrentMap { get; private set; }
    public Player Player { get; set; }

    private bool drawGrid;
    private bool drawRooms;
    
    private bool startMoving;
    private Vector2 startingPoint;
    private Vector2 startingCamera;
    private Vector2 endPoint;
    
    public Point spawn;
    private bool PlayerInvincible { get; set; } = false;

    public override void Update(GameTime gameTime)
    {
        Entity.UpdateAll(gameTime);
        Controls();
    }

    public override void Draw(GameTime gameTime)
    {
        var spriteBatch = Game.SpriteBatch;
        var screen = Game.Screen;

        
        void drawMainElements()
        {
            CurrentMap.Draw(spriteBatch);
            Entity.DrawAll(spriteBatch);
        }
        
        if (MainGame.DebugMode)
        {
            drawMainElements();
        
            if(drawGrid)
            {
                for(int y = 0; y < screen.Y; y += 32)
                {
                    spriteBatch.DrawLine(0, y, screen.X, y, new Color(Color.Black,100));
                }
                for(int x = 0; x < screen.X; x += 32)
                {
                    spriteBatch.DrawLine(x, 0, x, screen.Y, new Color(Color.Black,100));
                }
            }
            
            spriteBatch.DrawString(UI.Font, "Ents count: " + Entity.Count.ToString(), Game.camera.Position, Color.Red);
            spriteBatch.DrawString(UI.Font, "Touching ground: " + Player.isTouchingGround, Game.camera.Position + new Vector2(0, 30), Color.Red);
            
            foreach (Vector2 dir in BulletImpact.launchDirections)
            {
                Vector2 startPoint = new Vector2(60, 60);
                spriteBatch.DrawLine(startPoint , dir * 20 + startPoint, Color.Red);
            }
        
            return;
        }
        
        drawMainElements();
    }
    
    private void Controls()
    {
        var camera = Game.camera;

        if (MainGame.DebugMode)
        {
            if (Input.KeyPressed(Keys.D1))
            {
                PlayerInvincible = !PlayerInvincible;
                Console.WriteLine("Players is " + (PlayerInvincible ? "" : "not ") + "invincible");  
            }
            
            if (Input.KeyPressed(Keys.D2)) Player.Death();
            
            
            if (Input.KeyPressed(Keys.D3)) drawGrid = !drawGrid;
            if (Input.KeyPressed(Keys.D4)) drawRooms = !drawRooms;

            float zoom = Input.Mouse.ScrollWheelValue / 2000f + 1f;
            camera.Zoom = clamp(zoom, camera.MinimumZoom, camera.MaximumZoom);

            if (Input.Mouse.MiddleButton == ButtonState.Pressed)
            {
                if (!startMoving)
                {
                    startingPoint = Input.Mouse.Position.ToVector2();
                    startingCamera = camera.Position;
                }

                endPoint = Input.Mouse.Position.ToVector2();
                camera.Position = startingCamera - (endPoint - startingPoint);

                startMoving = true;
            }
            else
                startMoving = false;
        }
    }
    
    public void Reset()
    {
        CurrentMap.Reload();
    }
    
    public void CreatePlayer()
    {
        Player player = new Player(spawn);
        Entity.AddEntity(player);
        Player = player;
    }

    public override void LoadContent()
    {
        base.LoadContent();
        
        CreateUi();
        
        Player.PlayerTexture = Assets.LoadTexture("Player");
        
        Map.ConvertToBinary("map3.txt", "bin_map3.bin");
        CurrentMap = new Map("bin_map3", this);
    }

    public override void Initialize()
    {
        Entity.RemoveAll();
        Event.ClearEvents();
        UI.Clear();
        Entity.Platformer = this;
    }

    //UI
    private void CreateUi()
    {
    }
}
