using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace PlatformerV2;

//Static
abstract partial class Entity
{
    public static MainGame Game { get; set; }
    private static List<Entity> ents = new();
    private static int updatePosition = 0;
    public static int Count => ents.Count;

    public static void UpdateAll(GameTime gameTime)
    {
        updatePosition = 0;
        
        while(updatePosition < ents.Count)
        {
            ents[updatePosition++].Update(gameTime);
        }
    }
    public static void DrawAll(SpriteBatch spriteBatch)
    {
        ents.ForEach(ent => ent.Draw(spriteBatch));
    }
    public static void AddEntity(Entity ent)
    {
        ents.Add(ent);
    }
    public static void RemoveEntity(Entity ent)
    {
        void removing()
        {
            int index = ents.IndexOf(ent);
        
            //Removing from entities
            //If entity was updated, update position should be lowered
            if (index <= updatePosition) --updatePosition;
            if(updatePosition < 0) updatePosition = 0;

            if (!ents.Remove(ent))
            {
                //throw new Exception("Entity wasnt found in ents when removing - Entity/RemoveEntity");
            }
        }

        ent.PreDestroy?.Invoke();
        removing();
        ent.PostDestroy?.Invoke();
    }
    
    public static void RemoveAll()
    {        
        while (ents.Count > 0)
        {
            RemoveEntity(ents.First());
        }
    }
}

//Main
abstract partial class Entity : ICloneable
{
    public object Clone() => MemberwiseClone();

    private bool destroyed = false;
    
    protected event Action PreDestroy;
    protected event Action PostDestroy;
    
    protected RectangleF hitbox;
    protected Texture2D? texture;
    
    public RectangleF Hitbox => hitbox;
    public Texture2D? Texture => texture;

    protected abstract void Update(GameTime gameTime);

    protected virtual void Draw(SpriteBatch spriteBatch)
    {
        if(texture == null) return;
        spriteBatch.Draw(texture, (Rectangle)hitbox, Color.White);
    }

    protected Entity(RectangleF hitbox, Texture2D? texture)
    {
        this.hitbox = hitbox;
        this.texture = texture;
    }

    public Entity() : this(new RectangleF(0, 0, 0, 0), null) { }

    public void Destroy()
    {
        if (destroyed) return;
        destroyed = true;

        RemoveEntity(this);
    }
}