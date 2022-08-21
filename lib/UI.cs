using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System.Text;

using static Lib.Utils;

namespace Lib;

//Static
abstract partial class UI
{
    private static List<UI> elements = new();
    private static bool clicking;
    public static bool Clicking => clicking;
    public static int CurrentLayer { get; set; }
    public static GameWindow window;
    public static SpriteFont Font { get; set; }
    
    //Main colors
    public static Color MainDefaultColor { get; set; } = Color.Black;
    public static Color MainSelectedColor { get; set; } = Color.Gold;
    public static Color MainLockedColor { get; set; } = Color.Gray;

    //Bg colors
    public static Color BgDefaultColor { get; set; } = Color.White;
    public static Color BgSelectedColor { get; set; } = new Color(Color.Yellow, 50);
    public static Color BgLockedColor { get; set; } = Color.DarkGray;
    
    public static void UpdateElements(KeyboardState keys, MouseState mouse)
    {
        //Mouse.SetCursor(MouseCursor.Arrow);

        foreach (UI element in elements)
            if (element.layer == CurrentLayer && !element.locked)
                element.Update(keys, mouse);

        clicking = (mouse.LeftButton == ButtonState.Pressed);
    }
    public static void DrawElements(SpriteBatch spriteBatch)
    {
        foreach (UI element in elements)
            if (element.layer == CurrentLayer && !element.Hidden)
                element.Draw(spriteBatch);
    }
    public static T Add<T>(T elem) where T : UI
    {
        elements.Add(elem);
        return elem;
    }
    public static void Clear()
    {
        CurrentLayer = 0;
        elements.Clear();
    }
}

//Element
abstract partial class UI
{
    public Rectangle rect = Rectangle.Empty;
    protected Color mainColor = Color.Purple;
    protected Color bgColor = Color.Purple;
    
    public Point Position { get => rect.Location; set => rect.Location = value; }
    public Point Size { get => rect.Size; set => rect.Size = value; }

    protected readonly int layer = 0;
    public string text;
    protected bool allowHold;
    private bool locked = false;
    public bool Locked
    {
        get => locked;
        set
        {
            locked = value;
            if (locked)
            {
                mainColor = MainLockedColor;
                bgColor = BgLockedColor;
            };
        }
    }
    public bool Hidden { get; set; }

    public abstract void Activate();

    protected virtual void Update(KeyboardState keys, MouseState mouse)
    {
        mainColor = MainDefaultColor;
        bgColor = BgDefaultColor;

        if (rect.Contains(mouse.Position) && !Clicking)
        {
            Mouse.SetCursor(MouseCursor.Hand);

            mainColor = MainSelectedColor;
            bgColor = BgSelectedColor;

            if (mouse.LeftButton == ButtonState.Pressed)
                Activate();
        }
    }

    protected abstract void Draw(SpriteBatch spriteBatch);
    
    protected UI(Rectangle rect, string text, int layer)
    {
        this.rect = rect;
        this.text = text;
        this.layer = layer;
    }
}

//Button
class Button : UI
{
    event Action func;

    public Button(Rectangle rect, Action func, string text, int layer)
        : base(rect, text, layer)
    {
        this.func = func;
        allowHold = false;
    }
    public override void Activate() => func.Invoke();

    protected override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.FillRectangle(rect, bgColor);
        spriteBatch.DrawRectangle(rect, mainColor, 4);
        
        float scale = 1f;
        
        Vector2 measure = Font.MeasureString(text) * scale;
        Vector2 position = new Vector2(rect.Center.X - measure.X / 2, rect.Center.Y - measure.Y / 2);
        
        spriteBatch.DrawString(Font, text, position, mainColor, 0, new Vector2(0, 0), scale, SpriteEffects.None, 0);
    }
}

class TextureButton : Button
{
    static int instance = 0;
    Texture2D texture;

    public TextureButton(Texture2D texture, Rectangle rect, Action func, int layer)
        : base(rect, func, "TEXTURE_BUTTON" + (instance++), layer)
    {
        this.texture = texture;
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(texture, rect, Color.White);
    }
}

class CheckBox : UI
{
    static readonly Point size = new Point(24, 24);
    const int textOffset = 5;

    bool isChecked = false;
    bool IsChecked => isChecked;
    event Action act1;
    event Action act2;

    public CheckBox(Point position, Action act1, Action act2, string text, int layer)
        : base( new Rectangle(position, size), text, layer)
    {
        this.act1 = act1;
        this.act2 = act2;
        allowHold = false;
        rect.Width = (int)Font.MeasureString(text).X;
    }
    public override void Activate()
    {
        isChecked = !isChecked;

        if (isChecked)
        {
            act1.Invoke();
        }
        else
        {
            act2.Invoke();
        }
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        Rectangle box = new(rect.Location, size);
        spriteBatch.FillRectangle(box, bgColor);
        spriteBatch.DrawRectangle(box, mainColor, 3);

        float scale = 0.7f;

        Vector2 measure = Font.MeasureString(text) * scale;
        Vector2 position = new Vector2(box.Right + textOffset, center(box.Y, box.Bottom, measure.Y));

        spriteBatch.DrawString(Font, text, position, mainColor, 0, new Vector2(0, 0), scale, SpriteEffects.None, 0);

        if(isChecked)
        {
            spriteBatch.DrawCircle(new CircleF(box.Center, 7f), 32, mainColor, 7);
        }
    }
}

class Slider : UI
{
    public const int sizeY = 50;
    const int sliderSizeX = 20;
    const int sliderOffset = 15;

    static readonly Point size = new(300 + sliderSizeX, sizeY);
    static readonly Point sliderSize = new(sliderSizeX, size.Y);
    static readonly int barSizeY = 10;

    int min, max;
    int sliderX;
    event Action<int> func;

    Point textSize;
    Point textPos;

    public Slider(Point pos, string text, Action<int> func, int defaultValue, int layer)
        : base(new Rectangle(pos, size), text, layer)
    {
        textSize = Font.MeasureString(text).ToPoint();
        textPos = new Point(pos.X, center(pos.Y, pos.Y + size.Y, textSize.Y) - 3);

        int offset = textSize.X + sliderOffset;

        rect.X += offset;
        min = rect.X;
        max = rect.X + size.X - sliderSize.X;
        sliderX = rect.X + defaultValue;
        allowHold = true;

        this.func = func;
    }

    public override void Activate()
    {
        sliderX = Input.Mouse.Position.X;
        sliderX = clamp(sliderX, min, max);

        //Getting range from 0 to 100
        int denominator = (size.X - sliderSizeX) / 100;
        float value = (float)(sliderX - rect.X) / denominator;

        func.Invoke((int)Math.Round(value));
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.DrawString(Font, text, textPos.ToVector2(), Color.Black);

        Rectangle bar = new Rectangle(rect.X, center(rect.Y, rect.Y + size.Y, barSizeY), size.X, barSizeY);
        spriteBatch.FillRectangle(bar, Color.Gray);

        Rectangle slider = new Rectangle(sliderX, rect.Y, sliderSize.X, sliderSize.Y);
        spriteBatch.FillRectangle(slider, Color.White);
        spriteBatch.DrawRectangle(slider, Color.Black, 3);
    }
}

class Label : UI
{
    public Color Color { get; set; }

    public Label(Point pos, string text, Color color, int layer)
        : base(new Rectangle(pos, Point.Zero), text, layer)
    {
        Color = color;
    }
    
    protected override void Update(KeyboardState keys, MouseState mouse) {}

    protected override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.DrawString(Font, text, rect.Location.ToVector2(), Color);
    }

    public override void Activate() {}
}

class TextBox : UI
{
    private const string avaliableCharaters = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890-=!@#$%^&*()_+[]{};':|\\\",./<>?`~ ";

    public string Text => writtenText.ToString();
    StringBuilder writtenText = new();

    bool focus = false;

    public TextBox(Rectangle rect, string text, int layer)
        : base(rect, text, layer)
    {
        mainColor = Color.Black;
        bgColor = Color.White;
    }

    public override void Activate()
    {
        if (focus) return;

        window.TextInput += TextInput;
        focus = true;        
    }

    public void Deactivate()
    {
        window.TextInput -= TextInput;
        focus = false;
    }


    private void TextInput(object? sender, TextInputEventArgs e)
    {
        if (e.Character == '\b')
        {
            if (writtenText.Length > 0)
                writtenText.Remove(writtenText.Length - 1, 1);
        }

        if(avaliableCharaters.Contains(e.Character))
            writtenText.Append(e.Character);
    }

    protected override void Update(KeyboardState keys, MouseState mouse)
    {
        if (rect.Contains(mouse.Position))
        {
            Mouse.SetCursor(MouseCursor.IBeam);

            if (mouse.LeftButton == ButtonState.Pressed && !Clicking)
            {
                Activate();
                return;
            }
        }
        
        bool unfocusInput = (
            keys.IsKeyDown(Keys.Escape) ||
            (mouse.LeftButton == ButtonState.Pressed && !Clicking)
        ); 
        
        if (focus && unfocusInput)
            Deactivate();
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.FillRectangle(rect, bgColor);

        if(writtenText.Length == 0 && !focus)
            spriteBatch.DrawString(Font, text, rect.Location.ToVector2() + new Vector2(10, 10), Color.Gray);
        else
            spriteBatch.DrawString(Font, writtenText, rect.Location.ToVector2() + new Vector2(10, 10), mainColor);

        spriteBatch.DrawRectangle(rect, focus ? Color.Blue : mainColor, 3);
    }
}

class Image : UI
{
    public Texture2D Texture { get; private set; }
    public Angle Rotation { get; set; }
    
    public Image(Texture2D image, Rectangle box, int layer)
        : base(box, "", layer)
    {
        Texture = image;
    }
    
    public Image(Texture2D image, Point position, int layer)
        : this(image, new Rectangle(position, image.Bounds.Size), layer)
    {
    }
    
    protected override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(Texture, new Rectangle(rect.Center, rect.Size), null, Color.White, -Rotation.Radians, Texture.Bounds.Size.ToVector2() / 2, SpriteEffects.None, 0);
    }

    protected override void Update(KeyboardState keys, MouseState mouse) {}

    public override void Activate() {}
}

//Containers
class Container : UI
{
    private List<UI> elements = new();
    public int ElementOffset { get; set; }

    public Container(Rectangle box, int elementOffset, int layer)
        : base(box, "", layer)
    {
        ElementOffset = elementOffset;
    }
    
    public void Add(UI element)
    {
        elements.Add(element);
        Rearrange();
    }
    public void Remove(UI element)
    {
        elements.Remove(element);
        Rearrange();
    }

    private void Rearrange()
    {
        UI? previousElement = null;
        
        foreach (UI element in elements)
        {
            element.rect.X = (previousElement?.rect.Right + ElementOffset ?? rect.X);
            element.rect.Y = rect.Y;
            previousElement = element;
        }
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        throw new NotImplementedException();
    }

    protected override void Update(KeyboardState keys, MouseState mouse) {}

    public override void Activate() {}
}