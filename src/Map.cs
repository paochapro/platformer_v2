using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace PlatformerV2;
using static Utils;

class Map
{
    //General
    private MainGame game;
    public const int TileUnit = 32;
    public const string MapExtension = ".bin";
    public const string MapDirectory = "Content/maps/";
    private readonly string mapName;

    //Room
    private int CurrentRoomIndex { get; set; }
    private Room[] rooms;
    private Rectangle[] roomRectangles;
    public IEnumerable<Rectangle> RoomRectangles => roomRectangles;
    public Rectangle CurrentRoomRectangle => roomRectangles[CurrentRoomIndex];
    
    public IEnumerable<Entity> CurrentEntities { get; private set; }

    private class Room
    {
        public Rectangle rectangle { get; init; }
        public IEnumerable<(Type type, Point2 position)> Spawners;

        public Room(Point position, Size size, IEnumerable<(Type, Point2)> spawners)
        {
            rectangle = new(position, size);
            Spawners = spawners;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (MainGame.DebugMode)
            {
                Rectangle final = new(rectangle.Location * new Point(TileUnit), rectangle.Size * new Point(TileUnit));
                spriteBatch.DrawRectangle(final, Color.Red, 2f);
            }
        }
    }
    
    //Solids
    private IEnumerable<RectangleF> walls;
    private List<ISolid> currentSolids;
    public IEnumerable<RectangleF> Solids => //walls (static) + currentSolids (dynamic)
        walls.Union(currentSolids
            .Select(s => s.SolidHitbox))
            .ToList();
    
    public IEnumerable<RectangleF> SemiSolids { get; private set; }
    
    private const float semiSolidHeight = 2f;
    private const float semiSolidVisualHeight = 6f;

    public void Reload()
    {
        var emptyRects = Enumerable.Empty<RectangleF>();
        
        //Clean up
        walls = emptyRects;
        SemiSolids = emptyRects;
        currentSolids = new();
        CurrentEntities = Enumerable.Empty<Entity>();
        
        CurrentRoomIndex = -1;
        Entity.RemoveAll();
        
        LoadMap(mapName);
        game.CreatePlayer();
        LoadRoom(0);
    }
    
    //Tiles
    public enum Tile {
        //General
        None = 0,
        Wall = 1,
        Spawn = 2,
        SemiSolid = 7,
        //Entities
        Spring = 3,
        Bonus = 4,
        Spike = 5,
        MovingBlock = 6,
        //Max
        Max,
    }

    private readonly Dictionary<Tile, Type> TileEntityType = new() {
        [Tile.Spring] = typeof(Spring),
        [Tile.Bonus] = typeof(Bonus),
        [Tile.Spike] = typeof(Spike),
        [Tile.MovingBlock] = typeof(MovingBlock),
    };

    public Map(string filename, MainGame game)
    {
        this.game = game;
        mapName = filename;
        Reload();
    }

    public void LoadRoom(int index)
    {
        //Console.WriteLine("Loading room");
        if (CurrentRoomIndex == index) return;
        
        CurrentRoomIndex = index;
        
        
        //Destroying previous loaded room entities
        foreach (Entity ent in CurrentEntities)
            ent.Destroy();
        
        currentSolids.Clear();

        //Room stuff
        Room room = rooms[index];
        game.camera.Position = room.rectangle.Location.ToVector2() * TileUnit;
        
        //Loading entities of this room
        var spawnEntity = ((Type type, Point2 position) spawn) => Activator.CreateInstance(spawn.type, spawn.position) as Entity;
        CurrentEntities = room.Spawners.Select(spawnEntity).ToList();

        foreach (Entity ent in CurrentEntities)
        {
            if (ent is ISolid solid)
                currentSolids.Add(solid);
                
            Entity.AddEntity(ent);
        }
        
        //Console.WriteLine("Loading room finished");
    }
    
    private void LoadMap(string filename)
    {
        List<Point> wallPositions = new();
        List<Point> loadedSemiSolids = new();
        
        Room NextRoom(BinaryReader reader)
        {
            Point roomPos = new(reader.ReadInt32(), reader.ReadInt32());
            Point roomSize = new(reader.ReadInt32(), reader.ReadInt32());

            List<(Type, Point2)> spawners = new();

            //Loading room
            for (int y = 0; y < roomSize.Y; ++y)
            {
                for (int x = 0; x < roomSize.X; ++x)
                {
                    Tile tile = (Tile)reader.ReadByte();
                    Point pos = (new Point(x,y) + roomPos) * new Point(Map.TileUnit);
                    if(tile == Tile.None) continue;

                    if(tile == Tile.Spawn)              game.spawn = pos;
                    else if (tile == Tile.Wall)         wallPositions.Add(pos);
                    else if (tile == Tile.SemiSolid)    loadedSemiSolids.Add(pos);
                    else
                    {
                        if (!LoadEntity())
                            Console.WriteLine($"Unhandled tile type - {tile}, index - {(byte)tile} in Map/LoadMap");
                    }

                    bool LoadEntity()
                    {
                        if (!TileEntityType.ContainsKey(tile)) return false;
                        
                        Type type = TileEntityType[tile];
                        spawners.Add((type, pos));
                        return true;
                    }
                }
            }
            
            return new Room(roomPos, roomSize, spawners);
        }

        filename = MapDirectory + filename;
        filename = EndingAbcense(filename, MapExtension);

        using(BinaryReader reader = new BinaryReader(File.OpenRead(filename)))
        {
            List<Room> readRooms = new();

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                Room room = NextRoom(reader);
                readRooms.Add(room);
            }
            
            walls = wallPositions.Select(pos => new RectangleF(pos, new(TileUnit, TileUnit)));
            SemiSolids = loadedSemiSolids.Select(pos => new RectangleF(pos, new(TileUnit, semiSolidHeight)));
            
            rooms = readRooms.ToArray();
            roomRectangles = rooms.Select(room => new Rectangle(room.rectangle.Location * new Point(TileUnit), room.rectangle.Size * new Point(TileUnit))).ToArray();
        }
    }
    
    public static void ConvertToBinary(string src, string dest)
    {
        void WriteRoom(BinaryWriter writer, StreamReader reader)
        {
            string roomX = reader.ReadUntil(' ', true);
            string roomY = reader.ReadUntil(' ', true);
            
            string roomW = reader.ReadUntil(' ', true);
            string roomH = reader.ReadUntil('\n', true);

            //Debug
            /*Console.WriteLine("Reading room:");
            Console.WriteLine($"RX: {roomX}, RY: {roomY}");
            Console.WriteLine($"RW: {roomW}, RH: {roomH}");*/
     
            Point roomPos = new Point(int.Parse(roomX), int.Parse(roomY));
            Point roomSize = new Point(int.Parse(roomW), int.Parse(roomH));
            
            writer.Write(roomPos.X);
            writer.Write(roomPos.Y);
            writer.Write(roomSize.X);
            writer.Write(roomSize.Y);

            //Console.WriteLine("Room:");
            for (int y = 0; y < roomSize.Y; ++y)
            {
                for (int x = 0; x < roomSize.X; ++x)
                {
                    int ch = reader.Read();

                    //Console.Write(ch-48);
                    
                    bool tileAvaliable = false;
                    for(char numb = '0'; numb < ('9'+1); ++numb)
                    {
                        if (ch != numb) continue;
                        tileAvaliable = true;
                    }
     
                    if(!tileAvaliable)
                    {
                        throw new Exception($"Wrong tile type in ConvertToBinary: {ch}");
                    }
     
                    writer.Write((byte)(ch - 48));
                }
                
                //skipping new line \n (13,10)
                reader.Read();
                reader.Read();
                //Console.WriteLine();
            }
        }

        StreamReader reader = new StreamReader(MapDirectory + src);
        BinaryWriter writer = new BinaryWriter(File.Open(MapDirectory + dest, FileMode.Create));
        
        using (reader)
        using (writer)
        {
            while (reader.Peek() != -1)
            {
                WriteRoom(writer, reader);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        rooms[CurrentRoomIndex].Draw(spriteBatch);

        foreach (RectangleF wall in walls)
            spriteBatch.FillRectangle(wall, Color.Black);

        foreach (RectangleF semiSolid in SemiSolids)
        {
            RectangleF final = semiSolid with { Height = semiSolidVisualHeight };
            spriteBatch.FillRectangle(final, Color.Brown);
        }
    }
}
