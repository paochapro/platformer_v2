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
    private List<Rectangle> rooms = new();
    
    private static readonly Color canPlaceColor = Color.Gray;
    private static readonly Color cannotPlaceColor = Color.DarkRed;
    
    private static readonly Color roomColor = new(40, 40, 40);
    private static readonly Color selectedColor = Color.Gold;
    
    private bool canPlaceRoom;
    private Rectangle constructionRoomOutline;
    private Rectangle constructionRoom;
    private Point startMouseTile;
    private Rectangle selectedRoom;
    private int selectedRoomIndex;

    public RoomManager(Editor editor)
    { 
        this.editor = editor;
        ModeSwitch();
    }

    public void ModeSwitch()
    {
        canPlaceRoom = false;
        startMouseTile = Point.Zero;
        constructionRoom = Rectangle.Empty;
        constructionRoomOutline = Rectangle.Empty;
        selectedRoom = Rectangle.Empty;
        selectedRoomIndex = -1;
    }

    private Rectangle RoomConstruction(Point startMouseTile, Point endMouseTile)
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
        foreach (Rectangle room in rooms)
        {
            if (constructedRoom.Intersects(room))
                canPlaceRoom = false;
        }
        
        return constructedRoom;
    }
    
    private void PlaceRoom(Rectangle room)
    {
        rooms.Add(room);
    }
    
    public void RoomConstructionControls(Point mouseTile)
    {
        if (Input.LBPressed()) startMouseTile = mouseTile;
        if (Input.LBDown())
        {
            constructionRoom = RoomConstruction(startMouseTile, mouseTile);

            constructionRoomOutline = ScaleRoom(constructionRoom);
        }
        if (Input.LBReleased())
        {
            if(canPlaceRoom) 
                PlaceRoom(constructionRoom);
            
            constructionRoomOutline = Rectangle.Empty;
            constructionRoom = Rectangle.Empty;
        }
    }
    
    public void RoomSelectionControls(Point mouseTile)
    {
        if (Input.LBPressed())
        {
            startMouseTile = mouseTile;
            selectedRoomIndex = -1;

            for (int i = 0; i < rooms.Count; ++i)
            {
                Rectangle room = rooms[i];

                if (room.Contains(mouseTile))
                {
                    selectedRoomIndex = i;
                    selectedRoom = room;
                }
            }
        }

        if(selectedRoomIndex == -1) return;

        Point endMouseTile = mouseTile;
        
        if (Input.LBDown())
        {
            Point difference = (startMouseTile - endMouseTile);
            selectedRoom.Location = rooms[selectedRoomIndex].Location - difference;

            canPlaceRoom = true;
            for(int i = 0; i < rooms.Count; ++i)
            {
                if (selectedRoomIndex == i) continue;
                
                if (selectedRoom.Intersects(rooms[i]))
                    canPlaceRoom = false;
            }
        }

        if (Input.LBReleased())
        {
            if(canPlaceRoom)
                rooms[selectedRoomIndex] = selectedRoom;
            else
            {
                selectedRoom = rooms[selectedRoomIndex];
                canPlaceRoom = true;
            }
        }
    }

    public void DrawUnderGrid(SpriteBatch spriteBatch)
    {
        for(int i = 0; i < rooms.Count; ++i)
        {
            if (selectedRoomIndex == i) continue;
            Rectangle scaledRoom = ScaleRoom(rooms[i]);
            spriteBatch.FillRectangle(scaledRoom, roomColor);
        }

        if (selectedRoomIndex != -1)
        {
            spriteBatch.FillRectangle(ScaleRoom(selectedRoom), canPlaceRoom ? canPlaceColor : cannotPlaceColor);
        }
    }
    
    public void DrawOnGrid(SpriteBatch spriteBatch)
    {
        Color outlineColor = canPlaceRoom ? canPlaceColor : cannotPlaceColor;
        spriteBatch.DrawRectangle(constructionRoomOutline, outlineColor, 1.5f / editor.CameraMatrixScale.X);

        if (selectedRoomIndex != -1)
        {
            spriteBatch.DrawRectangle(ScaleRoom(selectedRoom), selectedColor, 1.5f / editor.CameraMatrixScale.X);
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
}