using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;

using Lib;
using PlatformerV2.Main;

namespace PlatformerV2.LevelEditor;

class RoomHandler
{
    private Editor editor;
    private GizmoHandler gizmoHandler;
    private List<Room> rooms = new();
    public IEnumerable<Room> Rooms => rooms;
    
    //Colors
    private static readonly Color canPlaceColor = Color.Gray;
    private static readonly Color cannotPlaceColor = Color.DarkRed;
    
    private static readonly Color roomColor = new(40, 40, 40);
    private static readonly Color roomOutlineColor = Color.Black;
    private static readonly Color selectedColor = Color.Gold;
    
    //General
    private bool canChangeRoom;

    //Room construction
    private Rectangle constructionRoomOutline;
    private Rectangle constructionRoom;

    //Indicator
    private (Point pos, Vector2 dir) indicator;
    private readonly (Point pos, Vector2 dir) emptyIndicator = (Point.Zero, Vector2.Zero);
    private readonly Size fullRoomSize = new(52,32);
    private readonly Color indicatorColor = new Color(Color.Yellow, 0.25f);
    
    //Room selection
    private Room? selectedRoom;
    private Room? transformRoom;

    
    //Methods
    public RoomHandler(Editor editor)
    {
        this.editor = editor;
        ModeSwitch();
    }

    public void ModeSwitch()
    {
        canChangeRoom = false;
        constructionRoom = Rectangle.Empty;
        constructionRoomOutline = Rectangle.Empty;
        
        ResetSelection();
    }

    //Room construction
    private Rectangle ConstructRoom(Point startMouseTile, Point endMouseTile)
    {
        Point startPos = startMouseTile;
        Point endPos = endMouseTile;

        if (endPos.X >= startPos.X)
            endPos.X += 1;
        else
            startPos.X += 1;

        if (endPos.Y >= startPos.Y)
            endPos.Y += 1;
        else
            startPos.Y += 1;

        Rectangle constructedRoom = Rectangle.Union(new Rectangle(startPos, Size.Empty), new Rectangle(endPos, Size.Empty));

        canChangeRoom = true;
        foreach (Room room in rooms)
        {
            if (constructedRoom.Intersects(room.Box))
                canChangeRoom = false;
        }
        
        return constructedRoom;
    }
    
    private void PlaceRoom(Room room)
    {
        rooms.Add(room);
    }
    
    public void RoomConstructionControls(Vector2 mousePos)
    {
        if (!editor.MousePressedInside) return;
        
        if (Input.LBDown())
        {
            constructionRoom = ConstructRoom(editor.GetMouseTile(editor.StartMousePos), editor.GetMouseTile(mousePos));
            constructionRoomOutline = ScaleRoom(constructionRoom);

            indicator.pos = constructionRoom.Location;
            indicator.dir = Vector2.One;
            if (mousePos.X < editor.StartMousePos.X) indicator.dir.X = -1;
            if (mousePos.Y < editor.StartMousePos.Y) indicator.dir.Y = -1;
        }

        if (Input.LBReleased())
        {
            if(canChangeRoom) 
                PlaceRoom(new Room(constructionRoom));
            
            constructionRoomOutline = Rectangle.Empty;
            constructionRoom = Rectangle.Empty;
            indicator = (Point.Zero, Vector2.Zero);
        }
    }
    //Room selection
    private void ResetSelection()
    {
        grabingGizmo = false;
        transformRoom = null;
        selectedRoom = null;
        gizmoHandler = null;
        indicator = emptyIndicator;
        
        Mouse.SetCursor(MouseCursor.Arrow);
    }

    public void RoomSelectionControls(Vector2 mousePos)
    {
        Point startMouseTile = editor.GetMouseTile(editor.StartMousePos);
        
        if(editor.MousePressedInside)
        if (Input.LBPressed() && (selectedRoom == null || !selectedRoom.Box.Contains(startMouseTile)))
        {
            ResetSelection();

            foreach (Room room in rooms)
                if (room.Box.Contains(startMouseTile))
                {
                    selectedRoom = room;
                    transformRoom = (Room)room.Clone();
                    gizmoHandler = new GizmoHandler(editor, this);
                }

            return;
        }
           
        if(selectedRoom != null)
            SelectedRoomControls(mousePos);
    }

    //Loading, saving
    public void LoadMap(string? mapfile)
    {
        Console.WriteLine("loading: " + (mapfile ?? "empty map (not a file)"));
        
        rooms.Clear();
        ModeSwitch();

        if (mapfile == null) return;

        Room NextRoom(BinaryReader reader)
        {
            Rectangle roomBox = new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            Map.Tile[,] tiles = new Map.Tile[roomBox.Height, roomBox.Width];
                    
            for(int y = 0; y < roomBox.Height; ++y)
            for(int x = 0; x < roomBox.Width; ++x)
            {
                tiles[y, x] = (Map.Tile)reader.ReadByte();
            }

            return new Room(roomBox, tiles);
        }
        
        using (BinaryReader reader = new BinaryReader(File.OpenRead(mapfile)))
        {
            while (reader.BaseStream.Position != reader.BaseStream.Length)
                rooms.Add(NextRoom(reader));
        }
    }
    
    public void SaveMap(string mapname)
    {
        Console.WriteLine("saving: " + mapname);
        
        using (BinaryWriter writer = new BinaryWriter(File.Open(mapname, FileMode.Create)))
        {
            foreach (Room room in rooms)
            {
                writer.Write(room.Box.X);
                writer.Write(room.Box.Y);
                writer.Write(room.Box.Width);
                writer.Write(room.Box.Height);

                foreach (IEnumerable<Map.Tile> row in room.Tiles)
                foreach (Map.Tile tile in row)
                {
                    writer.Write((byte)tile);
                }
            }
        }
    }
    
    private bool grabingGizmo = false;

    private void SelectedRoomControls(Vector2 mousePos)
    {
        Vector2 endMousePos = mousePos;
        
        if (Input.KeyPressed(Keys.X))
        {
            rooms.Remove(selectedRoom);
            ResetSelection();
            return;
        }
        
        //TODO: fix mouse inside of viewmap
        if(Input.LBUp() || grabingGizmo)
        {
            gizmoHandler.UpdateGizmos(mousePos, selectedRoom.Box, ScaleRoom(transformRoom.Box));
        }
        
        //Main stuff
        if (!editor.MousePressedInside) return;
        
        if(Input.LBPressed())
        {
            grabingGizmo = gizmoHandler.CheckTouchingGizmos(mousePos);
        }

        if(Input.LBDown())
        {
            if(!grabingGizmo)
            {
                Mouse.SetCursor(MouseCursor.SizeAll);
                Point difference = (editor.GetMouseTile(editor.StartMousePos) - editor.GetMouseTile(endMousePos));
                Point finalPos = selectedRoom.Box.Location - difference;

                if(transformRoom.Box.Location != finalPos)
                    transformRoom.Box = transformRoom.Box with { Location = finalPos };
            }
            else
            {
                Rectangle gizmoTransform = gizmoHandler.GizmosControls(mousePos, transformRoom.Box);

                if (gizmoTransform != transformRoom.Box)
                {
                    transformRoom = (Room)selectedRoom.Clone();
                    transformRoom.Box = gizmoTransform;
                }
                
                indicator.pos = transformRoom.Box.Location;
                indicator.dir = gizmoHandler.GrabedGizmoSide;
                if (gizmoTransform.X < selectedRoom.Box.X) indicator.dir.X = -1;
                if (gizmoTransform.Y < selectedRoom.Box.Y) indicator.dir.Y = -1;
            }

            canChangeRoom = true;
            foreach (Room room in rooms)
            {
                if(selectedRoom == room) continue;
                
                if (transformRoom.Box.Intersects(room.Box))
                    canChangeRoom = false;
            }
            
        }
            
        if (Input.LBReleased())
        {
            Mouse.SetCursor(MouseCursor.Arrow);
            indicator = emptyIndicator;
            gizmoHandler.Released();

            if (canChangeRoom)
                selectedRoom.Box = transformRoom.Box;
            else
            {
                canChangeRoom = true;
                transformRoom.Box = selectedRoom.Box;
            }
        }
    }

    //Draw
    public void DrawUnderGrid(SpriteBatch spriteBatch)
    {
        foreach (Room room in rooms)
        {
            Room finalRoom = room;
            Color color = roomColor;
            
            if(room == selectedRoom)
            {
                finalRoom = transformRoom;
                color = canChangeRoom ? canPlaceColor : cannotPlaceColor;
            }
        
            spriteBatch.FillRectangle(ScaleRoom(finalRoom.Box), color);

            //TODO: maybe optimize tile drawing
            int x = 0;
            int y = 0;

            IEnumerable<IEnumerable<Map.Tile>> rows = finalRoom.Tiles;

            foreach(IEnumerable<Map.Tile> row in rows)
            {
                foreach(Map.Tile tile in row)
                {
                    if (tile != Map.Tile.None)
                    {
                        Point pos = (new Point(x, y) + finalRoom.Box.Location) * new Point(Map.TileUnit);
                        spriteBatch.Draw(editor.TileTextures[tile], new Rectangle(pos, new Point(Map.TileUnit)), Color.White);
                    }
                    x++;
                }
                y++;
                x = 0;
            }
        }
    } 
    
    public void DrawOnGrid(SpriteBatch spriteBatch, Vector2 mousePos)
    {
        foreach (Room room in rooms)
        {
            if (room == selectedRoom) continue;
            spriteBatch.DrawRectangle(ScaleRoom(room.Box), roomOutlineColor, 1f / editor.CameraMatrixScale.X);
        }
        
        Color outlineColor = canChangeRoom ? canPlaceColor : cannotPlaceColor;

        if (constructionRoom != Rectangle.Empty)
            spriteBatch.DrawRectangle(constructionRoomOutline, outlineColor, 1.5f / editor.CameraMatrixScale.X);
        
        if (selectedRoom != null)
        {
            Rectangle final = ScaleRoom(transformRoom.Box);
            spriteBatch.DrawRectangle(final, selectedColor, 1.5f / editor.CameraMatrixScale.X);

            if(Input.LBUp() || grabingGizmo)
                gizmoHandler.DrawGizmos(spriteBatch);
        }

        if (indicator != emptyIndicator)
        {
            Point unit = new Point(Map.TileUnit);
            Rectangle fullRoom = new(indicator.pos, fullRoomSize - new Size(1,1));
            Rectangle box = transformRoom?.Box ?? constructionRoom;
            
            if (indicator.dir.X == -1) fullRoom.X = fullRoom.X + box.Width - fullRoom.Width - 1;
            if (indicator.dir.Y == -1) fullRoom.Y = fullRoom.Y + box.Height - fullRoom.Height - 1;
            
            Rectangle scaledFullRoom = ScaleRoom(fullRoom);

            var drawIndicator = (Point pos) =>
            {
                spriteBatch.FillRectangle(new Rectangle(pos, new Point(Map.TileUnit)), indicatorColor);
            };

            drawIndicator(new Point(scaledFullRoom.Left, scaledFullRoom.Top));
            drawIndicator(new Point(scaledFullRoom.Right, scaledFullRoom.Top));
            drawIndicator(new Point(scaledFullRoom.Left, scaledFullRoom.Bottom));
            drawIndicator(new Point(scaledFullRoom.Right, scaledFullRoom.Bottom));
        }
    }
    
    //General
    private Rectangle ScaleRoom(Rectangle room)
    {
        return room with
        {
            Location = room.Location * new Point(Editor.TileUnit),
            Size = room.Size * new Point(Editor.TileUnit),
        };
    }
}


//TODO: needs rework
class Room : ICloneable
{
    public object Clone()
    {
        Room newRoom = new Room(box);
        newRoom.tiles = (Map.Tile[,])tiles.Clone();
        newRoom.readonlyTiles = tiles.ToJaggedArray();
        return newRoom;
    }

    private Rectangle box;
    public Rectangle Box
    {
        get => box; 
        set
        {
            Rectangle oldBox = box;
            
            box = value;

            if(oldBox.Size == box.Size) return;

            int yOffset = oldBox.Height - box.Height;
            int xOffset = oldBox.Width - box.Width;

            if (oldBox.X == box.X) xOffset = 0;
            if (oldBox.Y == box.Y) yOffset = 0;
            
            //New tiles
            Map.Tile[,] newTiles = new Map.Tile[box.Height, box.Width];
            
            for (int y = 0; y < newTiles.GetLength(0); ++y)
            for (int x = 0; x < newTiles.GetLength(1); ++x)
            {
                bool outside = (
                    y + yOffset < 0 ||
                    x + xOffset < 0 ||
                    y + yOffset >= tiles.GetLength(0) ||
                    x + xOffset >= tiles.GetLength(1)
                );
                if (outside) continue;

                newTiles[y, x] = tiles[y + yOffset, x + xOffset];
            }

            tiles = newTiles;
            readonlyTiles = tiles.ToJaggedArray<Map.Tile>();
        }
    }

    private Map.Tile[,] tiles;
    private IEnumerable<IEnumerable<Map.Tile>> readonlyTiles;
    public IEnumerable<IEnumerable<Map.Tile>> Tiles => readonlyTiles;
    
    public void SetTile(int x, int y, Map.Tile tile) 
    {
        tiles[y, x] = tile;
        readonlyTiles = tiles.ToJaggedArray<Map.Tile>();
    }
    
    public Room(Rectangle box, Map.Tile[,] tiles)
    {
        this.box = box;
        this.tiles = tiles;
        this.readonlyTiles = tiles.ToJaggedArray();
    } 
    
    public Room(Rectangle box)
    {
        this.box = box;
        this.tiles = new Map.Tile[box.Height, box.Width];
        this.readonlyTiles = tiles.ToJaggedArray();
    } 

    public Room() : this(Rectangle.Empty) {}
}