using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace PlatformerV2;

abstract class Interactable : Entity
{
    private static int updatePosition = 0;
    private static List<Interactable> interactables = new();
    public new static int Count => interactables.Count;
    
    public static void Iterate(Action<Interactable> func)
    {
        updatePosition = 0;
        
        while(updatePosition < interactables.Count)
        {
            func.Invoke(interactables[updatePosition++]);
        }
    }

    private void OnDestroy()
    {
        if(interactables.IndexOf(this) <= updatePosition) --updatePosition;
        if(updatePosition < 0) updatePosition = 0;
        interactables.Remove(this);
    }

    public Interactable(RectangleF rect, Texture2D texture)
        : base(rect, texture)
    {
        PreDestroy += OnDestroy;
        interactables.Add(this);
    }

    public virtual void TouchesPlayer(Player player)
    {
        if(hitbox.Intersects(player.Hitbox))
            OnPlayerTouch(player);
        else
            OnNotTouching(player);
    }

    protected abstract void OnPlayerTouch(Player player);
    protected virtual void OnNotTouching(Player player) {}
}