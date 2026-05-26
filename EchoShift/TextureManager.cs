using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

public static class TextureManager
{
    public static Texture2D Pixel { get; private set; }
    public static Texture2D Player { get; private set; }
    public static Texture2D EnemyRed { get; private set; }
    public static Texture2D EnemyOrange { get; private set; }
    public static Texture2D EnemyPurple { get; private set; }
    public static Texture2D BackgroundDetailed { get; private set; }
    public static Texture2D BulletAnim { get; private set; }
    public static Texture2D EchoOrbTex { get; private set; }
    public static Texture2D DecoyTex { get; private set; }

    public static void Load(ContentManager content)
    {
        Pixel = CreateSolidTexture(1, 1, Color.White);
        
        Player = content.Load<Texture2D>("player");
        EnemyRed = content.Load<Texture2D>("red_enemy");
        EnemyOrange = content.Load<Texture2D>("orange_enemy");
        EnemyPurple = content.Load<Texture2D>("purple_enemy");
        
        BackgroundDetailed = CreateBackgroundTexture(1280, 720);
        BulletAnim = CreateSolidTexture(8, 8, Color.Yellow);
        EchoOrbTex = CreateSolidTexture(16, 16, Color.Cyan);
        DecoyTex = Player;
    }

    private static Texture2D CreateSolidTexture(int width, int height, Color color)
    {
        var tex = new Texture2D(GameServices.GraphicsDevice, width, height);
        var data = new Color[width * height];
        for (var i = 0; i < data.Length; i++) data[i] = color;
        tex.SetData(data);
        return tex;
    }

    private static Texture2D CreateBackgroundTexture(int width, int height)
    {
        var tex = new Texture2D(GameServices.GraphicsDevice, width, height);
        var data = new Color[width * height];

        var baseCol = new Color(20, 28, 32);
        var gridCol = new Color(28, 42, 48);
        var speckCol = new Color(40, 70, 75);

        for (var y = 0; y < height; y++)
        {
            var v = (float)y / (height - 1);
            var add = (byte)(18 * v);
            var rowBase = new Color(
                (byte)MathHelper.Clamp(baseCol.R + add, 0, 255),
                (byte)MathHelper.Clamp(baseCol.G + add, 0, 255),
                (byte)MathHelper.Clamp(baseCol.B + add, 0, 255)
            );

            for (var x = 0; x < width; x++)
            {
                var grid = (x % 64 == 0) || (y % 64 == 0) || (x % 16 == 0 && y % 16 == 0);
                var speck = ((x * 73856093) ^ (y * 19349663)) % 97 == 0;
                var c = rowBase;
                if (grid) c = Color.Lerp(c, gridCol, 0.25f);
                if (speck) c = Color.Lerp(c, speckCol, 0.35f);
                data[y * width + x] = c;
            }
        }

        tex.SetData(data);
        return tex;
    }
}

public static class GameServices
{
    public static GraphicsDevice GraphicsDevice { get; set; }
}