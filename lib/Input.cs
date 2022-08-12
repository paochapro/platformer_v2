using Microsoft.Xna.Framework.Input;

namespace Lib;

static class Input
{
    public static MouseState Mouse => Microsoft.Xna.Framework.Input.Mouse.GetState();
    public static KeyboardState Keys => Keyboard.GetState();
    public static KeyboardState PreviousKeys { get; private set; }
    public static MouseState PreviousMouse { get; private set; }

    public static void CycleEnd()
    {
        PreviousKeys = Keys;
        PreviousMouse = Mouse;
    }

    //Mouse
    public static bool LBPressed() => Mouse.LeftButton == ButtonState.Pressed && PreviousMouse.LeftButton != ButtonState.Pressed;
    public static bool RBPressed() => Mouse.RightButton == ButtonState.Pressed && PreviousMouse.RightButton != ButtonState.Pressed;
    public static bool LBReleased() => Mouse.LeftButton != ButtonState.Pressed && PreviousMouse.LeftButton == ButtonState.Pressed;
    public static bool RBReleased() => Mouse.RightButton != ButtonState.Pressed && PreviousMouse.RightButton == ButtonState.Pressed;
    public static bool LBDown() => Mouse.LeftButton == ButtonState.Pressed;
    public static bool RBDown() => Mouse.RightButton == ButtonState.Pressed;
    public static bool LBUp() => Mouse.LeftButton != ButtonState.Pressed;
    public static bool RBUp() => Mouse.RightButton != ButtonState.Pressed;
    
    //Keys
    public static bool KeyPressed(Keys key) => Keys.IsKeyDown(key) && !PreviousKeys.IsKeyDown(key);
    public static bool KeyReleased(Keys key) => !Keys.IsKeyDown(key) && PreviousKeys.IsKeyDown(key);
    public static bool IsKeyDown(Keys key) => Keys.IsKeyDown(key);
    public static bool IsKeyUp(Keys key) => Keys.IsKeyUp(key);
}