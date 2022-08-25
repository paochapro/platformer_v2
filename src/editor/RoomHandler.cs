using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;

using Lib;
using PlatformerV2.Main;

namespace PlatformerV2.LevelEditor;

partial class RoomHandler
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
    private bool isConstructingRoom;
    private Rectangle constructionRoomOutline;
    private Rectangle constructionRoom;
    
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
        isConstructingRoom = false;
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
        if (Input.LBPressed())
        {
            isConstructingRoom = true;
        }

        if (isConstructingRoom)
            ConstructionControls(mousePos);
        else
        {
            constructionRoomOutline = Rectangle.Empty;
            constructionRoom = Rectangle.Empty;
        }
        
        if (Input.LBReleased())
        {
            isConstructingRoom = false;
            return;
        }
    }

    private void ConstructionControls(Vector2 mousePos)
    {
        if (Input.LBDown())
        {
            constructionRoom = ConstructRoom(editor.GetMouseTile(editor.StartMousePos), editor.GetMouseTile(mousePos));
            constructionRoomOutline = ScaleRoom(constructionRoom);
        }
        
        if (Input.LBReleased())
        {
            if(canChangeRoom) 
                PlaceRoom(new Room(constructionRoom));
            
            constructionRoomOutline = Rectangle.Empty;
            constructionRoom = Rectangle.Empty;

            isConstructingRoom = false;
        }
    }

    //Room selection
    private void ResetSelection()
    {
        transformRoom = null;
        selectedRoom = null;
        gizmoHandler = null;

        grabingGizmo = false;
    }

    public void RoomSelectionControls(Vector2 mousePos)
    {
        Point startMouseTile = editor.GetMouseTile(editor.StartMousePos);
        
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


    bool grabingGizmo = false;
    private void SelectedRoomControls(Vector2 mousePos)
    {
        Vector2 endMousePos = mousePos;
        
        if (Input.KeyPressed(Keys.X))
        {
            rooms.Remove(selectedRoom);
            ResetSelection();
            return;
        }

        //Main stuff
        if(Input.LBUp() || grabingGizmo)
        {
            gizmoHandler.UpdateGizmos(mousePos, selectedRoom.Box, ScaleRoom(transformRoom.Box));
        }

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
                    Color tileColor = tile != Map.Tile.None ? Color.Black : Color.Transparent;
                    Point pos = (new Point(x, y) + finalRoom.Box.Location) * new Point(Map.TileUnit);
                    spriteBatch.FillRectangle(new Rectangle(pos, new Point(Map.TileUnit)), tileColor);

                    x++;
                }
                y++;
                x = 0;
            }
        }
    } 
    
    public void DrawOnGrid(SpriteBatch spriteBatch)
    {
        foreach (Room room in rooms)
        {
            if (room == selectedRoom) continue;
            spriteBatch.DrawRectangle(ScaleRoom(room.Box), roomOutlineColor, 1f / editor.CameraMatrixScale.X);
        }
        
        Color outlineColor = canChangeRoom ? canPlaceColor : cannotPlaceColor;

        if (isConstructingRoom)
            spriteBatch.DrawRectangle(constructionRoomOutline, outlineColor, 1.5f / editor.CameraMatrixScale.X);
        
        if (selectedRoom != null)
        {
            Rectangle final = ScaleRoom(transformRoom.Box);
            spriteBatch.DrawRectangle(final, selectedColor, 1.5f / editor.CameraMatrixScale.X);

            if(Input.LBUp() || grabingGizmo)
                gizmoHandler.DrawGizmos(spriteBatch);
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
    
    public Room(Rectangle box)
    {
        this.box = box;
        this.tiles = new Map.Tile[box.Height, box.Width];
        this.readonlyTiles = Enumerable.Empty<IEnumerable<Map.Tile>>();
    } 

    public Room() : this(Rectangle.Empty) {}
}