using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class Enemy
{
    public Vector2 Position;
    public int Health;
    public int MaxHealth;
    public int Damage;
    public float Speed;
    public int EchoValue;
    public bool IsDead => Health <= 0;
    public bool IsMelee => _type == 1;
    public bool IsRanged => _type != 1;
    private Texture2D _texture;
    private int _type;
    private float _shootTimer;
    private float _shootCooldown;
    private float _shootRange;
    private int _shootDamage;

    public Enemy(Vector2 startPos, int type)
    {
        Position = startPos;
        _type = type;
        switch (type)
        {
            case 0:
                MaxHealth = 30;
                Health = MaxHealth;
                Damage = 10;
                Speed = 100f;
                EchoValue = 1;
                _texture = TextureManager.EnemyRed;
                _shootCooldown = 1.2f;
                _shootRange = 650f;
                _shootDamage = 8;
                break;
            case 1:
                MaxHealth = 20;
                Health = MaxHealth;
                Damage = 8;
                Speed = 250f;
                EchoValue = 1;
                _texture = TextureManager.EnemyOrange;
                _shootCooldown = 999f;
                _shootRange = 0f;
                _shootDamage = 0;
                break;
            case 2:
                MaxHealth = 100;
                Health = MaxHealth;
                Damage = 20;
                Speed = 50f;
                EchoValue = 3;
                _texture = TextureManager.EnemyPurple;
                _shootCooldown = 0.9f;
                _shootRange = 720f;
                _shootDamage = 12;
                break;
        }

        _shootTimer = (float)new Random().NextDouble() * _shootCooldown;
    }

    public void Update(float deltaTime, Vector2 playerPos, Vector2? decoyPos)
    {
        Vector2 target = decoyPos ?? playerPos;
        Vector2 direction = target - Position;
        if (direction != Vector2.Zero) direction.Normalize();
        Position += direction * Speed * deltaTime;

        if (IsRanged)
            _shootTimer -= deltaTime;
    }

    public bool TryShoot(Vector2 targetPos, out Vector2 direction, out int damage)
    {
        direction = Vector2.Zero;
        damage = 0;
        if (!IsRanged) return false;
        if (_shootTimer > 0) return false;

        Vector2 toTarget = targetPos - Position;
        float dist = toTarget.Length();
        if (dist <= 1f || dist > _shootRange) return false;
        direction = toTarget / dist;
        damage = _shootDamage;
        _shootTimer = _shootCooldown;
        return true;
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

        // HP-бар
        float hp01 = MaxHealth <= 0 ? 0f : MathHelper.Clamp((float)Health / MaxHealth, 0f, 1f);
        int barW = 32;
        int barH = 5;
        int x = (int)Position.X;
        int y = (int)Position.Y - 8;
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(x, y, barW, barH), new Color(0, 0, 0, 160));
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(x, y, (int)(barW * hp01), barH), Color.LimeGreen);
    }
}