using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace PlatformerV2;
using static Utils;

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
        if (!shot) return false;
        shot = false;
        return true;
    }
}