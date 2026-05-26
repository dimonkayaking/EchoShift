using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Player
{
    public Vector2 Position;
    public int Health;
    public int MaxHealth;
    public float Speed;
    public int Damage;
    public float ShootCooldownMax;
    private float _shootCooldown;
    public bool IsInvincible { get; private set; }
    private float _invincibleTimer;
    public bool IsShiftActive { get; set; }

    private Texture2D _texture;
    private SpriteEffects _spriteEffect = SpriteEffects.None;

    public static int ScreenWidth = 1280;
    public static int ScreenHeight = 720;

    public Player(Vector2 startPos)
    {
        Position = startPos;
        MaxHealth = 100;
        Health = MaxHealth;
        Speed = 300f;
        Damage = 10;
        ShootCooldownMax = 0.3f;
        _shootCooldown = 0;
        IsInvincible = false;
        IsShiftActive = false;
    }

    public void LoadContent()
    {
        _texture = TextureManager.Player;
    }

    public void Update(float deltaTime, MouseState mouse, KeyboardState keyboard)
    {
        var move = Vector2.Zero;
        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up)) move.Y -= 1;
        if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down)) move.Y += 1;
        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) move.X -= 1;
        if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) move.X += 1;
        if (move != Vector2.Zero) move.Normalize();
        Position += move * Speed * deltaTime;
        Position.X = MathHelper.Clamp(Position.X, 0, ScreenWidth - 32);
        Position.Y = MathHelper.Clamp(Position.Y, 0, ScreenHeight - 32);

        if (_shootCooldown > 0) _shootCooldown -= deltaTime;

        if (IsInvincible)
        {
            _invincibleTimer -= deltaTime;
            if (_invincibleTimer <= 0) IsInvincible = false;
        }

        var mousePos = mouse.Position.ToVector2();
        if (mousePos.X < Position.X) _spriteEffect = SpriteEffects.FlipHorizontally;
        else _spriteEffect = SpriteEffects.None;

        IsShiftActive = false;
    }

    public bool CanShoot(float deltaTime) => _shootCooldown <= 0;
    public void ResetShootCooldown() => _shootCooldown = ShootCooldownMax;

    public void TakeDamage(int amount)
    {
        if (IsInvincible) return;
        Health -= amount;
        IsInvincible = true;
        _invincibleTimer = 1.0f;
        if (Health < 0) Health = 0;
    }

    public void GiveIFrames(float seconds)
    {
        if (seconds <= 0) return;
        IsInvincible = true;
        _invincibleTimer = MathHelper.Max(_invincibleTimer, seconds);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture, Position, null, Color.White, 0f, Vector2.Zero, 1f, _spriteEffect, 0f);
    }
}