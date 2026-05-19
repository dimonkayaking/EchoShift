using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class EnemyBullet
{
    public Vector2 Position;
    private Vector2 _direction;
    private float _speed;
    private float _range;
    private float _distanceTraveled;
    public int Damage;
    public bool IsExpired { get; set; }

    public static int ScreenWidth = 1280;
    public static int ScreenHeight = 720;

    public EnemyBullet(Vector2 startPos, Vector2 direction, int damage, float range = 900f)
    {
        Position = startPos;
        _direction = direction == Vector2.Zero ? Vector2.UnitX : Vector2.Normalize(direction);
        _speed = 520f;
        _range = range;
        Damage = damage;
        _distanceTraveled = 0;
        IsExpired = false;
    }

    public void Update(float deltaTime)
    {
        var step = _speed * deltaTime;
        Position += _direction * step;
        _distanceTraveled += step;
        if (_distanceTraveled >= _range) IsExpired = true;
        if (Position.X < -120 || Position.X > ScreenWidth + 120 || Position.Y < -120 || Position.Y > ScreenHeight + 120)
            IsExpired = true;
    }

    public bool CollidesWith(Player player)
    {
        var bulletRect = new Rectangle((int)Position.X, (int)Position.Y, 8, 8);
        var playerRect = new Rectangle((int)player.Position.X, (int)player.Position.Y, 32, 32);
        return bulletRect.Intersects(playerRect);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle((int)Position.X, (int)Position.Y, 6, 6), Color.OrangeRed);
    }
}