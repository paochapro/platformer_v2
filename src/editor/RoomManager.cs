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
    private Rectangle selectedTransform;
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
        startMousePos = Point.Zero;
        
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
        selectedTransform = Rectangle.Empty;
        selectedRoom = null;
        selectionBox = RectangleF.Empty;
        startMousePos = Point.Zero;
        gizmoSide = GizmoSide.None;
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
                    selectedTransform = room.Box;
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
        UpdateGizmos();
        
        if (Input.KeyPressed(Keys.X))
        {
            rooms.Remove(selectedRoom);
            ResetSelection();
            return;
        }

        if (Input.LBDown())
        {
            //Gizmos
            GizmosControls(mousePos);
            
            //Move
            if (gizmoSide == GizmoSide.None)
            {
                Point difference = (GetMouseTile(startMousePos) - GetMouseTile(endMousePos));
                selectedTransform.Location = selectedRoom.Box.Location - difference;
            }
            
            canPlaceRoom = true;
            foreach (Room room in rooms)
            {
                if(selectedRoom == room) continue;
                
                if (selectedTransform.Intersects(room.Box))
                    canPlaceRoom = false;
            }
            
        }

        if (Input.LBReleased())
        {
            if (canPlaceRoom)
                selectedRoom.Box = selectedTransform;
            else
            {
                canPlaceRoom = true;
                selectedTransform = selectedRoom.Box;
            }
        }
        
    }

    private(RectangleF Top, 
            RectangleF Bottom, 
            RectangleF Left, 
            RectangleF Right,
            RectangleF TopLeft, 
            RectangleF TopRight, 
            RectangleF BottomLeft, 
            RectangleF BottomRight) sizeGizmos;

    private const int cornerGizmoSizeDefault = 16;
    private float cornerGizmoSize;

    enum GizmoSide { None, Top, Bottom, Left, Right }
    private GizmoSide gizmoSide;

    private void UpdateGizmos()
    {
        Rectangle box = ScaleRoom(selectedTransform);

        cornerGizmoSize = cornerGizmoSizeDefault / editor.CameraMatrixScale.X;
        
        float cornerSize = cornerGizmoSize;
        
        sizeGizmos.Top      = new(box.Left + cornerSize, box.Top, box.Width - cornerSize*2, cornerSize);
        sizeGizmos.Bottom   = new(box.Left + cornerSize, box.Bottom - cornerSize, box.Width - cornerSize*2, cornerSize);
        sizeGizmos.Left     = new(box.Left, box.Top + cornerSize, cornerSize, box.Height - cornerSize*2);
        sizeGizmos.Right    = new(box.Right - cornerGizmoSize, box.Top + cornerSize, cornerSize, box.Height - cornerSize*2);

        sizeGizmos.TopLeft          = new(box.Left, box.Top, cornerSize, cornerSize);
        sizeGizmos.TopRight         = new(box.Right - cornerSize, box.Top, cornerSize, cornerSize);
        sizeGizmos.BottomLeft       = new(box.Left, box.Bottom - cornerSize, cornerSize, cornerSize);
        sizeGizmos.BottomRight      = new(box.Right - cornerSize, box.Bottom - cornerSize, cornerSize, cornerSize);
    }

    private void GizmosControls(Point mousePos)
    {
        if (Input.LBPressed())
        {
            startMousePos = mousePos;
            
            if (sizeGizmos.Top.Contains(mousePos))      gizmoSide = GizmoSide.Top;
            if (sizeGizmos.Bottom.Contains(mousePos))   gizmoSide = GizmoSide.Bottom;
            if (sizeGizmos.Left.Contains(mousePos))     gizmoSide = GizmoSide.Left;
            if (sizeGizmos.Right.Contains(mousePos))    gizmoSide = GizmoSide.Right;
        }

        if (gizmoSide != GizmoSide.None)
        {
            selectedTransform = selectedRoom.Box;
            
            if (gizmoSide == GizmoSide.Top)
            {
                selectedTransform.Y = GetMouseTile(mousePos).Y;
                selectedTransform.Height = selectedRoom.Box.Height + (selectedRoom.Box.Y - selectedTransform.Y);
            }
            
            if (gizmoSide == GizmoSide.Bottom)
            {
                selectedTransform.Height = GetMouseTile(mousePos).Y - selectedRoom.Box.Y + 1;
            }
            
            if (gizmoSide == GizmoSide.Left)
            {
                selectedTransform.X = GetMouseTile(mousePos).X;
                selectedTransform.Width = selectedRoom.Box.Width + (selectedRoom.Box.X - selectedTransform.X);
            }
            
            if (gizmoSide == GizmoSide.Right)
            {
                selectedTransform.Width = GetMouseTile(mousePos).X - selectedRoom.Box.X + 1;
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
            Rectangle final = ScaleRoom(selectedTransform);
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
            Rectangle final = ScaleRoom(selectedTransform);
            spriteBatch.DrawRectangle(final, selectedColor, 1.5f / editor.CameraMatrixScale.X);
            
            //Gizmos
            float gizmoAlpha = 50;
            void drawGizmo(Color color, RectangleF rect) => spriteBatch.FillRectangle(rect, new Color(color, gizmoAlpha));
            
            drawGizmo(Editor.gridCenterVerticalColor, sizeGizmos.Top);
            drawGizmo(Editor.gridCenterVerticalColor, sizeGizmos.Bottom);
            drawGizmo(Editor.gridCenterHorizontalColor, sizeGizmos.Left);
            drawGizmo(Editor.gridCenterHorizontalColor, sizeGizmos.Right);
            
            Color cornerGizmoColor = Color.White;
            drawGizmo(cornerGizmoColor, sizeGizmos.TopLeft);
            drawGizmo(cornerGizmoColor, sizeGizmos.TopRight);
            drawGizmo(cornerGizmoColor, sizeGizmos.BottomLeft);
            drawGizmo(cornerGizmoColor, sizeGizmos.BottomRight);
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