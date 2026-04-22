using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class UI
{
    private SpriteFont _font;

    public UI() { }

    public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
    {
        try
        {
            _font = content.Load<SpriteFont>("default");
        }
        catch
        {
            _font = null; // если шрифт не найден, текст не отображаем
        }
    }

    public void Draw(SpriteBatch spriteBatch, int kills, int echoPoints, int echoProgress, int teleportsAvailable, int health, int maxHealth, int nextUpgradeKills)
    {
        // Текст (только если шрифт загружен)
        if (_font != null)
        {
            spriteBatch.DrawString(_font, $"Kills: {kills}", new Vector2(10, 10), Color.White);
            spriteBatch.DrawString(_font, $"Echo: {echoPoints}  (TP x{teleportsAvailable})", new Vector2(10, 30), Color.Cyan);
            int needed = nextUpgradeKills - kills;
            if (needed > 0)
                spriteBatch.DrawString(_font, $"Next upgrade in {needed} kills", new Vector2(10, 120), Color.Yellow);

            spriteBatch.DrawString(_font, "Shift: teleport  |  Space: heal  |  Tab: roulette  |  Esc: menu", new Vector2(10, 145), Color.LightGray);
        }

        // Полоски здоровья и эхо (всегда рисуются)
        Rectangle healthRect = new Rectangle(10, 50, (int)(200 * ((float)health / maxHealth)), 20);
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(10, 50, 200, 20), new Color(0, 0, 0, 160));
        spriteBatch.Draw(TextureManager.Pixel, healthRect, Color.Red);

        // % здоровья поверх шкалы (работает даже без SpriteFont)
        int pct = maxHealth <= 0 ? 0 : (int)Math.Round(100.0 * health / maxHealth);
        string pctText = $"{pct}%";
        int scale = 2;
        Vector2 size = TinyFont.Measure(pctText, scale);
        Vector2 pos = new Vector2(10 + (200 - size.X) / 2f, 50 + (20 - size.Y) / 2f);
        TinyFont.Draw(spriteBatch, pctText, pos + new Vector2(1, 1), new Color(0, 0, 0, 220), scale);
        TinyFont.Draw(spriteBatch, pctText, pos, Color.White, scale);

        Rectangle echoRect = new Rectangle(10, 80, (int)(200 * ((float)echoProgress / 20)), 20);
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(10, 80, 200, 20), new Color(0, 0, 0, 160));
        spriteBatch.Draw(TextureManager.Pixel, echoRect, Color.Cyan);
    }

    public SpriteFont GetFont() => _font;
}