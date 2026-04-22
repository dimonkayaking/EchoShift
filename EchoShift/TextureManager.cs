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
        // Создаём все текстуры программно (чтобы не зависеть от файлов)
        Pixel = CreateSolidTexture(1, 1, Color.White);
        PlayerIdle = CreateSolidTexture(32, 32, Color.LimeGreen);
        PlayerWalk = CreateStripTexture(32, 32, 5, 
            new Color[] { Color.LimeGreen, Color.Green, Color.ForestGreen, Color.Green, Color.LimeGreen });
        PlayerShoot = CreateStripTexture(32, 32, 4,
            new Color[] { Color.LimeGreen, Color.OrangeRed, Color.LimeGreen, Color.OrangeRed });
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
        Texture2D tex = new Texture2D(GameServices.GraphicsDevice, width, height);
        Color[] data = new Color[width * height];
        for (int i = 0; i < data.Length; i++) data[i] = color;
        tex.SetData(data);
        return tex;
    }

    private static Texture2D CreateStripTexture(int frameWidth, int frameHeight, int framesCount, Color[] frameColors)
    {
        int width = frameWidth * framesCount;
        int height = frameHeight;
        Texture2D tex = new Texture2D(GameServices.GraphicsDevice, width, height);
        Color[] data = new Color[width * height];
        for (int frame = 0; frame < framesCount; frame++)
        {
            Color col = frameColors[frame % frameColors.Length];
            for (int y = 0; y < frameHeight; y++)
                for (int x = 0; x < frameWidth; x++)
                    data[y * width + frame * frameWidth + x] = col;
        }
        tex.SetData(data);
        return tex;
    }

    private static Texture2D CreateBackgroundTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(GameServices.GraphicsDevice, width, height);
        Color[] data = new Color[width * height];

        Color baseCol = new Color(20, 28, 32);
        Color gridCol = new Color(28, 42, 48);
        Color speckCol = new Color(40, 70, 75);

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / (height - 1);
            byte add = (byte)(18 * v);
            Color rowBase = new Color(
                (byte)MathHelper.Clamp(baseCol.R + add, 0, 255),
                (byte)MathHelper.Clamp(baseCol.G + add, 0, 255),
                (byte)MathHelper.Clamp(baseCol.B + add, 0, 255)
            );

            for (int x = 0; x < width; x++)
            {
                bool grid = (x % 64 == 0) || (y % 64 == 0) || (x % 16 == 0 && y % 16 == 0);
                bool speck = ((x * 73856093) ^ (y * 19349663)) % 97 == 0;
                Color c = rowBase;
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