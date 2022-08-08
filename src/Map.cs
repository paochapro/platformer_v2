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
    //public Room CurrentRoom => rooms[CurrentRoomIndex];
    //public IEnumerable<Room> Rooms => rooms;
    
    //private Room CurrentRoom => rooms[CurrentRoomIndex];
    //private IEnumerable<Room> Rooms => rooms;
    
    private Room[] rooms;
    private Rectangle[] roomRectangles;
    private IEnumerable<Entity>? currentEntities;
    
    public IEnumerable<Entity> CurrentEntities => currentEntities;
    public IEnumerable<Rectangle> RoomRectangles => roomRectangles;
    public Rectangle CurrentRoomRectangle => roomRectangles[CurrentRoomIndex];

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

    private List<ISolid> solids;
    public IEnumerable<ISolid> Solids => solids;
    
    //Walls
    public record Wall(Point position);
    private List<Wall> walls;
    private IEnumerable<Rectangle> rectangleWalls;
    public IEnumerable<Rectangle> Walls => rectangleWalls;
    
    public void Reload()
    {
        //Clear up
        solids = new();
        walls = new();
        currentEntities = null;
        CurrentRoomIndex = 0;
        Entity.RemoveAll();
        
        LoadMap(mapName);
        game.CreatePlayer();
    }
    
    //Tiles
    public enum Tile {
        //General
        None = 0,
        Wall = 1,   
        Spawn = 2,
        //Entities
        Spring = 3,
        Bonus = 4,
        Spike = 5,
        //Max
        Max,
    }

    private readonly Dictionary<Tile, Type> TileEntityType = new() {
        [Tile.Spring] = typeof(Spring),
        [Tile.Bonus] = typeof(Bonus),
        [Tile.Spike] = typeof(Spike),
    };

    public Map(string filename, MainGame game)
    {
        this.game = game;
        mapName = filename;
        Reload();
    }

    public void LoadRoom(int index)
    {
        if (CurrentRoomIndex == index) return;
        
        CurrentRoomIndex = index;
        
        //Destroying previous loaded room entities
        if (currentEntities != null)
            foreach (Entity ent in currentEntities)
                ent.Destroy();

        //Room stuff
        Room room = rooms[index];
        game.camera.Position = room.rectangle.Location.ToVector2() * TileUnit;
        
        //Loading entities of this room
        var spawnEntity = ((Type type, Point2 position) spawn) => Activator.CreateInstance(spawn.type, spawn.position) as Entity;
        currentEntities = room.Spawners.Select(spawnEntity).ToList();
        
        foreach (Entity ent in currentEntities)
            Entity.AddEntity(ent);
    }
    
    private void LoadMap(string filename)
    {
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

                    if(tile == Tile.Spawn)
                    {
                        game.spawn = pos;
                    }
                    else if (tile == Tile.Wall)
                    {
                        walls.Add(new Wall(pos));
                    }
                    else
                    {
                        if (TileEntityType.ContainsKey(tile))
                        {
                            spawners.Add((TileEntityType[tile], pos));
                        }
                        else if(tile != Tile.None)
                        {
                            Console.WriteLine($"Unhandled tile type - {tile}, index - {(byte)tile} in Map/LoadMap");
                        }
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
            
            rooms = readRooms.ToArray();
            rectangleWalls = walls.Select(wall => new Rectangle(wall.position, new Point(TileUnit)));
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
                
                //skipping new line (13,10)
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
        
        foreach (Rectangle wall in Walls)
        {
            Color wallColor = Color.Black;
            spriteBatch.FillRectangle(wall, wallColor);
        }
    }
}