using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;

namespace PlatformerV2;
using static Utils;

//////////////////////////////// Starting point
class MainGame : Game
{
    //More important stuff
    private const string GameName = "PlatformerV2";
    private const float DefaultVolume = 0.3f;
    private const bool Resizable = false;
    private readonly Point DefaultScreenSize = new(1400, 1000); //computer (1400, 1000) / notebook (1400, 800)
    
    public Point Screen => screen;
    private Point screen;
    
    //General stuff
    public GraphicsDeviceManager Graphics => graphics;
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    public OrthographicCamera camera { get; set; }
    
    private readonly Dictionary<GameState, Action> drawMethods;
    private readonly Dictionary<GameState, Action<GameTime>> updateMethods;

    public enum GameState { Menu, Game }

    private GameState gameState;
    public GameState State
    {
        get => gameState;
        private set
        {
            gameState = value;
            UI.CurrentLayer = Convert.ToInt32(value);
        }
    }
    
    public static bool DebugMode { get; private set; } = true;
    private bool PlayerInvincible { get; set; } = false;
    public float Delta { get; private set; }
    
    //Game
    public Map CurrentMap { get; private set; }
    public Point spawn;
    
    //Initialization
    private void ChangeScreenSize(Point size)
    {
        screen = size;
        graphics.PreferredBackBufferWidth = size.X;
        graphics.PreferredBackBufferHeight = size.Y;
        graphics.ApplyChanges();
    }

    public void Reset()
    {
        CurrentMap.Reload();
    }

    public Player Player { get; set; }

    public void CreatePlayer()
    {
        Player player = new Player(spawn);
        Entity.AddEntity(player);
        Player = player;
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        Assets.Content = Content;
        UI.Font = Content.Load<SpriteFont>("bahnschrift");
        UI.window = Window;
        
        //UI style
        UI.BgDefaultColor = Color.Black;
        UI.BgSelectedColor = Color.Black;
        UI.MainDefaultColor = Color.White;

        CreateUi();

        Player.PlayerTexture = Assets.LoadTexture("Player");

        Map.ConvertToBinary("map1.txt", "bin_map1.bin");
        CurrentMap = new Map("bin_map1", this);
        
        State = GameState.Game;
    }

    protected override void Initialize()
    {
        Window.AllowUserResizing = Resizable;
        Window.Title = GameName;
        IsMouseVisible = true;
        camera = new OrthographicCamera(GraphicsDevice);
        screen = DefaultScreenSize;

        Entity.Game = this;
        
        ChangeScreenSize(DefaultScreenSize);

        SoundEffect.MasterVolume = DefaultVolume;
        
        base.Initialize();
    }
    
    //Main
    private void UpdateGame(GameTime gameTime)
    {
        Delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Entity.UpdateAll(gameTime);
        Controls();
    }
    
    private void UpdateMenu(GameTime gameTime)
    {
        
    }

    protected override void Update(GameTime gameTime)
    {
        //Exit
        if (Input.IsKeyDown(Keys.Escape)) Exit();

        UI.UpdateElements(Input.Keys, Input.Mouse);
        Event.ExecuteEvents(gameTime);
        updateMethods[gameState].Invoke(gameTime);
        Input.CycleEnd();

        base.Update(gameTime);
    }

    private bool drawGrid;
    private bool drawRooms;
    private void Controls()
    {
        if (Input.KeyPressed(Keys.OemTilde)) 
            DebugMode = !DebugMode;
        
        if (DebugMode)
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

    private bool startMoving;
    private Vector2 startingPoint;
    private Vector2 startingCamera;
    private Vector2 endPoint;
    
    //Draw
    private void DrawGame()
    {
        void drawMainElements()
        {
            CurrentMap.Draw(spriteBatch);
            Entity.DrawAll(spriteBatch);
        }

        if (DebugMode)
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
            
            spriteBatch.DrawString(UI.Font, "Ents count: " + Entity.Count.ToString(), camera.Position, Color.Red);
            spriteBatch.DrawString(UI.Font, "Touching ground: " + Player.isTouchingGround, camera.Position + new Vector2(0, 30), Color.Red);
            
            foreach (Vector2 dir in BulletImpact.launchDirections)
            {
                Vector2 startPoint = new Vector2(60, 60);
                spriteBatch.DrawLine(startPoint , dir * 20 + startPoint, Color.Red);
            }

            return;
        }

        drawMainElements();
    }

    private void DrawMenu()
    {
    }


    protected override void Draw(GameTime gameTime)
    {
        graphics.GraphicsDevice.Clear(Color.White);
        
        spriteBatch.Begin(transformMatrix: camera.GetViewMatrix());
        {
            drawMethods[State].Invoke();
            UI.DrawElements(spriteBatch);
        }
        spriteBatch.End();

        base.Draw(gameTime);
    }
    
    //UI
    private void CreateUi()
    {
    }
    
    public MainGame()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        
        updateMethods = new()
        {
            [GameState.Menu] = UpdateMenu,
            [GameState.Game] = UpdateGame,
        };

        drawMethods = new()
        {
            [GameState.Menu] = DrawMenu,
            [GameState.Game] = DrawGame,
        };
    }
}

class Program
{
    public static void Main()
    {
        using (MainGame game = new MainGame())
            game.Run();
    }
}