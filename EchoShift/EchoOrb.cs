using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class EchoOrb
{
    public Vector2 Position;
    public int Value;
    public bool Collected { get; private set; }
    private float _magnetSpeed = 300f;

    public EchoOrb(Vector2 pos, int value)
    {
        Position = pos;
        Value = value;
        Collected = false;
    }

    public void Update(float deltaTime, Vector2 playerPos)
    {
        Vector2 dir = playerPos - Position;
        if (dir.Length() < 5f)
        {
            Collected = true;
            return;
        }
        if (dir != Vector2.Zero) dir.Normalize();
        Position += dir * _magnetSpeed * deltaTime;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(TextureManager.EchoOrbTex, Position, Color.White);
    }
}