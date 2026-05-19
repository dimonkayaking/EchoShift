using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class UI
{
    private SpriteFont _font;

    public UI() { }

    public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
    {
        try { _font = content.Load<SpriteFont>("default"); }
        catch { _font = null; }
    }

    public void Draw(SpriteBatch spriteBatch, int kills, int echoPoints, int echoProgress, int teleportsAvailable, int health, int maxHealth, int nextUpgradeKills)
    {
        // Шкала здоровья
        Rectangle healthBg = new Rectangle(10, 10, 200, 20);
        Rectangle healthRect = new Rectangle(10, 10, (int)(200 * ((float)health / maxHealth)), 20);
        spriteBatch.Draw(TextureManager.Pixel, healthBg, new Color(0, 0, 0, 160));
        spriteBatch.Draw(TextureManager.Pixel, healthRect, Color.Red);
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(10, 10, 200, 2), Color.White);
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(10, 28, 200, 2), Color.White);
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(10, 10, 2, 20), Color.White);
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(208, 10, 2, 20), Color.White);

        int pct = maxHealth <= 0 ? 0 : (int)Math.Round(100.0 * health / maxHealth);
        string pctText = $"{pct}%";
        Vector2 size = TinyFont.Measure(pctText, 2);
        Vector2 pos = new Vector2(10 + (200 - size.X) / 2f, 10 + (20 - size.Y) / 2f);
        TinyFont.Draw(spriteBatch, pctText, pos + new Vector2(1, 1), new Color(0, 0, 0, 220), 2);
        TinyFont.Draw(spriteBatch, pctText, pos, Color.White, 2);

        // Шкала эха
        Rectangle echoBg = new Rectangle(10, 40, 200, 20);
        Rectangle echoRect = new Rectangle(10, 40, (int)(200 * ((float)echoProgress / 20)), 20);
        spriteBatch.Draw(TextureManager.Pixel, echoBg, new Color(0, 0, 0, 160));
        spriteBatch.Draw(TextureManager.Pixel, echoRect, Color.Cyan);
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(10, 40, 200, 2), Color.White);
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(10, 58, 200, 2), Color.White);
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(10, 40, 2, 20), Color.White);
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(208, 40, 2, 20), Color.White);

        string echoText = $"ЭХО: {echoPoints}  (ТЕЛ. x{teleportsAvailable})";
        TinyFont.Draw(spriteBatch, echoText, new Vector2(10, 65), Color.Cyan, 2);
    }

    public SpriteFont GetFont() => _font;
}