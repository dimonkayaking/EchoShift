using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Bullet
{
    public Vector2 Position;
    private Vector2 _direction;
    private float _speed;
    private float _range;
    private float _distanceTraveled;
    public int Damage;
    public bool IsExpired { get; set; }

    public Bullet(Vector2 startPos, Vector2 direction, int damage, float range = 800f)
    {
        Position = startPos;
        _direction = direction;
        _speed = 800f;
        _range = range;
        Damage = damage;
        _distanceTraveled = 0;
        IsExpired = false;
    }

    public void Update(float deltaTime)
    {
        float step = _speed * deltaTime;
        Position += _direction * step;
        _distanceTraveled += step;
        if (_distanceTraveled >= _range) IsExpired = true;
        if (Position.X < -100 || Position.X > 1380 || Position.Y < -100 || Position.Y > 820)
            IsExpired = true;
    }

    public bool CollidesWith(Enemy enemy)
    {
        Rectangle bulletRect = new Rectangle((int)Position.X, (int)Position.Y, 8, 8);
        Rectangle enemyRect = new Rectangle((int)enemy.Position.X, (int)enemy.Position.Y, 32, 32);
        return bulletRect.Intersects(enemyRect);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(TextureManager.BulletAnim, Position, Color.White);
    }
}