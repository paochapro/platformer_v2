using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System.Reflection;

namespace PlatformerV2;

//Static
abstract partial class Entity
{
    public static MainGame Game { get; set; }
    private static List<Entity> ents = new();
    private static int updatePosition = 0;

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
        ent.index = ents.Count;
        ents.Add(ent);
    }
    public static void RemoveEntity(Entity ent)
    {
        ent.PreDestroy?.Invoke();

        int index = ent.index;

        //Removing from entities
        //If entity was updated, update position should be lowered
        if (index <= updatePosition) --updatePosition;

        try
        {
            ents.RemoveAt(index);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine("AOOE Exception in Entity/RemoveEntity: " + ex.Message);
            Console.WriteLine("{");
            Console.WriteLine("\t Index Var: " + index);
            Console.WriteLine("\t Index Entity: " + ent.index);
            Console.WriteLine("\t Ents Count: " + ents.Count);
            Console.WriteLine("}");
        }
        
        //Updating indexing
        for (int i = index; i < ents.Count; ++i)
            --(ents[i].index);
        
        ent.PostDestroy?.Invoke();
    }
    
    public static void RemoveAll()
    {
        foreach (GroupBase group in GroupBase.AllGroups)
            group.Clear();
        
        updatePosition = 0;

        while (ents.Count > 0)
        {
            Entity ent = ents.First();
            ent.PreDestroy?.Invoke();
            RemoveEntity(ent);
            ent.PostDestroy?.Invoke();
        }
    }
}

//Main
abstract partial class Entity
{
    private bool destroyed = false;
    private int index;
    
    public Dictionary<GroupBase, int> group_index { get; private set; } = new();

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
        
        //Removing entity from all groups that its belongs to
        foreach (var kv in group_index)
            kv.Key.Remove(this);
        
        RemoveEntity(this);
    }
}

//Group
abstract class GroupBase
{
    private static List<GroupBase> groups = new();
    public static IEnumerable<GroupBase> AllGroups => groups; 

    protected int groupIndex { get; private set; }
    
    public GroupBase()
    {
        groupIndex = groups.Count;
        groups.Add(this);    
    }

    public abstract void Remove(Entity item);
    public abstract void Clear(bool destroy = true);
}

class Group<T> : GroupBase where T : Entity
{
    private int iteratePosition = 0;
    
    public T this[int i] => list[i];
    
    private List<T> list = new();
    public IEnumerable<T> All => list;
    public int Count => list.Count;
    
    public void Iterate(Action<T> func)
    {
        iteratePosition = 0;
        
        while (iteratePosition < Count)
        {
            T item = this[iteratePosition++];
            func.Invoke(item);
        }
    }
    public void Add(T item, bool addToEntities = true)
    {
        int index = Count;
        
        item.group_index.Add(this, index); //Adding group index to item
        list.Add(item);                    //Adding to the list
        
        //Adding to the entities
        if(addToEntities) Entity.AddEntity(item);
    }
    public override void Remove(Entity item)
    {
        int index = item.group_index[this];
        
        //If item was updated, iteration position should be lowered
        if (index <= iteratePosition) 
            --iteratePosition;
        
        try
        {
            //Removing group from item, and removing item from group
            list.RemoveAt(index);
            item.group_index.Remove(this);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine("AOOE Exception in Group/Remove: " + ex.Message);
            Console.WriteLine("{");
            Console.WriteLine("\t Group: " + this);
            Console.WriteLine("\t Index var: " + index);
            Console.WriteLine("\t Index group: " + item.group_index[this]);
            Console.WriteLine("\t List count: " + Count);
            Console.WriteLine("}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Other Exception: " + ex.Message);
        }
        
        //Updating indexing
        for (int i = index; i < Count; ++i)
            --(list[i].group_index[this]);
    }
    public override void Clear(bool destroy = true)
    {
        if (destroy)
            list.ForEach(ent => Entity.RemoveEntity(ent)); //Destroying all members
        else 
            list.ForEach(ent => ent.group_index.Remove(this)); //Removing group from all members
        
        list.Clear(); //Clear the list
    }
    
    public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
    public override string ToString() => $"group{groupIndex}-{typeof(T).GetTypeInfo().Name}";
}