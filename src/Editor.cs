using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;
using MonoGame.Extended.Screens;

namespace PlatformerV2;

class Editor : GameScreen
{
    public new MainGame Game => base.Game as MainGame;
    public Editor(MainGame game) : base(game) {}

    public override void Update(GameTime gameTime)
    {
        Console.WriteLine("Updating editor!");
        //throw new NotImplementedException();
    }

    public override void Draw(GameTime gameTime)
    {
        Game.SpriteBatch.FillRectangle(new RectangleF(0,0, 64,64), Color.Green);
        //throw new NotImplementedException();
    }
}