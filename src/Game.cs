using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;
using MonoGame.Extended.Screens;

using Lib;
using PlatformerV2.LevelEditor;
using PlatformerV2.Main;
using static Lib.Utils;

namespace PlatformerV2.Base;

class MainGame : Game
{
    //More important stuff
    private const string GameName = "PlatformerV2";
    private const float DefaultVolume = 0.3f;
    private const bool Resizable = false;
    
    //most common   - 1920x1056 (60x33 blocks by 32)
    //my computer   - 1680x924  (60x33 blocks by 28)
    private readonly Point DefaultScreenSize = new(1680, 924); //notebook (1400, 800)
    
    public Point Screen => screen;
    private Point screen;
    
    private ScreenManager screenManager;
    
    //General stuff
    public SpriteBatch SpriteBatch => spriteBatch;
    public GraphicsDeviceManager Graphics => graphics;
    private SpriteBatch spriteBatch;
    private GraphicsDeviceManager graphics;
    private OrthographicCamera Camera { get; set; }

    public enum GameState { Menu, Game, Editor }

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
    
    public float Delta { get; private set; }
    public static bool DebugMode { get; private set; } = true;

    //Initialization
    public void ChangeScreenSize(Point size)
    {
        screen = size;
        graphics.PreferredBackBufferWidth = size.X;
        graphics.PreferredBackBufferHeight = size.Y;
        graphics.ApplyChanges();
    }
    
    private void LoadScreen(GameScreen screen)
    {
        Event.ClearEvents();
        UI.Clear();
        Camera = new OrthographicCamera(GraphicsDevice);
        screenManager.LoadScreen(screen);
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

        State = GameState.Game;
        
        LoadScreen(new Editor(this));
    }
    
    protected override void Initialize()
    {
        Window.AllowUserResizing = Resizable;
        Window.Title = GameName;
        screen = DefaultScreenSize;
        SoundEffect.MasterVolume = DefaultVolume;
        IsMouseVisible = true;
        Camera = new OrthographicCamera(GraphicsDevice);

        Entity.Game = this;
        
        ChangeScreenSize(DefaultScreenSize);
        
        base.Initialize();
    }
    
    //Main
    protected override void Update(GameTime gameTime)
    {
        Delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        Controls();
        
        UI.UpdateElements(Input.Keys, Input.Mouse);
        Event.ExecuteEvents(gameTime);
        
        screenManager.Update(gameTime);
        
        Input.CycleEnd();
        base.Update(gameTime);
    }
    
    protected override void Draw(GameTime gameTime)
    {
        screenManager.Draw(gameTime);
        base.Draw(gameTime);
    }

    private void Controls()
    {
        if (Input.IsKeyDown(Keys.Escape)) Exit();

        if (Input.KeyPressed(Keys.OemTilde)) 
            DebugMode = !DebugMode;
        
        if (Input.KeyPressed(Keys.M)) 
            LoadScreen(new Editor(this));
        
        if (Input.KeyPressed(Keys.N)) 
            LoadScreen(new Platformer(this));
    }

    public MainGame()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";

        //Screens
        screenManager = new ScreenManager();
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