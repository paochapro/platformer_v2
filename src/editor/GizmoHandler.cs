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
    private Gizmos gizmos;
    private Rectangle initialBox;
    
    private const float gizmoAlpha = 0.05f;
    private const int cornerGizmoSizeDefault = 16;
    private float cornerGizmoSize;
    private Gizmo? grabedGizmo;
    public Vector2 GrabedGizmoSide => grabedGizmo.Side;

    public GizmoHandler(Rectangle initialBox, Editor editor, RoomHandler roomHandler)
    {
        this.initialBox = initialBox;
        this.roomHandler = roomHandler;
        this.editor = editor;
        this.gizmos = new Gizmos();
    }

    class Gizmo
    {
        public RectangleF Rect;
        public Vector2 Side { get; init; }
        public MouseCursor Cursor { get; init; }
        public Color Color { get; init; }

        public Gizmo(Vector2 side)
        {
            Side = side;

            const int minusV = 100;
            const int minusH = 100;
            
            if (side.X == 0)
            {
                Cursor = MouseCursor.SizeNS;
                Color = new(132 - minusV, 206 - minusV, 13 - minusV);
            }

            if (side.Y == 0)
            {
                Cursor = MouseCursor.SizeWE;
                Color = new(208-minusH,56-minusH,78-minusH);
            }

            if (side == new Vector2(-1,-1) || side == new Vector2(1,1))
            {
                Cursor = MouseCursor.SizeNWSE;
                Color = Color.White;
            }
            
            if (side == new Vector2(-1,1) || side == new Vector2(1,-1))
            {
                Cursor = MouseCursor.SizeNESW;
                Color = Color.White;
            }
        } 
        
        //Debug constructor
        public Gizmo(Vector2 side, bool fictiveDebug)
        {
            Side = side;
            
            if (side == new Vector2(0, -1))
            {
                Cursor = MouseCursor.SizeNS;
                Color = Color.Green;
            }
            
            if (side == new Vector2(0, 1))
            {
                Cursor = MouseCursor.SizeNS;
                Color = Color.Yellow;
            }
            
            if (side == new Vector2(1, 0))
            {
                Cursor = MouseCursor.SizeWE;
                Color = Color.Red;
            }
            
            if (side == new Vector2(-1, 0))
            {
                Cursor = MouseCursor.SizeWE;
                Color = Color.Blue;
            }

            if (side == new Vector2(-1,-1) || side == new Vector2(1,1))
            {
                Cursor = MouseCursor.SizeNWSE;
                Color = Color.White;
            }
            
            if (side == new Vector2(-1,1) || side == new Vector2(1,-1))
            {
                Cursor = MouseCursor.SizeNESW;
                Color = Color.White;
            }
        }
    }

    class Gizmos
    {
        public Gizmo Top         = new(Directions.Up);
        public Gizmo Bottom      = new(Directions.Down);
        public Gizmo Left        = new(Directions.Left);
        public Gizmo Right       = new(Directions.Right);
        public Gizmo TopLeft     = new(Directions.UpLeft);
        public Gizmo TopRight    = new(Directions.UpRight);
        public Gizmo BottomLeft  = new(Directions.DownLeft);
        public Gizmo BottomRight = new(Directions.DownRight);

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

    //Gizmos
    public void UpdateGizmos(Vector2 mousePos, Rectangle viewmapSelectedBox)
    {
        RectangleF box = viewmapSelectedBox;

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

        foreach (Gizmo gizmo in gizmos)
            if (gizmo.Rect.Contains(mousePos))
            {
                Mouse.SetCursor(gizmo.Cursor);
                return;
            }

        Mouse.SetCursor(MouseCursor.Arrow);
    }

    public bool CheckTouchingGizmos(Vector2 mousePos)
    {
        foreach (Gizmo gizmo in gizmos)
            if (gizmo.Rect.Contains(mousePos))
                grabedGizmo = gizmo;

        return grabedGizmo != null;
    }

    public Rectangle GizmosControls(Vector2 mousePos, Rectangle selectedTransform)
    {
        if (grabedGizmo == null) 
            throw new Exception("no grabed gizmo in GizmosControls");

        Vector2 side = grabedGizmo.Side;
        Point mouseTile = editor.GetMouseTile(mousePos);

        if (side.Y == 1)
        {
            selectedTransform.Height = mouseTile.Y - initialBox.Y + 1;
            
            if (selectedTransform.Height <= 0) selectedTransform.Height = 1;
        }
        
        if (side.Y == -1)
        {
            selectedTransform.Y = mouseTile.Y;
            
            int bottomSide = initialBox.Y + initialBox.Height;
            
            if (selectedTransform.Y >= bottomSide)
                selectedTransform.Y = bottomSide - 1;
            
            selectedTransform.Height = initialBox.Height + (initialBox.Y - selectedTransform.Y);
        }

        if (side.X == 1)
        {
            selectedTransform.Width = mouseTile.X - initialBox.X + 1;
            
            if (selectedTransform.Width <= 0) selectedTransform.Width = 1;
        }
        
        if (side.X == -1)
        {
            selectedTransform.X = mouseTile.X;

            int leftSide = initialBox.X + initialBox.Width;

            if (selectedTransform.X >= leftSide)
                selectedTransform.X = leftSide - 1;
            
            selectedTransform.Width = initialBox.Width + (initialBox.X - selectedTransform.X);
        }

        Mouse.SetCursor(grabedGizmo.Cursor);
        return selectedTransform;
    }

    public void DrawGizmos(SpriteBatch spriteBatch)
    {
        //Gizmos
        foreach (Gizmo gizmo in gizmos)
            spriteBatch.FillRectangle(gizmo.Rect, new Color(gizmo.Color, gizmoAlpha));
    }
}