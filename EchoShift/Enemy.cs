using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class Enemy
{
    public Vector2 Position;
    public int Health;
    public int Damage;
    public float Speed;
    public int EchoValue;
    public bool IsDead => Health <= 0;
    private Texture2D _texture;
    private int _type;

    public Enemy(Vector2 startPos, int type)
    {
        Position = startPos;
        _type = type;
        switch (type)
        {
            case 0:
                Health = 30;
                Damage = 10;
                Speed = 100f;
                EchoValue = 1;
                _texture = TextureManager.EnemyRed;
                break;
            case 1:
                Health = 20;
                Damage = 8;
                Speed = 250f;
                EchoValue = 1;
                _texture = TextureManager.EnemyOrange;
                break;
            case 2:
                Health = 100;
                Damage = 20;
                Speed = 50f;
                EchoValue = 3;
                _texture = TextureManager.EnemyPurple;
                break;
        }
    }

    public void Update(float deltaTime, Vector2 playerPos, Vector2? decoyPos)
    {
        Vector2 target = decoyPos ?? playerPos;
        Vector2 direction = target - Position;
        if (direction != Vector2.Zero) direction.Normalize();
        Position += direction * Speed * deltaTime;
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
    }

    public bool CollidesWith(Player player)
    {
        Rectangle enemyRect = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
        Rectangle playerRect = new Rectangle((int)player.Position.X, (int)player.Position.Y, 32, 32);
        return enemyRect.Intersects(playerRect);
    }

    public void OnHitPlayer() { }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture, Position, Color.White);
    }
}