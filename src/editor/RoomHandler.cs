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
    private bool canChangeRooms;

    //Room construction
    private Rectangle constructionRoomOutline;
    private Rectangle constructionRoom;

    //Indicator
    private Vector2 indicatorDirection;
    private readonly Size fullRoomSize = new(52,32);
    private readonly Color indicatorColor = new(Color.Yellow, 0.25f);
    
    //Room selection
    private IEnumerable<Room> selectedRooms => rooms.Where(r => r.Selected);
    private IEnumerable<Room> nonSelectedRooms => rooms.Except(selectedRooms);
    private bool singleSelection => selectedRooms.Count() == 1;
    private bool grabingGizmo = false;

    //Methods
    public RoomHandler(Editor editor)
    {
        this.editor = editor;
        ModeSwitch();
    }

    public void ModeSwitch()
    {
        canChangeRooms = false;
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

        canChangeRooms = true;
        foreach (Room room in rooms)
        {
            if (constructedRoom.Intersects(room.Box))
                canChangeRooms = false;
        }
        
        return constructedRoom;
    }
    
    public void RoomConstructionControls(Vector2 mousePos)
    {
        if (!editor.MousePressedInside) return;
        
        if (Input.LBDown())
        {
            constructionRoom = ConstructRoom(editor.GetMouseTile(editor.StartMousePos), editor.GetMouseTile(mousePos));
            constructionRoomOutline = ScaleRoom(constructionRoom);

            indicatorDirection = Vector2.One;
            if (mousePos.X < editor.StartMousePos.X) indicatorDirection.X = -1;
            if (mousePos.Y < editor.StartMousePos.Y) indicatorDirection.Y = -1;
        }

        if (Input.LBReleased())
        {
            if(canChangeRooms) 
                rooms.Add(new Room(constructionRoom));
            
            constructionRoomOutline = Rectangle.Empty;
            constructionRoom = Rectangle.Empty;
            indicatorDirection = Vector2.Zero;
        }
    }
    
    //Room selection
    private void ResetSelection()
    {
        indicatorDirection = Vector2.Zero;
        grabingGizmo = false;
        gizmoHandler = null;
        rooms.ForEach(r => r.Selected = false);

        Mouse.SetCursor(MouseCursor.Arrow);
    }

    public void RoomSelectionControls(Vector2 mousePos)
    {
        Point startMouseTile = editor.GetMouseTile(editor.StartMousePos);
        
        if(editor.MousePressedInside)
        if(Input.LBPressed() && (selectedRooms.Count() == 0 || !selectedRooms.Any(r => r.Box.Contains(startMouseTile))))
        {
            if (!Input.IsKeyDown(Keys.LeftControl))
                ResetSelection();
            
            foreach (Room room in rooms)
                if (room.Box.Contains(startMouseTile))
                {
                    room.Selected = true;

                    if (singleSelection)
                        gizmoHandler = new GizmoHandler(room.Box, editor, this);
                    else
                        gizmoHandler = null;
                }

            return;
        }
           
        if(selectedRooms.Count() != 0)
            SelectedRoomControls(mousePos);
    }

    private void SelectedRoomControls(Vector2 mousePos)
    {
        if (Input.KeyPressed(Keys.X))
        {
            rooms.RemoveAll(r => r.Selected);
            ResetSelection();
            return;
        }
        
        //TODO: fix mouse inside of viewmap
        if((Input.LBUp() || grabingGizmo) && singleSelection)
        {
            gizmoHandler.UpdateGizmos(mousePos, ScaleRoom(selectedRooms.Single().Box));
        }
        
        //Main stuff
        if (!editor.MousePressedInside) 
            return;
        
        if(Input.LBPressed() && singleSelection)
        {
            grabingGizmo = gizmoHandler.CheckTouchingGizmos(mousePos);
        }

        if(Input.LBDown())
        {
            if(!grabingGizmo)
            {
                Mouse.SetCursor(MouseCursor.SizeAll);
                Point difference = (editor.GetMouseTile(editor.StartMousePos) - editor.GetMouseTile(mousePos));

                foreach (Room selectedRoom in selectedRooms)
                {
                    Point finalPos = selectedRoom.InitialBox.Location - difference;

                    if (selectedRoom.Box.Location != finalPos)
                        selectedRoom.Box = selectedRoom.Box with { Location = finalPos };
                }
            }
            else if(singleSelection)
            {
                Room selectedRoom = selectedRooms.Single();
                Rectangle gizmoTransform = gizmoHandler.GizmosControls(mousePos, selectedRoom.Box);

                selectedRoom.Box = gizmoTransform;
                
                indicatorDirection = gizmoHandler.GrabedGizmoSide;
                if (gizmoTransform.X < selectedRoom.Box.X) indicatorDirection.X = -1;
                if (gizmoTransform.Y < selectedRoom.Box.Y) indicatorDirection.Y = -1;
            }
            else
            {
                Console.WriteLine("Grabing gizmo and not single selection!");
            }

            canChangeRooms = true;
            foreach (Room selectedRoom in selectedRooms)
            foreach (Room room in nonSelectedRooms)
            {
                if (selectedRoom.Box.Intersects(room.Box))
                    canChangeRooms = false;
            }
        }
            
        if (Input.LBReleased())
        {
            if (canChangeRooms)
            {
                selectedRooms.Iterate(r => r.ApplyTransform());
            }
            else
            {
                canChangeRooms = true;
                selectedRooms.Iterate(r => r.CancelTransform());
            }
            
            Mouse.SetCursor(MouseCursor.Arrow);
            indicatorDirection = Vector2.Zero;
            grabingGizmo = false;

            if (singleSelection)
                gizmoHandler = new GizmoHandler(selectedRooms.Single().Box, editor, this);
            else
                gizmoHandler = null;
        }
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
                
                room.Tiles.Iterate(tile =>
                {
                    writer.Write((byte)tile);
                });

                // for(int y = 0; y < room.Tiles.GetLength(0); ++y)
                // for(int x = 0; x < room.Tiles.GetLength(1); ++x)
                // {
                //     writer.Write((byte)room.Tiles[y, x]);
                // }
            }
        }
    }

    //Draw
    public void DrawUnderGrid(SpriteBatch spriteBatch)
    {
        //TODO: maybe optimize tile drawing
        foreach (Room room in nonSelectedRooms)
        {
            spriteBatch.FillRectangle(ScaleRoom(room.Box), roomColor);
            
            room.Tiles.Iterate((y, x) =>
            {
                Map.Tile tile = room.Tiles[y, x];
                if (tile == Map.Tile.None) return;
                
                Point pos = (new Point(x, y) + room.Box.Location) * new Point(Map.TileUnit);
                spriteBatch.Draw(editor.TileTextures[tile], new Rectangle(pos, new Point(Map.TileUnit)), Color.White);
            });
        }

        foreach (Room selectedRoom in selectedRooms)
        {
            spriteBatch.FillRectangle(ScaleRoom(selectedRoom.Box), canChangeRooms ? canPlaceColor : cannotPlaceColor);
            
            selectedRoom.InitialTiles.Iterate((y, x) =>
            {
                Map.Tile tile = selectedRoom.InitialTiles[y, x];

                bool skip = (
                    tile == Map.Tile.None ||
                    y < 0 ||
                    x < 0 ||
                    y >= selectedRoom.InitialTiles.Height ||
                    x <= selectedRoom.InitialTiles.Width
                );
                if (skip) return;

                Point pos = (new Point(x, y) + selectedRoom.Box.Location) * new Point(Map.TileUnit);
                spriteBatch.Draw(editor.TileTextures[tile], new Rectangle(pos, new Point(Map.TileUnit)), Color.White);
            });
        }
    } 
    
    public void DrawOnGrid(SpriteBatch spriteBatch, Vector2 mousePos)
    {
        foreach (Room room in nonSelectedRooms)
            spriteBatch.DrawRectangle(ScaleRoom(room.Box), roomOutlineColor, 1f / editor.CameraMatrixScale.X);
        
        Color outlineColor = canChangeRooms ? canPlaceColor : cannotPlaceColor;

        if (constructionRoom != Rectangle.Empty)
            spriteBatch.DrawRectangle(constructionRoomOutline, outlineColor, 1.5f / editor.CameraMatrixScale.X);

        foreach (Room selectedRoom in selectedRooms)
        {
            spriteBatch.DrawRectangle(ScaleRoom(selectedRoom.Box), selectedColor, 1.5f / editor.CameraMatrixScale.X);
        }
        
        if((Input.LBUp() || grabingGizmo) && singleSelection)
            gizmoHandler.DrawGizmos(spriteBatch);

        if (indicatorDirection != Vector2.Zero)
        {
            Rectangle box = selectedRooms.SingleOrDefault()?.Box ?? constructionRoom;
            Point unit = new Point(Map.TileUnit);
            Rectangle fullRoom = new(box.Location, fullRoomSize - new Size(1,1));
            
            if (indicatorDirection.X == -1) fullRoom.X = fullRoom.X + box.Width - fullRoom.Width - 1;
            if (indicatorDirection.Y == -1) fullRoom.Y = fullRoom.Y + box.Height - fullRoom.Height - 1;
            
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
    private Rectangle box;
    private Map.Tile[,] tiles;
    
    public ReadOnly2DArray<Map.Tile> Tiles => new(tiles);
    public Rectangle Box
    {
        get => box;
        set
        {
            Rectangle oldBox = box;
            box = value;

            if(oldBox.Size == box.Size) return;

            //New tiles
            int yOffset = oldBox.Height - box.Height;
            int xOffset = oldBox.Width - box.Width;

            if (oldBox.X == box.X) xOffset = 0;
            if (oldBox.Y == box.Y) yOffset = 0;
            
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
        }
    }
    
    public Room(Rectangle box, Map.Tile[,] tiles)
    {
        this.box = box;
        this.tiles = tiles;
    }
    
    public Room(Rectangle box)
    {
        this.box = box;
        this.tiles = new Map.Tile[box.Height, box.Width];
    }

    public void SetTile(int x, int y, Map.Tile tile) => tiles[y, x] = tile;
    
    public object Clone() => new Room(box, tiles);
    
    //Selection
    private Rectangle initialBox;
    private Map.Tile[,] initialTiles;
    private bool selected;
    
    public Rectangle InitialBox => initialBox;
    public ReadOnly2DArray<Map.Tile> InitialTiles => new(initialTiles);
    public bool Selected 
    { 
        get => selected;
        set 
        {
            selected = value;
            ApplyTransform();
        }
    }

    public void CancelTransform()
    {
        box = initialBox;
        tiles = initialTiles;
    }

    public void ApplyTransform()
    {
        initialBox = box;
        initialTiles = (Map.Tile[,])tiles.Clone();
    }
}