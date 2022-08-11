using Microsoft.Xna.Framework.Graphics;

using static Lib.Utils;

namespace Lib;

static class Assets
{
    static public Microsoft.Xna.Framework.Content.ContentManager Content;

    static private void ContentAvaliable()
    {
        if (Content == null)
            throw new Exception("content is null in MonoGame");
    }

    static public T? Load<T>(string asset)
    {
        ContentAvaliable();

        if (!AssetExists(asset))
        {
            print("No asset \"" + asset + "\" was found in MonoGame:Load");
            return default(T);
        }
        return Content.Load<T>(asset);
    }
    static public Texture2D LoadTexture(string asset)
    {
        ContentAvaliable();

        if (!AssetExists(asset))
        {
            print("No texture \"" + asset + "\" was found in MonoGame:Load");
            asset = "error";
        }

        return Content.Load<Texture2D>(asset);
    }
    static private bool AssetExists(string asset)
    {
        ContentAvaliable();
        return File.Exists(Content.RootDirectory + @"\" + asset + ".xnb");
    }
}
