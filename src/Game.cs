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
    private readonly Point DefaultScreenSize = new(1400, 1000);
    
    public Point Screen => screen;
    private Point screen;
    
    //General stuff
    public GraphicsDeviceManager Graphics => graphics;
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    private OrthographicCamera camera;
    
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
    
    public void Death()
    {
    }
    
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
        
    }

    private Player player;
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

        Map.ConvertToBinary("map2.txt", "bin_map2.bin");
        CurrentMap = new Map("bin_map2");

        Player.PlayerTexture = Assets.LoadTexture("player");

        player = new Player(new Point(0, 900));
        Entity.AddEntity(player);

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
            
            if (Input.KeyPressed(Keys.D2))
            {
                player.Reset();
            }

            float zoom = Input.Mouse.ScrollWheelValue / 2000f + 1f;
            camera.Zoom = clamp(zoom, camera.MinimumZoom, camera.MaximumZoom);
        }
    }
    
    //Draw
    private void DrawGame()
    {
        if (DebugMode)
        {
            foreach (Room room in CurrentMap.Rooms)
            {
                spriteBatch.FillRectangle(new Rectangle(room.Position * new Point(Map.TileUnit), room.Size * new Point(Map.TileUnit)), Color.Fuchsia);
            }
        }
        
        CurrentMap.Draw(spriteBatch);
        Entity.DrawAll(spriteBatch);
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