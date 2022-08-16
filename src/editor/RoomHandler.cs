using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;
using MonoGame.Extended.Screens;

using Lib;

namespace PlatformerV2.LevelEditor;

partial class RoomHandler
{
    private Editor editor;
    private GizmoHandler gizmoHandler;
    private List<Room> rooms = new();
    
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
    private Rectangle selectedTransform;
    
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
    
    //Room construction
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
        selectedTransform = Rectangle.Empty;
        selectedRoom = null;
        gizmoHandler = null;
    }

    public void RoomSelectionControls(Vector2 mousePos)
    {
        Point startMouseTile = editor.GetMouseTile(editor.StartMousePos);
        
        if(selectedRoom != null)
            SelectedRoomControls(mousePos);
        
        if (Input.LBPressed() && (selectedRoom == null || !selectedRoom.Box.Contains(startMouseTile)))
        {
            ResetSelection();

            foreach (Room room in rooms)
                if (room.Box.Contains(startMouseTile))
                {
                    selectedRoom = room;
                    selectedTransform = room.Box;
                    gizmoHandler = new GizmoHandler(editor, this);
                }
        }
    }

    private void SelectedRoomControls(Vector2 mousePos)
    {
        Vector2 endMousePos = mousePos;
        
        if (Input.KeyPressed(Keys.X))
        {
            rooms.Remove(selectedRoom);
            ResetSelection();
            return;
        }
        
        bool gizmoGrabed = gizmoHandler.UpdateGizmos(mousePos, selectedRoom.Box, ScaleRoom(selectedTransform));
        
        if (Input.LBDown())
        {
            if (gizmoGrabed)
                selectedTransform = gizmoHandler.GizmosControls(mousePos, selectedTransform);
            else
            {
                Mouse.SetCursor(MouseCursor.SizeAll);
                Point difference = (editor.GetMouseTile(editor.StartMousePos) - editor.GetMouseTile(endMousePos));
                selectedTransform.Location = selectedRoom.Box.Location - difference;
            }
            
            canChangeRoom = true;
            foreach (Room room in rooms)
            {
                if(selectedRoom == room) continue;
                
                if (selectedTransform.Intersects(room.Box))
                    canChangeRoom = false;
            }
            
        }

        if (Input.LBReleased())
        {
            if (canChangeRoom)
                selectedRoom.Box = selectedTransform;
            else
            {
                canChangeRoom = true;
                selectedTransform = selectedRoom.Box;
            }
        }
        
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
            Rectangle final = ScaleRoom(selectedTransform);
            spriteBatch.FillRectangle(final, canChangeRoom ? canPlaceColor : cannotPlaceColor);
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
            Rectangle final = ScaleRoom(selectedTransform);
            spriteBatch.DrawRectangle(final, selectedColor, 1.5f / editor.CameraMatrixScale.X);
            gizmoHandler.DrawGizmos(spriteBatch);
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

class Room
{
    public Rectangle Box;
    public Room(Rectangle box) => Box = box;
}