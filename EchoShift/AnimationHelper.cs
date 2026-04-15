using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class AnimationHelper
{
    private Texture2D _texture;
    private int _frameWidth;
    private int _frameHeight;
    private int _framesCount;
    private int _currentFrame;
    private float _frameTime;
    private float _elapsed;
    private bool _loop;

    public AnimationHelper(Texture2D texture, int framesCount, float frameDuration, bool loop = true)
    {
        _texture = texture;
        _framesCount = framesCount;
        _frameWidth = texture.Width / framesCount;
        _frameHeight = texture.Height;
        _frameTime = frameDuration;
        _loop = loop;
        _currentFrame = 0;
        _elapsed = 0;
    }

    public void Update(float deltaTime)
    {
        _elapsed += deltaTime;
        if (_elapsed >= _frameTime)
        {
            _elapsed = 0;
            _currentFrame++;
            if (_currentFrame >= _framesCount)
            {
                if (_loop) _currentFrame = 0;
                else _currentFrame = _framesCount - 1;
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position, SpriteEffects effects = SpriteEffects.None)
    {
        Rectangle sourceRect = new Rectangle(_currentFrame * _frameWidth, 0, _frameWidth, _frameHeight);
        spriteBatch.Draw(_texture, position, sourceRect, Color.White, 0f, Vector2.Zero, 1f, effects, 0f);
    }

    public void Reset() => _currentFrame = 0;
    public bool IsFinished => !_loop && _currentFrame == _framesCount - 1;
}