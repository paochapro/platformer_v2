﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;

using Lib;
using PlatformerV2.Main;

namespace PlatformerV2.LevelEditor;

class TileHandler
{
    private Editor editor;
    private RoomHandler roomHandler;
    private Map.Tile currentTile;

    public TileHandler(Editor editor, RoomHandler roomHandler)
    {
        currentTile = Map.Tile.Wall;
        this.editor = editor;
        this.roomHandler = roomHandler;
    }

    public void Controls(Vector2 mousePos)
    {
        void place(Map.Tile tile)
        {
            Point tileWorldPos = editor.GetMouseTile(mousePos);

            foreach (Room room in roomHandler.Rooms)
            {
                if (room.Box.Contains(tileWorldPos))
                {
                    Point tileRoomPos = tileWorldPos - room.Box.Location;
                    room.SetTile(tileRoomPos.X, tileRoomPos.Y, tile);
                }
            }
        }
        
        if (Input.RBDown())
        {
            place(Map.Tile.None);
        }
        
        if (Input.LBDown())
        {
            place(currentTile);
        }
    }
}