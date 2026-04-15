using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class EchoDecoy
{
    public Vector2 Position;
    private float _lifeTime;
    private float _timer;
    public bool IsExploded { get; private set; }
    private Texture2D _texture;

    public EchoDecoy(Vector2 pos, Texture2D texture, float duration = 2f)
    {
        Position = pos;
        _texture = texture;
        _lifeTime = duration;
        _timer = 0;
        IsExploded = false;
    }

    public void Update(float deltaTime)
    {
        _timer += deltaTime;
        if (_timer >= _lifeTime) IsExploded = true;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture, Position, new Color(0, 255, 255, 128));
    }
}