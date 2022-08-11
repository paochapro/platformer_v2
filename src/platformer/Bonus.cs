using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

using Lib;

namespace PlatformerV2.Main;
using static Lib.Utils;

class Bonus : Interactable
{
    private static readonly Size2 size = new(24,24);

    public Bonus(Point2 pos)
        : base(new RectangleF(Point.Zero, size), null)
    {
        hitbox.Position = pos + (new Point(Map.TileUnit) - size) / 2;
    }

    protected override void Update(GameTime gameTime)
    {
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.FillRectangle(hitbox, Color.Blue);
    }

    protected override void OnPlayerTouch(Player player)
    {
        if(player.BonusBehavior())
            Destroy();
    }
}

partial class Player : Entity
{
    public bool BonusBehavior()
    {
        if (bullet) return false;
        bullet = true;
        return true;
    }
}