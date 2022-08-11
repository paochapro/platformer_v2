using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace PlatformerV2;
using static Utils;

class Spike : Interactable
{
    private static readonly Size2 size = new Point(Map.TileUnit, 4);

    public Spike(Point2 pos)
        : base(new RectangleF(Point2.Zero, size), null)
    {
        float floor = pos.Y + Map.TileUnit;
        hitbox.Y = floor - size.Height;
        hitbox.X = pos.X;
    }

    protected override void Update(GameTime gameTime)
    {
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.FillRectangle(hitbox, Color.Red);
    }

    protected override void OnPlayerTouch(Player player) => player.SpikeBehavior();
}

partial class Player : Entity
{
    public void SpikeBehavior() => Death();
}