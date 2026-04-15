using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Player
{
    public Vector2 Position;
    public int Health;
    public float Speed;
    public int Damage;
    public float ShootCooldownMax;
    private float _shootCooldown;
    public bool IsInvincible { get; private set; }
    private float _invincibleTimer;
    public bool IsShiftActive { get; set; }

    private AnimationHelper _idleAnim;
    private AnimationHelper _walkAnim;
    private AnimationHelper _shootAnim;
    private AnimationHelper _currentAnim;
    private SpriteEffects _spriteEffect = SpriteEffects.None;

    public Player(Vector2 startPos)
    {
        Position = startPos;
        Health = 100;
        Speed = 300f;
        Damage = 10;
        ShootCooldownMax = 0.3f;
        _shootCooldown = 0;
        IsInvincible = false;
        IsShiftActive = false;
    }

    public void LoadContent()
    {
        _idleAnim = new AnimationHelper(TextureManager.PlayerIdle, 1, 0.1f, true);
        _walkAnim = new AnimationHelper(TextureManager.PlayerWalk, 5, 0.1f, true);
        _shootAnim = new AnimationHelper(TextureManager.PlayerShoot, 4, 0.05f, false);
        _currentAnim = _idleAnim;
    }

    public void Update(float deltaTime, MouseState mouse, KeyboardState keyboard)
    {
        Vector2 move = Vector2.Zero;
        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up)) move.Y -= 1;
        if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down)) move.Y += 1;
        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) move.X -= 1;
        if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) move.X += 1;
        if (move != Vector2.Zero) move.Normalize();
        Position += move * Speed * deltaTime;
        Position.X = MathHelper.Clamp(Position.X, 0, 1280 - 32);
        Position.Y = MathHelper.Clamp(Position.Y, 0, 720 - 32);

        bool isShooting = mouse.LeftButton == ButtonState.Pressed && _shootCooldown <= 0;
        bool isMoving = move != Vector2.Zero;

        if (isShooting)
        {
            if (_currentAnim != _shootAnim || _shootAnim.IsFinished)
            {
                _currentAnim = _shootAnim;
                _shootAnim.Reset();
            }
        }
        else if (isMoving)
        {
            _currentAnim = _walkAnim;
        }
        else
        {
            _currentAnim = _idleAnim;
        }

        _currentAnim.Update(deltaTime);

        if (_shootCooldown > 0) _shootCooldown -= deltaTime;

        if (IsInvincible)
        {
            _invincibleTimer -= deltaTime;
            if (_invincibleTimer <= 0) IsInvincible = false;
        }

        Vector2 mousePos = mouse.Position.ToVector2();
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

    public void Draw(SpriteBatch spriteBatch)
    {
        _currentAnim.Draw(spriteBatch, Position, _spriteEffect);
    }
}