using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class Enemy
{
    public Vector2 Position;
    public int Health;
    public int MaxHealth;
    public int Shield;
    public int MaxShield;
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
    private int _currentWave;
    private SpriteEffects _spriteEffect = SpriteEffects.None;

    public Enemy(Vector2 startPos, int type, int currentWave = 1)
    {
        Position = startPos;
        _type = type;
        _currentWave = currentWave;
        
        switch (type)
        {
            case 0:
                MaxHealth = 30;
                Health = MaxHealth;
                Damage = 10;
                Speed = 100f;
                EchoValue = 0;
                _texture = TextureManager.EnemyRed;
                _shootCooldown = 1.2f;
                _shootRange = 650f;
                _shootDamage = 8;
                break;
            case 1:
                MaxHealth = 20;
                Health = MaxHealth;
                Damage = 8;
                Speed = 350f;
                EchoValue = 0;
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
                EchoValue = 1;
                _texture = TextureManager.EnemyPurple;
                _shootCooldown = 0.9f;
                _shootRange = 720f;
                _shootDamage = 12;
                break;
        }

        if (_currentWave >= 5)
        {
            var shieldPercent = 0.33f + (_currentWave - 5) * 0.05f;
            shieldPercent = MathHelper.Min(shieldPercent, 0.8f);
            Shield = (int)(MaxHealth * shieldPercent);
            MaxShield = Shield;
        }
        else
        {
            Shield = 0;
            MaxShield = 0;
        }

        _shootTimer = (float)new Random().NextDouble() * _shootCooldown;
    }

    public void Update(float deltaTime, Vector2 playerPos, Vector2? decoyPos)
    {
        var target = decoyPos ?? playerPos;
        var direction = target - Position;
        if (direction != Vector2.Zero) direction.Normalize();
        Position += direction * Speed * deltaTime;

        if (IsRanged)
            _shootTimer -= deltaTime;

        if (_type == 1)
        {
            if (target.X < Position.X) _spriteEffect = SpriteEffects.None;
            else _spriteEffect = SpriteEffects.FlipHorizontally;
        }
        else
        {
            if (target.X < Position.X) _spriteEffect = SpriteEffects.FlipHorizontally;
            else _spriteEffect = SpriteEffects.None;
        }
    }

    public bool TryShoot(Vector2 targetPos, out Vector2 direction, out int damage)
    {
        direction = Vector2.Zero;
        damage = 0;
        if (!IsRanged) return false;
        if (_shootTimer > 0) return false;

        var toTarget = targetPos - Position;
        var dist = toTarget.Length();
        if (dist <= 1f || dist > _shootRange) return false;
        direction = toTarget / dist;
        damage = _shootDamage;
        _shootTimer = _shootCooldown;
        return true;
    }

    public void TakeDamage(int amount)
    {
        if (Shield > 0)
        {
            var shieldDamage = Math.Min(Shield, amount);
            Shield -= shieldDamage;
            amount -= shieldDamage;
        }
        if (amount > 0)
            Health -= amount;
    }

    public bool CollidesWith(Player player)
    {
        var enemyRect = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
        var playerRect = new Rectangle((int)player.Position.X, (int)player.Position.Y, 32, 32);
        return enemyRect.Intersects(playerRect);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture, Position, null, Color.White, 0f, Vector2.Zero, 1f, _spriteEffect, 0f);

        var hp01 = MaxHealth <= 0 ? 0f : MathHelper.Clamp((float)Health / MaxHealth, 0f, 1f);
        var barW = 32;
        var barH = 5;
        var x = (int)Position.X;
        var y = (int)Position.Y - 8;
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(x, y, barW, barH), new Color(0, 0, 0, 160));
        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(x, y, (int)(barW * hp01), barH), Color.LimeGreen);

        if (MaxShield > 0)
        {
            var shield01 = MathHelper.Clamp((float)Shield / MaxShield, 0f, 1f);
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle(x, y - 4, (int)(barW * shield01), 3), Color.White);
        }
    }
}