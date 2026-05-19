using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

public static class TextureManager
{
    public static Texture2D Pixel { get; private set; }
    public static Texture2D PlayerIdle { get; private set; }
    public static Texture2D PlayerWalk { get; private set; }
    public static Texture2D PlayerShoot { get; private set; }
    public static Texture2D BackgroundDetailed { get; private set; }
    public static Texture2D BulletAnim { get; private set; }
    public static Texture2D EnemyRed { get; private set; }
    public static Texture2D EnemyOrange { get; private set; }
    public static Texture2D EnemyPurple { get; private set; }
    public static Texture2D EchoOrbTex { get; private set; }
    public static Texture2D DecoyTex { get; private set; }

    public static void Load(ContentManager content)
    {
        Pixel = CreateSolidTexture(1, 1, Color.White);
        PlayerIdle = CreateSolidTexture(32, 32, Color.LimeGreen);
        PlayerWalk = CreateStripTexture(32, 32, 5, new Color[] { Color.LimeGreen, Color.Green, Color.ForestGreen, Color.Green, Color.LimeGreen });
        PlayerShoot = CreateStripTexture(32, 32, 4, new Color[] { Color.LimeGreen, Color.OrangeRed, Color.LimeGreen, Color.OrangeRed });
        BackgroundDetailed = CreateBackgroundTexture(1280, 720);
        BulletAnim = CreateSolidTexture(8, 8, Color.Yellow);
        EnemyRed = CreateSolidTexture(32, 32, Color.Red);
        EnemyOrange = CreateSolidTexture(32, 32, Color.Orange);
        EnemyPurple = CreateSolidTexture(32, 32, Color.Purple);
        EchoOrbTex = CreateSolidTexture(16, 16, Color.Cyan);
        DecoyTex = PlayerIdle;
    }

    private static Texture2D CreateSolidTexture(int width, int height, Color color)
    {
        var tex = new Texture2D(GameServices.GraphicsDevice, width, height);
        var data = new Color[width * height];
        for (var i = 0; i < data.Length; i++) data[i] = color;
        tex.SetData(data);
        return tex;
    }

    private static Texture2D CreateStripTexture(int frameWidth, int frameHeight, int framesCount, Color[] frameColors)
    {
        var width = frameWidth * framesCount;
        var height = frameHeight;
        var tex = new Texture2D(GameServices.GraphicsDevice, width, height);
        var data = new Color[width * height];
        for (var frame = 0; frame < framesCount; frame++)
        {
            var col = frameColors[frame % frameColors.Length];
            for (var y = 0; y < frameHeight; y++)
                for (var x = 0; x < frameWidth; x++)
                    data[y * width + frame * frameWidth + x] = col;
        }
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