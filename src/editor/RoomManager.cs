using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;
using MonoGame.Extended.Screens;

using Lib;

namespace PlatformerV2.LevelEditor;

class RoomManager
{
    private Editor editor;
    //private List<Rectangle> rooms = new();
    private List<Room> rooms = new();
    
    private static readonly Color canPlaceColor = Color.Gray;
    private static readonly Color cannotPlaceColor = Color.DarkRed;
    
    private static readonly Color roomColor = new(40, 40, 40);
    private static readonly Color roomOutlineColor = Color.Black;//new(60, 60, 60);
    private static readonly Color selectedColor = Color.Gold;
    
    private bool canPlaceRoom;
    private Point startMousePos;
    
    //Room construction
    private bool isConstructingRoom;
    private Rectangle constructionRoomOutline;
    private Rectangle constructionRoom;
    
    //Room selection
    private Room? selectedRoom;
    private Point selectedPos;
    private RectangleF selectionBox;
    
    public RoomManager(Editor editor)
    { 
        this.editor = editor;
        ModeSwitch();
    }

    public void ModeSwitch()
    {
        canPlaceRoom = false;
        isConstructingRoom = false;
        constructionRoom = Rectangle.Empty;
        constructionRoomOutline = Rectangle.Empty;
        selectedRoom = null;
        startMousePos = Point.Zero;
    }

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

        canPlaceRoom = true;
        foreach (Room room in rooms)
        {
            if (constructedRoom.Intersects(room.Box))
                canPlaceRoom = false;
        }
        
        return constructedRoom;
    }
    
    private void PlaceRoom(Room room)
    {
        rooms.Add(room);
    }
    
    //Room construction
    public void RoomConstructionControls(Point mousePos)
    {
        if (Input.LBPressed())
        {
            startMousePos = mousePos;
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
            startMousePos = Point.Zero;
            isConstructingRoom = false;
            return;
        }
    }

    private void ConstructionControls(Point mousePos)
    {
        if (Input.LBDown())
        {
            constructionRoom = ConstructRoom(GetMouseTile(startMousePos), GetMouseTile(mousePos));
            constructionRoomOutline = ScaleRoom(constructionRoom);
        }
        
        if (Input.LBReleased())
        {
            if(canPlaceRoom) 
                PlaceRoom(new Room(constructionRoom));
            
            constructionRoomOutline = Rectangle.Empty;
            constructionRoom = Rectangle.Empty;

            isConstructingRoom = false;
        }
    }

    private void ResetSelection()
    {
        selectedPos = Point.Zero;
        selectedRoom = null;
        selectionBox = RectangleF.Empty;
        startMousePos = Point.Zero;
    }
    
    //Room selection
    public void RoomSelectionControls(Point mousePos)
    {
        if (Input.LBPressed())
        {
            ResetSelection();

            //Rooms
            startMousePos = mousePos;
            
            foreach (Room room in rooms)
                if (room.Box.Contains(GetMouseTile(startMousePos)))
                {
                    selectedRoom = room;
                    selectedPos = room.Box.Location;
                }

            //Selection box
            if (selectedRoom == null)
                selectionBox = new RectangleF(startMousePos, Size2.Empty);
        }

        if (selectedRoom != null)
        {
            SelectedRoomControls(mousePos);
            return;
        }

        if (selectionBox != RectangleF.Empty);       
            SelectionBoxControls(mousePos);
    }

    private void SelectedRoomControls(Point mousePos)
    {
        Point endMousePos = mousePos;
        
        if (Input.KeyPressed(Keys.X))
        {
            rooms.Remove(selectedRoom);
            ResetSelection();
            return;
        }

        if (Input.LBDown())
        {
            Point difference = (GetMouseTile(startMousePos) - GetMouseTile(endMousePos));
            selectedPos = selectedRoom.Box.Location - difference;

            canPlaceRoom = true;
            foreach (Room room in rooms)
            {
                if(selectedRoom == room) continue;

                Rectangle currentSelection = selectedRoom.Box with { Location = selectedPos };
                
                if (currentSelection.Intersects(room.Box))
                    canPlaceRoom = false;
            }
        }
        
        if (Input.LBReleased())
        {
            if (canPlaceRoom)
                selectedRoom.Box.Location = selectedPos;
            else
            {
                canPlaceRoom = true;
                selectedPos = selectedRoom.Box.Location;
            }
        }
    }

    private void SelectionBoxControls(Point mousePos)
    {
        if (Input.LBDown())
            selectionBox = RectangleF.Union(new RectangleF(startMousePos, Size2.Empty), new RectangleF(mousePos, Size2.Empty));

        if (Input.LBReleased())
            ResetSelection();
    }

    //Draw
    public void DrawUnderGrid(SpriteBatch spriteBatch)
    {
        foreach (Room room in rooms)
        {
            if (room == selectedRoom) continue;
            spriteBatch.FillRectangle(ScaleRoom(room.Box), roomColor);
        }

        if (selectedRoom != null)
        {
            Rectangle final = ScaleRoom(selectedRoom.Box with { Location = selectedPos });
            spriteBatch.FillRectangle(final, canPlaceRoom ? canPlaceColor : cannotPlaceColor);
        }
    }
    
    public void DrawOnGrid(SpriteBatch spriteBatch)
    {
        foreach (Room room in rooms)
        {
            if (room == selectedRoom) continue;
            spriteBatch.DrawRectangle(ScaleRoom(room.Box), roomOutlineColor, 1f / editor.CameraMatrixScale.X);
        }
        
        spriteBatch.DrawRectangle(selectionBox, Color.Orange, 1f / editor.CameraMatrixScale.X);
        
        Color outlineColor = canPlaceRoom ? canPlaceColor : cannotPlaceColor;

        if (isConstructingRoom)
            spriteBatch.DrawRectangle(constructionRoomOutline, outlineColor, 1.5f / editor.CameraMatrixScale.X);
        
        if (selectedRoom != null)
        {
            Rectangle final = ScaleRoom(selectedRoom.Box with { Location = selectedPos });
            spriteBatch.DrawRectangle(final, selectedColor, 1.5f / editor.CameraMatrixScale.X);
        }
        
    }

    private Rectangle ScaleRoom(Rectangle room)
    {
        return room with
        {
            Location = room.Location * new Point(Editor.TileUnit),
            Size = room.Size * new Point(Editor.TileUnit),
        };
    }
    
    private Point GetMouseTile(Point mousePos)
    {
        return new( (int)Math.Floor(mousePos.X / (float)Editor.TileUnit), (int)Math.Floor(mousePos.Y / (float)Editor.TileUnit) );
    }
}

class Room
{
    public Rectangle Box;
    public Room(Rectangle box) => Box = box;
}