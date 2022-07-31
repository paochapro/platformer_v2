using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace PlatformerV2;
using static Utils;

class Map
{
    public const int TileUnit = 32;
    public const string MapExtension = ".bin";
    public const string MapDirectory = "Content/maps/";
    public int CurrentRoomIndex { get; set; }
    public Room CurrentRoom => Rooms[CurrentRoomIndex];
    public Room[] Rooms { get; private set; }
    
    public static void ConvertToBinary(string src, string dest)
    {
        void WriteRoom(BinaryWriter writer, StreamReader reader)
        {
             string roomX = reader.ReadUntil(' ', true);
             string roomY = reader.ReadUntil(' ', true);
             
             string roomW = reader.ReadUntil(' ', true);
             string roomH = reader.ReadUntil('\n', true);

             Console.WriteLine("Reading room:");
             Console.WriteLine($"RX: {roomX}, RY: {roomY}");
             Console.WriteLine($"RW: {roomW}, RH: {roomH}");
     
             Point roomPos = new Point(int.Parse(roomX), int.Parse(roomY));
             Point roomSize = new Point(int.Parse(roomW), int.Parse(roomH));
             
             writer.Write(roomPos.X);
             writer.Write(roomPos.Y);
             writer.Write(roomSize.X);
             writer.Write(roomSize.Y);

             Console.WriteLine("Room:");
             for (int y = 0; y < roomSize.Y; ++y)
             {
                 for (int x = 0; x < roomSize.X; ++x)
                 {
                     int ch = reader.Read();

                     Console.Write(ch);
                     
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
                 Console.WriteLine();
             }
        }

        StreamReader reader = new StreamReader(MapDirectory + src);
        BinaryWriter writer = new BinaryWriter(File.Open(MapDirectory + dest, FileMode.OpenOrCreate));
        
        using (reader)
        using (writer)
        {
            while (reader.Peek() != -1)
            {
                WriteRoom(writer, reader);
            }
        }
    }
    
    public Map(string filename)
    {
        LoadMap(filename);
    }
    
    private void LoadMap(string filename)
    {
        Room NextRoom(BinaryReader reader)
        {
            Point roomPos = new(reader.ReadInt32(), reader.ReadInt32());
            Point roomSize = new(reader.ReadInt32(), reader.ReadInt32());
            bool[,] tiles = new bool[roomSize.Y, roomSize.X];

            for (int y = 0; y < roomSize.Y; ++y)
            {
                for (int x = 0; x < roomSize.X; ++x)
                {
                    tiles[y, x] = Convert.ToBoolean(reader.ReadByte());
                }
            }
            
            return new Room(roomPos, tiles);
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
            Rooms = readRooms.ToArray();
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        /*foreach (Room room in Rooms)
        {
            room.Draw(spriteBatch);
        }
        */
        
        Rooms[CurrentRoomIndex].Draw(spriteBatch);
    }
}

class Room
{
    public Point Position { get; private init; }
    public Size Size { get; private init; }
    public bool[,] Tiles { get; private init; }

    public Room(Point position, bool[,] tiles)
    {
        Tiles = tiles;
        Position = position;
        Size = new Size(tiles.GetLength(1), tiles.GetLength(0));
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        if (MainGame.DebugMode)
        {
            spriteBatch.FillRectangle(new Rectangle( Position * new Point(Map.TileUnit),  Size * new Point(Map.TileUnit)), new Color(Color.Aqua, 100));
        }
        
        for (int y = 0; y <  Size.Height; ++y)
        {
            for (int x = 0; x <  Size.Width; ++x)
            {
                Color color = Tiles[y, x] ? Color.Black : Color.Transparent;
                Point pos = (new Point(x,y) +  Position) * new Point(Map.TileUnit);
                spriteBatch.FillRectangle(new Rectangle(pos, new Point(Map.TileUnit)), color);
            }
        }
    }
}