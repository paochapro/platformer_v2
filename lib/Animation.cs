using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lib;

class Animation
{
    public Texture2D CurrentTexture => textureSheet[Frame];
    private Texture2D[] textureSheet;
    public int End => textureSheet.Length - 1;

    public bool HoldAnimation { get; set; }
    public bool Playing { get; private set; }
    public int Frame { get; private set; }
    public int Speed { get; set; }

    private float currentTime;
    private int direction;

    public Animation(Texture2D[] sheet, bool holdAnimation, int speed = 1)
    {
        HoldAnimation = holdAnimation;
        textureSheet = sheet;
        Playing = false;
        Speed = speed;
        Frame = 0;
    }
    public Animation(Animation other)
    {
        //Copying texture sheet
        textureSheet = new Texture2D[other.textureSheet.Length];
        Array.Copy(other.textureSheet, textureSheet, other.textureSheet.Length);

        HoldAnimation = other.HoldAnimation;
        Speed = other.Speed;
        Playing = false;
        Frame = 0;
    }
    
    public void PlayForward()
    {
        Playing = true;
        direction = 1;
    }
    public void PlayBackwards()
    {
        Playing = true;
        direction = -1;
    }
    public void Stop()
    {
        Playing = false;
        currentTime = 0;
        Frame = 0;
    }
    private void NextFrame()
    {
        Frame += direction;
        
        if (Frame > End)
        {
            Frame = HoldAnimation ? End : 0;
        }
        if (Frame < 0)
        {
            Frame = HoldAnimation ? 0 : End;
        }
    }
    public void Update(GameTime gameTime)
    {
        if (Playing)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            currentTime += dt;

            if (Playing && currentTime > (Speed * dt))
            {
                NextFrame();
                currentTime = 0;
            }
        }
    }
}