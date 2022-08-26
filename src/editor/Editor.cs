using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;
using MonoGame.Extended.Screens;

using Myra;
using Myra.Graphics2D.UI;

using Lib;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI.Styles;
using PlatformerV2.Base;
using PlatformerV2.Main;
using static Lib.Utils;

namespace PlatformerV2.LevelEditor;

class Editor : GameScreen
{
    public new MainGame Game => base.Game as MainGame;

    public Editor(MainGame game) : base(game)
    {
        TileTextures = new();

        IEnumerable<Map.Tile> tiles = Enum.GetValues<Map.Tile>().Skip(1).SkipLast(1);

        foreach (Map.Tile tile in tiles)
        {
            if (tile == Map.Tile.Spawn)
            {
                TileTextures.Add(tile, Assets.LoadTexture("player"));
                continue;
            }
            TileTextures.Add(tile, Assets.LoadTexture("texture_" + Enum.GetName(tile).ToLower()));
        }
    }

    public const int TileUnit = PlatformerV2.Main.Map.TileUnit;

    private RoomHandler roomHandler;
    private TileHandler tileHandler;
    
    private Texture2D errorTexture;
    private int scrollValue;
    
    private bool startMoving;
    private Vector2 startingPoint;
    private Vector2 startingCamera;
    private Vector2 endPoint;
    
    //Textures
    public Dictionary<Map.Tile, Texture2D> TileTextures { get; init; }

    //Viewmap
    private RenderTarget2D viewmapRenderTarget;
    private Vector2 viewmapMousePos;
    public Vector2 StartMousePos { get; private set; }
    
    //Viewmap Camera
    public OrthographicCamera Camera { get; private set;}
    public Vector2 CameraMatrixPos { get; private set; }
    public Vector2 CameraMatrixScale { get; private set; }

    //Building
    private bool _inRooms;
    private bool InRooms
    {
        get => _inRooms;
        set
        {
            _inRooms = value;
            inSelection = false;
            
            roomsRadio.IsPressed = value;
            tilesRadio.IsPressed = !value;

            
            roomHandler.ModeSwitch();
        }
    }
    private bool inSelection;
    
    //Menu
    private Desktop menu;
    private Point SelectedUITilePosition;
    
    private bool _mousePressedInside;
    public bool MousePressedInside => _mousePressedInside;
    public bool MouseInside => Input.Mouse.X > menuWidth;

    public override void Update(GameTime gameTime)
    {
        Controls();
        
        Camera.GetViewMatrix().Decompose(out Vector2 cameraMatrixPos, out float rotation, out Vector2 cameraMatrixScale);

        CameraMatrixPos = cameraMatrixPos;
        CameraMatrixScale = cameraMatrixScale;
        
        viewmapMousePos = Camera.ScreenToWorld((Input.Mouse.Position - new Point(menuWidth,0)).ToVector2());
    }
    
    //Controls
    private void Controls()
    {
        //Switch modes
        if (Input.KeyPressed(Keys.E)) InRooms = false;
        if (Input.KeyPressed(Keys.R)) InRooms = true;
        if (Input.KeyPressed(Keys.S))
        {
            inSelection = true;
            roomHandler.ModeSwitch();
        }
        
        //Navigation
        if(MouseInside) 
            NavigationControls();

        //Different controls
        if (Input.LBPressed())
        {
            StartMousePos = viewmapMousePos;
            _mousePressedInside = MouseInside;
        }
        
        if (InRooms)
        {
            if(inSelection)
                roomHandler.RoomSelectionControls(viewmapMousePos);
            else            
                roomHandler.RoomConstructionControls(viewmapMousePos);
        }
        else
        {
            if (inSelection)
                ;
            else
                tileHandler.Controls(viewmapMousePos);
        }
        
        if (Input.LBReleased())
        {
            StartMousePos = Vector2.Zero;
            _mousePressedInside = false;
        }
    }

    private void NavigationControls()
    {
        scrollValue += Input.Mouse.ScrollWheelValue - Input.PreviousMouse.ScrollWheelValue;

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
            //TODO: menu (whole ui) renders behind viewmap for some reason
            spriteBatch.Draw(viewmapRenderTarget, new Vector2(menuWidth, 0), Color.White);
            menu.Render();
            spriteBatch.DrawRectangle(new Rectangle(SelectedUITilePosition, new Point(32)), Color.Gold, 1);
        }
        spriteBatch.End();
    }

    public override void LoadContent()
    {
        base.LoadContent();
        CreateUI();
        
        viewmapRenderTarget = new RenderTarget2D(Game.GraphicsDevice, viewmap.Width, viewmap.Height);
        errorTexture = Assets.LoadTexture("error");

        InRooms = true;
        Event.Add((tilesPanel.Widgets.First() as ImageButton).DoClick, 0.2f);
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

    private VerticalStackPanel menuPanel;
    private Grid tilesPanel;
    private RadioButton roomsRadio;
    private RadioButton tilesRadio;

    private const string nonCompileMapDirectory = @"..\..\..\Content\maps";

    private void Save(string mapname)
    {
        mapname = nonCompileMapDirectory + mapname;
        mapname = EndingAbcense(mapname, Map.MapExtension);
        roomHandler.SaveMap(mapname);
    }
    
    private void Load(string? mapname) => roomHandler.LoadMap(mapname);
    
    private void CreateUI()
    {
        const int horizontalMargin = 15;
        const int verticalMargin = 10;
        const int tilesHorizontalSlots = 7;
        const int unit = 32;
        const int sectionOffset = 48;
        const int buttonWidth = 150;
        const int buttonHeight = 50;

        //Menu
        menuPanel = new VerticalStackPanel() {
            Width = menuWidth,
            Height = ScreenSize.Height,
            Margin = new Myra.Graphics2D.Thickness(horizontalMargin, verticalMargin),
            //Background = new SolidBrush(Color.Aqua),
        };
        
        //Mode buttons
        roomsRadio = new RadioButton() {
            Text = "Rooms",
            TextColor = Color.Black,
            //Image = new TextureRegion(Assets.LoadTexture("error")),
        };
        tilesRadio = new RadioButton() {
            Text = "Tiles",
            TextColor = Color.Black,
            //Image = new TextureRegion(Assets.LoadTexture("error")),
        };
        roomsRadio.Click += (s, a) => InRooms = true;
        tilesRadio.Click += (s, a) => InRooms = false;

        //Tiles Panel
        tilesPanel = new() {
            Width = tilesHorizontalSlots * unit,
            Height = unit,
            Background = new SolidBrush(Color.Gray),
            Border = new SolidBrush(Color.Black),
            BorderThickness = new Myra.Graphics2D.Thickness(1),
            Margin = new Myra.Graphics2D.Thickness(-1),
            GridLinesColor = new Color(60,60,60),
            ShowGridLines = true
        };

        int gridColumn = 0;
        int gridRow = 0;

        var tilesEnum = Enum.GetValues<Map.Tile>().Skip(1).SkipLast(1); //Skipping None and Max
        
        foreach (Map.Tile tile in tilesEnum)
        {
            Texture2D tileTexture = TileTextures[tile];

            var tileButton = new ImageButton() {
                Image = new TextureRegion(tileTexture),
                Width = unit,
                Height = unit,
                GridColumn = gridColumn,
                GridRow = gridRow,
            };
            
            gridColumn++;

            if (gridColumn * unit >= tilesPanel.Width)
            {
                gridColumn = 0;
                gridRow++;
                tilesPanel.Height += unit;
            }
            
            tilesPanel.Widgets.Add(tileButton);
            
            tileButton.Click += (s,a) =>
            {
                tileHandler.CurrentTile = tile;
                SelectedUITilePosition = tileButton.ContainerBounds.Location + tilesPanel.ContainerBounds.Location + menuPanel.ActualBounds.Location;
            };
        }
        
        //Save, load, new buttons
        var saveButton = new TextButton() {
            Text = "Save",
            TextColor = Color.White,
            Width = buttonWidth,
            Height = buttonHeight,
        };
        var loadButton = new TextButton() {
            Text = "Load",
            TextColor = Color.White,
            Width = buttonWidth,
            Height = buttonHeight,
        };
        var newButton = new TextButton() {
            Text = "New",
            TextColor = Color.White,
            Width = buttonWidth,
            Height = buttonHeight
        };
        
        //TODO: add unsaved changes warning
        saveButton.Click += (s, a) => {
            Myra.Graphics2D.UI.TextBox tbMapName = new()
            {
                TextColor = Color.White,
                HintText = "Enter map name",
                MaxWidth = 250,
                MinWidth = 250,
                Wrap = true
            };

            Dialog saveWindow = Dialog.CreateMessageBox("Save", tbMapName);
            saveWindow.ButtonOk.Click += (s, a) => Save(tbMapName.Text);
            saveWindow.ShowModal(menu, Point.Zero);
        };
        loadButton.Click += (s, a) => {
            ListBox maplist = new();
            
            var mapfiles = Directory.GetFiles(nonCompileMapDirectory, "*" + Map.MapExtension);
            
            //Load custom all maps
            foreach (string mapfile in mapfiles)
            {
                string mapname = mapfile.Substring(mapfile.LastIndexOf("\\") + 1);
                maplist.Items.Add(new ListItem(mapname) { Color = Color.White });
            }
            
            Dialog loadWindow = Dialog.CreateMessageBox("Choose map", maplist);

            loadWindow.ButtonOk.Click += (s, a) =>
            {
                if (maplist.SelectedIndex.HasValue)
                {
                    Load(mapfiles[maplist.SelectedIndex.Value]);
                }
            };
            
            loadWindow.ShowModal(menu, Point.Zero);
        };
        newButton.Click += (s, a) => {
            Load(null);
        };

        //Adding elements
        var addOffset = (int offset) => menuPanel.Widgets.Add(new Grid() { Height = offset });

        const int forthOffset = sectionOffset / 4;
        
        //Mode
        menuPanel.Widgets.Add(roomsRadio);
        addOffset(forthOffset);
        menuPanel.Widgets.Add(tilesRadio);
        addOffset(sectionOffset);
        
        //Tile panel
        menuPanel.Widgets.Add(new Myra.Graphics2D.UI.Label() { Text = "Tiles:", TextColor = Color.Black } );
        menuPanel.Widgets.Add(tilesPanel);
        addOffset(sectionOffset);
        
        //Save, load buttons
        menuPanel.Widgets.Add(saveButton);
        addOffset(forthOffset);
        menuPanel.Widgets.Add(loadButton);
        addOffset(forthOffset);
        menuPanel.Widgets.Add(newButton);
        

        menu = new Desktop();
        menu.Root = menuPanel;
    }
}