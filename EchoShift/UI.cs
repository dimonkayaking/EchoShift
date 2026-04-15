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

    public void Draw(SpriteBatch spriteBatch, int kills, int echoCharge, int echoCharges, int health, int maxHealth, int nextUpgradeKills)
    {
        // Текст (только если шрифт загружен)
        if (_font != null)
        {
            spriteBatch.DrawString(_font, $"Kills: {kills}", new Vector2(10, 10), Color.White);
            for (int i = 0; i < 5; i++)
            {
                Color color = i < echoCharges ? Color.Cyan : Color.Gray;
                spriteBatch.DrawString(_font, "●", new Vector2(200 + i * 20, 10), color);
            }
            int needed = nextUpgradeKills - kills;
            if (needed > 0)
                spriteBatch.DrawString(_font, $"Next upgrade in {needed} kills", new Vector2(10, 120), Color.Yellow);
        }

        // Полоски здоровья и эхо (всегда рисуются)
        Rectangle healthRect = new Rectangle(10, 50, (int)(200 * ((float)health / maxHealth)), 20);
        spriteBatch.Draw(TextureManager.EnemyRed, healthRect, Color.Red);
        Rectangle echoRect = new Rectangle(10, 80, (int)(200 * ((float)echoCharge / 20)), 20);
        spriteBatch.Draw(TextureManager.EchoOrbTex, echoRect, Color.Cyan);
    }

    public SpriteFont GetFont() => _font;
}