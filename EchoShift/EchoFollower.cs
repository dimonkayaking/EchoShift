using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

public class EchoFollower
{
    public Vector2 Position { get; private set; }
    private readonly Texture2D _texture;
    private readonly int _followDelayFrames;

    public EchoFollower(Texture2D texture, Vector2 startPos, int followDelayFrames = 60)
    {
        _texture = texture;
        Position = startPos;
        _followDelayFrames = Math.Max(1, followDelayFrames);
    }

    public void Update(IReadOnlyCollection<Vector2> positionHistory)
    {
        if (positionHistory == null || positionHistory.Count < _followDelayFrames) return;
        Position = positionHistory.First();
    }

    public void SnapTo(Vector2 pos) => Position = pos;

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture, Position, new Color(0, 255, 255, 140));
    }
}