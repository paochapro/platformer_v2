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

    public const int TileUnit = PlatformerV2.Main.Map.TileUnit;

    private RoomHandler roomHandler;
    private TileHandler tileHandler;
    
    private Texture2D errorTexture;
    
    private bool startMoving;
    private Vector2 startingPoint;
    private Vector2 startingCamera;
    private Vector2 endPoint;

    private int scrollValue;
    
    //Viewmap
    private RenderTarget2D viewmapRenderTarget;
    private Vector2 viewmapMousePos;
    public Vector2 StartMousePos { get; private set; }
    
    //Viewmap Camera
    public OrthographicCamera Camera { get; private set;}
    public Vector2 CameraMatrixPos { get; private set; }
    public Vector2 CameraMatrixScale { get; private set; }

    //Building
    enum Mode { RoomSelection, RoomConstruction, Tiles }
    private Mode mode;

    public override void Update(GameTime gameTime)
    {
        Controls();
        Camera.GetViewMatrix().Decompose(out Vector2 cameraMatrixPos, out float rotation, out Vector2 cameraMatrixScale);

        CameraMatrixPos = cameraMatrixPos;
        CameraMatrixScale = cameraMatrixScale;
        
        viewmapMousePos = Camera.ScreenToWorld((Input.Mouse.Position - new Point(menuWidth,0)).ToVector2());
        scrollValue += Input.Mouse.ScrollWheelValue - Input.PreviousMouse.ScrollWheelValue;
    }
    
    //Controls
    private void Controls()
    {
        NavigationControls();

        if (Input.LBPressed())
        {
            StartMousePos = viewmapMousePos;
        }

        if (mode == Mode.RoomSelection)
            roomHandler.RoomSelectionControls(viewmapMousePos);
        
        if(mode == Mode.RoomConstruction)
            roomHandler.RoomConstructionControls(viewmapMousePos);
        
        if(mode == Mode.Tiles)
            tileHandler.Controls(viewmapMousePos);
        
        if (Input.LBReleased())
        {
            StartMousePos = Vector2.Zero;
        }

        if (Input.KeyPressed(Keys.Q))
        {
            mode = Mode.RoomSelection;
            roomHandler.ModeSwitch();
        }
        
        if (Input.KeyPressed(Keys.W))
        {
            mode = Mode.RoomConstruction;
            roomHandler.ModeSwitch();
        }
        if (Input.KeyPressed(Keys.E))
        {
            mode = Mode.Tiles;
            roomHandler.ModeSwitch();
        }
    }

    private void NavigationControls()
    {
        float zoom = scrollValue / 2000f + 1f;
        Camera.Zoom = clamp(zoom, Camera.MinimumZoom, Camera.MaximumZoom);
        
        if (Input.Mouse.MiddleButton == ButtonState.Pressed)
        {
            if (!startMoving)
            {
                startingPoint = Input.Mouse.Position.ToVector2();
                startingCamera = Camera.Position;
            }
            endPoint = Input.Mouse.Position.ToVector2();
            Camera.Position = startingCamera - (endPoint - startingPoint) / CameraMatrixScale;

            startMoving = true;
        }
        else
            startMoving = false;
    }


    private void DrawGrid(SpriteBatch spriteBatch)
    {
        //Camera rectangle
        Point cameraPos = (-CameraMatrixPos / CameraMatrixScale).ToPoint();
        Size cameraSize = (Size)Camera.BoundingRectangle.Size;
        Rectangle cameraRect = new Rectangle(cameraPos, cameraSize);

        Point cameraTile = new(cameraRect.X / TileUnit, cameraRect.Y / TileUnit);
        Point cameraTilePos = cameraTile * new Point(TileUnit);

        //Horizontal lines
        for (int y = cameraTilePos.Y; y <= cameraRect.Bottom; y += TileUnit)
        {
            spriteBatch.DrawLine(cameraRect.Left, y, cameraRect.Right, y, gridColor, 1f / CameraMatrixScale.X);
        }
        
        //Vertical lines
        for (int x = cameraTilePos.X; x <= cameraRect.Right; x += TileUnit)
        {
            spriteBatch.DrawLine(x, cameraRect.Top, x, cameraRect.Bottom, gridColor, 1f / CameraMatrixScale.Y);
        }
        
        //Center lines
        spriteBatch.DrawLine(cameraRect.Left, 0, cameraRect.Right, 0, gridCenterHorizontalColor, 1f / CameraMatrixScale.X);
        spriteBatch.DrawLine(0, cameraRect.Top, 0, cameraRect.Bottom, gridCenterVerticalColor, 1f / CameraMatrixScale.X);
    }
    
    private void DrawViewmap(SpriteBatch spriteBatch)
    {
        roomHandler.DrawUnderGrid(spriteBatch);
        DrawGrid(spriteBatch);
        roomHandler.DrawOnGrid(spriteBatch);
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
        
        spriteBatch.Begin(transformMatrix: Camera.GetViewMatrix());
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
        Camera = new OrthographicCamera(Game.GraphicsDevice);
        roomHandler = new RoomHandler(this);
        tileHandler = new TileHandler(this, roomHandler);
    }
    
    public Point GetMouseTile(Vector2 mousePos)
    {
        return new( (int)Math.Floor(mousePos.X / (float)TileUnit), (int)Math.Floor(mousePos.Y / (float)TileUnit) );
    }
    
    //Viewmap
    private static readonly Rectangle viewmap = new(menuWidth, 0, 1200,800);
    public static readonly Color viewmap_BgColor = new(60,60,60);
    public static readonly Color gridColor = new(40,40,40, 100);
    public static readonly Color gridCenterVerticalColor = new(132, 206, 13);
    public static readonly Color gridCenterHorizontalColor = new(208,56,78);
    
    //General
    private const int menuWidth = 300;
    public static readonly Size ScreenSize = new(menuWidth + viewmap.Width, viewmap.Height);

    private void CreateUI()
    {
        
    }
}