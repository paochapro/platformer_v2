using System.Collections;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;
using MonoGame.Extended.Screens;

using Lib;

namespace PlatformerV2.LevelEditor;

class GizmoHandler
{
    private Editor editor;
    private RoomHandler roomHandler;
    private Rectangle selectedRoom;
    private Gizmos gizmos;

    public GizmoHandler(Rectangle selectedRoom, Editor editor, RoomHandler roomHandler)
    {
        this.selectedRoom = selectedRoom;
        this.roomHandler = roomHandler;
        this.editor = editor;
        this.gizmos = new Gizmos();
    }

    class Gizmo
    {
        public RectangleF Rect;
        public Vector2 Direction { get; private set; }
        public MouseCursor Cursor { get; private set; }
        public Gizmo(Vector2 direction)
        {
            Direction = direction;
            
            if(direction.X == 0) Cursor = MouseCursor.SizeNS;
            if(direction.Y == 0) Cursor = MouseCursor.SizeWE;

            if (direction == new Vector2(-1,-1) || direction == new Vector2(1,1))
            {
                Cursor = MouseCursor.SizeNWSE;
            }
            
            if (direction == new Vector2(-1,1) || direction == new Vector2(1,-1))
            {
                Cursor = MouseCursor.SizeNESW;
            }
        } 
    }

    class Gizmos
    {
        public Gizmo Top         = new(-Vector2.UnitY);
        public Gizmo Bottom      = new(Vector2.UnitY);
        public Gizmo Left        = new(-Vector2.UnitX);
        public Gizmo Right       = new(Vector2.UnitX);
        public Gizmo TopLeft     = new(new Vector2(-1,-1));
        public Gizmo TopRight    = new(new Vector2(1, -1));
        public Gizmo BottomLeft  = new(new Vector2(-1, 1));
        public Gizmo BottomRight = new(new Vector2(1, 1));

        public IEnumerator<Gizmo> GetEnumerator()
        {
            yield return Top;
            yield return Bottom;
            yield return Left;
            yield return Right;
            yield return TopLeft;
            yield return TopRight;
            yield return BottomLeft;
            yield return BottomRight;
        }
    }

    private const int cornerGizmoSizeDefault = 16;
    private float cornerGizmoSize;
    private Gizmo? grabedGizmo;
    
    //Gizmos
    public bool UpdateGizmos(Vector2 mousePos, Rectangle selectedTransform)
    {
        RectangleF box = selectedTransform;

        cornerGizmoSize = cornerGizmoSizeDefault / editor.CameraMatrixScale.X;
        
        float cornerSize = cornerGizmoSize;
        
        gizmos.Top.Rect      = new(box.Left + cornerSize, box.Top, box.Width - cornerSize*2, cornerSize);
        gizmos.Bottom.Rect   = new(box.Left + cornerSize, box.Bottom - cornerSize, box.Width - cornerSize*2, cornerSize);
        gizmos.Left.Rect     = new(box.Left, box.Top + cornerSize, cornerSize, box.Height - cornerSize*2);
        gizmos.Right.Rect    = new(box.Right - cornerGizmoSize, box.Top + cornerSize, cornerSize, box.Height - cornerSize*2);

        gizmos.TopLeft.Rect          = new(box.Left, box.Top, cornerSize, cornerSize);
        gizmos.TopRight.Rect         = new(box.Right - cornerSize, box.Top, cornerSize, cornerSize);
        gizmos.BottomLeft.Rect       = new(box.Left, box.Bottom - cornerSize, cornerSize, cornerSize);
        gizmos.BottomRight.Rect      = new(box.Right - cornerSize, box.Bottom - cornerSize, cornerSize, cornerSize);

        Gizmo? touchingGizmo = null;

        foreach (Gizmo gizmo in gizmos)
        {
            if (gizmo.Rect.Contains(mousePos))
            {
                touchingGizmo = gizmo;
                Mouse.SetCursor(gizmo.Cursor);
            }
        }

        if (Input.LBPressed())
            grabedGizmo = touchingGizmo;

        return grabedGizmo != null;
    }
    
    public Rectangle GizmosControls(Vector2 mousePos)
    {
        Rectangle selectedTransform;
        
        if (grabedGizmo == null)
        {
            Console.WriteLine("No gizmo grabed in GizmoControls! Line: 294");
            return Rectangle.Empty;
        }
        
        selectedTransform = selectedRoom;
        Vector2 dir = grabedGizmo.Direction;
        Mouse.SetCursor(grabedGizmo.Cursor);

        Point mouseTile = editor.GetMouseTile(mousePos);
        
        if (dir.Y == 1)
        {
            selectedTransform.Height = mouseTile.Y - selectedRoom.Y + 1;
        }
        
        if (dir.Y == -1)
        {
            selectedTransform.Y = mouseTile.Y;
            selectedTransform.Height = selectedRoom.Height + (selectedRoom.Y - selectedTransform.Y);
        }

        if (dir.X == 1)
        {
            selectedTransform.Width = mouseTile.X - selectedRoom.X + 1;
        }
        
        if (dir.X == -1)
        {
            selectedTransform.X = mouseTile.X;
            selectedTransform.Width = selectedRoom.Width + (selectedRoom.X - selectedTransform.X);
        }

        /*Console.WriteLine("dir: " + dir);

        if (selectedTransform.Width <= 0)
        {
            if (dir.X == 1)
            {
                grabedGizmo = gizmos.Left;
            }
            if(dir.X == -1)
            {
                grabedGizmo = gizmos.Right;
            }

            selectedTransform.Width = 1;
        }
        
        if (selectedTransform.Height < 0)
        {
            grabedGizmo = gizmos.Top;

            int positiveHeight = Math.Abs(selectedTransform.Height);
            
            selectedTransform.Y = selectedTransform.Y - positiveHeight;
            selectedTransform.Height = positiveHeight;
        }
        */

        return selectedTransform;
    }

    public void DrawGizmos(SpriteBatch spriteBatch)
    {
        //Gizmos
        float gizmoAlpha = 50;
        void drawGizmo(Color color, Gizmo gizmo) => spriteBatch.FillRectangle(gizmo.Rect, new Color(color, gizmoAlpha));
            
        drawGizmo(Editor.gridCenterVerticalColor, gizmos.Top);
        drawGizmo(Editor.gridCenterVerticalColor, gizmos.Bottom);
        drawGizmo(Editor.gridCenterHorizontalColor, gizmos.Left);
        drawGizmo(Editor.gridCenterHorizontalColor, gizmos.Right);
            
        Color cornerGizmoColor = Color.White;
        drawGizmo(cornerGizmoColor, gizmos.TopLeft);
        drawGizmo(cornerGizmoColor, gizmos.TopRight);
        drawGizmo(cornerGizmoColor, gizmos.BottomLeft);
        drawGizmo(cornerGizmoColor, gizmos.BottomRight);
    }
}