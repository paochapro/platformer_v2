using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;
using MonoGame.Extended.Screens;

using Lib;
using PlatformerV2.Base;
using static Lib.Utils;

namespace PlatformerV2.LevelEditor;

class Editor : GameScreen
{
    public new MainGame Game => base.Game as MainGame;
    public Editor(MainGame game) : base(game) {}

    private OrthographicCamera viewmapCamera;
    
    private bool startMoving;
    private Vector2 startingPoint;
    private Vector2 startingCamera;
    private Vector2 endPoint;

    private Texture2D errorTexture;
    private RenderTarget2D viewmapRenderTarget;
    
    public override void Update(GameTime gameTime)
    {
        Controls();
    }

    private void Controls()
    {
        float zoom = Input.Mouse.ScrollWheelValue / 2000f + 1f;
        viewmapCamera.Zoom = clamp(zoom, viewmapCamera.MinimumZoom, viewmapCamera.MaximumZoom);
        
        if (Input.Mouse.MiddleButton == ButtonState.Pressed)
        {
            if (!startMoving)
            {
                startingPoint = Input.Mouse.Position.ToVector2();
                startingCamera = viewmapCamera.Position;
            }

            endPoint = Input.Mouse.Position.ToVector2();
            viewmapCamera.Position = startingCamera - (endPoint - startingPoint);

            startMoving = true;
        }
        else
            startMoving = false;
    }

    private void DrawGrid(SpriteBatch spriteBatch)
    {
        int tileUnit = PlatformerV2.Main.Map.TileUnit;

        Point cameraPos = viewmapCamera.Position.ToPoint();
        Size cameraSize = (Size)viewmapCamera.BoundingRectangle.Size;
        Rectangle cameraRect = new Rectangle(cameraPos, cameraSize);
        
        Point cameraTile = new(cameraRect.X / tileUnit, cameraRect.Y / tileUnit);
        Point cameraTilePos = cameraTile * new Point(tileUnit);

        //Horizontal lines
        for (int y = cameraTilePos.Y; y <= cameraRect.Bottom; y += tileUnit)
        {
            spriteBatch.DrawLine(cameraRect.Left, y, cameraRect.Right, y, gridColor);
        }
        
        //Vertical lines
        for (int x = cameraTilePos.X; x <= cameraRect.Right; x += tileUnit)
        {
            spriteBatch.DrawLine(x, cameraRect.Top, x, cameraRect.Bottom, gridColor);
        }
    }
    
    private void DrawViewmap(SpriteBatch spriteBatch)
    {
        DrawGrid(spriteBatch);
        spriteBatch.Draw(errorTexture, Vector2.Zero, Color.White);
    }
    
    private void DrawMenu(SpriteBatch spriteBatch)
    {
    }

    public override void Draw(GameTime gameTime)
    {
        var spriteBatch = Game.SpriteBatch;
        var graphics = Game.Graphics.GraphicsDevice;
        
        //Viewmap
        graphics.SetRenderTarget(viewmapRenderTarget);
        graphics.Clear(viewmap_BgColor);
        
        spriteBatch.Begin(transformMatrix: viewmapCamera.GetViewMatrix());
        DrawViewmap(spriteBatch);
        spriteBatch.End();
        
        //Menu
        graphics.SetRenderTarget(null);
        graphics.Clear(Color.White);
        
        spriteBatch.Begin();
        {
            DrawMenu(spriteBatch);
            spriteBatch.Draw(viewmapRenderTarget, new Vector2(menuWidth, 0), Color.White);
        }
        spriteBatch.End();
    }

    public override void LoadContent()
    {
        base.LoadContent();
        CreateUI();
        
        viewmapRenderTarget = new RenderTarget2D(Game.GraphicsDevice, viewmap.Width, viewmap.Height);
        errorTexture = Assets.LoadTexture("error");
    }
    
    public override void Initialize()
    {
        Game.ChangeScreenSize(ScreenSize);
        viewmapCamera = new OrthographicCamera(Game.GraphicsDevice);
    }
    
    //Viewmap
    private static readonly Rectangle viewmap = new(menuWidth, 0, 1200,800);
    private static readonly Color viewmap_BgColor = new(60,60,60);
    private static readonly Color gridColor = new(80,80,80);
    
    //General
    private const int menuWidth = 300;
    public static readonly Size ScreenSize = new(menuWidth + viewmap.Width, viewmap.Height);

    private void CreateUI()
    {
        
    }
}