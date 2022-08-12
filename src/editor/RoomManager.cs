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
    
    private bool canPlaceRoom;
    private List<Rectangle> rooms = new();
    private Color canPlaceColor = Color.Gray;
    private Color cannotPlaceColor = Color.DarkRed;
    private static readonly Color roomColor = new(40, 40, 40);
    private Rectangle constructionRoomOutline;
    private Rectangle constructionRoom;
    private Point startMouseTile;

    public RoomManager(Editor editor) => this.editor = editor;

    private Rectangle RoomConstruction(Point startMouseTile, Point endMouseTile)
    {
        Point startPos = startMouseTile;
        Point endPos = endMouseTile;

        if (endPos.X >= startPos.X)
            startPos.X -= 1;
        else
            endPos.X -= 1;
        
        if (endPos.Y >= startPos.Y)
            startPos.Y -= 1;
        else
            endPos.Y -= 1;

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
            
            constructionRoomOutline = constructionRoom with 
            {
                Location = constructionRoom.Location * new Point(Editor.TileUnit),
                Size = constructionRoom.Size * new Point(Editor.TileUnit)
            };
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
        
    }

    public void DrawRooms(SpriteBatch spriteBatch)
    {
        foreach (Rectangle room in rooms)
        {
            Rectangle scaledRoom = room with
            {
                Location = room.Location * new Point(Editor.TileUnit),
                Size = room.Size * new Point(Editor.TileUnit),
            };
            
            spriteBatch.FillRectangle(scaledRoom, roomColor);
        }
    }
    
    public void DrawConstructionRoomOutline(SpriteBatch spriteBatch)
    {
        Color outlineColor = canPlaceRoom ? canPlaceColor : cannotPlaceColor;
        spriteBatch.DrawRectangle(constructionRoomOutline, outlineColor, 1.5f / editor.CameraMatrixScale.X);
    }
}