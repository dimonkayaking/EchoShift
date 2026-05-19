using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace EchoShift
{
    public class Game1 : Game
    {
        private enum Screen
        {
            Playing,
            Roulette,
            GameOver,
            WaveComplete,
        }

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private UI _ui;

        private Player _player;
        private EchoFollower _echo;
        private List<Enemy> _enemies;
        private List<Bullet> _bullets;
        private List<EnemyBullet> _enemyBullets;
        private List<EchoOrb> _echoOrbs;
        private int _kills;
        private int _echoPoints;
        private float _spawnTimer;
        private float _spawnInterval;
        private float _gameTimer;
        private bool _isMenuOpen;
        private Screen _screen;

        private WaveSystem _waveSystem;

        private Queue<Vector2> _positionHistory;
        private const int ECHO_HISTORY_LENGTH = 120;

        private KeyboardState _prevKeyboard;
        private MouseState _prevMouse;
        private Random _rand = new Random();

        private int _nextUpgradeKills = 5;
        private int _upgradeStep = 5;

        private Rectangle _nextWaveButton;
        private Rectangle _restartWaveButton;
        private bool _nextWaveButtonHover;
        private bool _restartWaveButtonHover;
        private float _waveCompleteFlashTimer;

        private Rectangle _gameOverRestartButton;
        private bool _gameOverButtonHover;

        private bool _isSlotSpinning;
        private float _slotSpinTimer;
        private const float SLOT_SPIN_DURATION = 2.2f;
        private int[,] _slotValues;
        private int _slotBetIndex;
        private readonly int[] _slotBets = new[] { 10, 20, 50, 100 };
        private string _slotResultText;
        private float _slotResultTimer;

        private Texture2D _crosshairTexture;

        private int _screenWidth;
        private int _screenHeight;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;

            var screenWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            var screenHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.PreferredBackBufferWidth = screenWidth;
            _graphics.PreferredBackBufferHeight = screenHeight;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();

            Window.IsBorderless = true;
            Window.Position = new Point(0, 0);
        }

        protected override void Initialize()
        {
            GameServices.GraphicsDevice = GraphicsDevice;
            _screenWidth = _graphics.PreferredBackBufferWidth;
            _screenHeight = _graphics.PreferredBackBufferHeight;

            Player.ScreenWidth = _screenWidth;
            Player.ScreenHeight = _screenHeight;
            Bullet.ScreenWidth = _screenWidth;
            Bullet.ScreenHeight = _screenHeight;
            EnemyBullet.ScreenWidth = _screenWidth;
            EnemyBullet.ScreenHeight = _screenHeight;

            _enemies = new List<Enemy>();
            _bullets = new List<Bullet>();
            _enemyBullets = new List<EnemyBullet>();
            _echoOrbs = new List<EchoOrb>();
            _kills = 0;
            _echoPoints = 0;
            _spawnTimer = 0f;
            _spawnInterval = 2.0f;
            _gameTimer = 0f;
            _isMenuOpen = false;
            _screen = Screen.Playing;
            _waveCompleteFlashTimer = 0f;

            _waveSystem = new WaveSystem();
            _waveSystem.StartWave();

            _positionHistory = new Queue<Vector2>();
            _prevKeyboard = Keyboard.GetState();
            _prevMouse = Mouse.GetState();

            _slotValues = new int[3, 3];
            _slotBetIndex = 0;
            _isSlotSpinning = false;
            _slotResultText = "";
            _slotResultTimer = 0f;
            for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                    _slotValues[i, j] = _rand.Next(0, 10);

            _ui = new UI();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            TextureManager.Load(Content);
            _ui.LoadContent(Content);

            _crosshairTexture = new Texture2D(GraphicsDevice, 31, 31);
            var data = new Color[31 * 31];
            for (var i = 0; i < data.Length; i++) data[i] = Color.Transparent;

            var center = 15;
            var gap = 4;

            for (var y = center - 1; y <= center; y++)
            {
                for (var x = 0; x < 31; x++)
                {
                    if (Math.Abs(x - center) > gap)
                        data[y * 31 + x] = Color.White;
                }
            }

            for (var x = center - 1; x <= center; x++)
            {
                for (var y = 0; y < 31; y++)
                {
                    if (Math.Abs(y - center) > gap)
                        data[y * 31 + x] = Color.White;
                }
            }

            _crosshairTexture.SetData(data);

            _player = new Player(new Vector2(_screenWidth / 2, _screenHeight / 2));
            _player.LoadContent();

            for (var i = 0; i < ECHO_HISTORY_LENGTH; i++)
                _positionHistory.Enqueue(_player.Position);

            _echo = new EchoFollower(TextureManager.DecoyTex, _player.Position, followDelayFrames: 60);
        }

        protected override void Update(GameTime gameTime)
        {
            var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (deltaTime > 0.033f) deltaTime = 0.033f;

            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();

            _screenWidth = _graphics.PreferredBackBufferWidth;
            _screenHeight = _graphics.PreferredBackBufferHeight;

            if (keyboard.IsKeyDown(Keys.F11) && !_prevKeyboard.IsKeyDown(Keys.F11))
            {
                _graphics.IsFullScreen = !_graphics.IsFullScreen;
                _graphics.ApplyChanges();
            }

            if (keyboard.IsKeyDown(Keys.Escape) && !_prevKeyboard.IsKeyDown(Keys.Escape))
            {
                if (_screen == Screen.Playing)
                    _isMenuOpen = !_isMenuOpen;
                else if (_screen == Screen.WaveComplete)
                    _screen = Screen.Playing;
                else if (_screen == Screen.Roulette)
                    _screen = Screen.Playing;
            }

            if (_screen == Screen.GameOver)
            {
                if (keyboard.IsKeyDown(Keys.R) && !_prevKeyboard.IsKeyDown(Keys.R))
                    RestartGame();
                if (keyboard.IsKeyDown(Keys.Enter) && !_prevKeyboard.IsKeyDown(Keys.Enter))
                    RestartGame();

                var click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
                var btnW = _screenWidth * 25 / 100;
                var btnH = _screenHeight * 8 / 100;
                var btnX = (_screenWidth - btnW) / 2;
                var btnY = _screenHeight * 65 / 100;
                _gameOverRestartButton = new Rectangle(btnX, btnY, btnW, btnH);
                _gameOverButtonHover = _gameOverRestartButton.Contains(mouse.Position);
                if (click && _gameOverButtonHover)
                    RestartGame();

                _prevKeyboard = keyboard;
                _prevMouse = mouse;
                base.Update(gameTime);
                return;
            }

            if (_screen == Screen.WaveComplete)
            {
                _waveCompleteFlashTimer += deltaTime;
                var btnW = _screenWidth * 25 / 100;
                var btnH = _screenHeight * 8 / 100;
                var btnX = (_screenWidth - btnW) / 2;
                _nextWaveButton = new Rectangle(btnX, _screenHeight * 45 / 100, btnW, btnH);
                _restartWaveButton = new Rectangle(btnX, _screenHeight * 55 / 100, btnW, btnH);
                var mousePos = mouse.Position;
                _nextWaveButtonHover = _nextWaveButton.Contains(mousePos);
                _restartWaveButtonHover = _restartWaveButton.Contains(mousePos);
                var click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
                if (click)
                {
                    if (_nextWaveButtonHover) NextWave();
                    else if (_restartWaveButtonHover) RestartGame();
                }
                if (keyboard.IsKeyDown(Keys.Enter) && !_prevKeyboard.IsKeyDown(Keys.Enter))
                    NextWave();
                if (keyboard.IsKeyDown(Keys.R) && !_prevKeyboard.IsKeyDown(Keys.R))
                    RestartGame();

                _prevKeyboard = keyboard;
                _prevMouse = mouse;
                base.Update(gameTime);
                return;
            }

            if (_screen == Screen.Roulette)
            {
                UpdateSlotMachine(deltaTime, keyboard);
                _prevKeyboard = keyboard;
                _prevMouse = mouse;
                base.Update(gameTime);
                return;
            }

            if (_isMenuOpen)
            {
                var click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
                var btnW = _screenWidth * 25 / 100;
                var btnH = _screenHeight * 7 / 100;
                var btnX = (_screenWidth - btnW) / 2;
                var resumeRect = new Rectangle(btnX, _screenHeight * 38 / 100, btnW, btnH);
                var restartRect = new Rectangle(btnX, _screenHeight * 48 / 100, btnW, btnH);
                var quitRect = new Rectangle(btnX, _screenHeight * 58 / 100, btnW, btnH);
                if (click)
                {
                    if (resumeRect.Contains(mouse.Position)) _isMenuOpen = false;
                    else if (restartRect.Contains(mouse.Position)) RestartGame();
                    else if (quitRect.Contains(mouse.Position)) Exit();
                }
                if (keyboard.IsKeyDown(Keys.Enter) && !_prevKeyboard.IsKeyDown(Keys.Enter))
                    _isMenuOpen = false;
                if (keyboard.IsKeyDown(Keys.R) && !_prevKeyboard.IsKeyDown(Keys.R))
                    RestartGame();
                if (keyboard.IsKeyDown(Keys.Q) && !_prevKeyboard.IsKeyDown(Keys.Q))
                    Exit();

                _prevKeyboard = keyboard;
                _prevMouse = mouse;
                base.Update(gameTime);
                return;
            }

            _positionHistory.Enqueue(_player.Position);
            if (_positionHistory.Count > ECHO_HISTORY_LENGTH) _positionHistory.Dequeue();

            _player.Update(deltaTime, Mouse.GetState(), keyboard);
            _echo.Update(_positionHistory);

            var shiftDown = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
            var prevShiftDown = _prevKeyboard.IsKeyDown(Keys.LeftShift) || _prevKeyboard.IsKeyDown(Keys.RightShift);
            if (shiftDown && !prevShiftDown && GetTeleportsAvailable() > 0)
                TeleportToEcho();

            if (keyboard.IsKeyDown(Keys.Space) && !_prevKeyboard.IsKeyDown(Keys.Space))
                HealPlayer();

            if (keyboard.IsKeyDown(Keys.Tab) && !_prevKeyboard.IsKeyDown(Keys.Tab))
            {
                _screen = Screen.Roulette;
                _slotResultText = "";
                _slotResultTimer = 0f;
                _isSlotSpinning = false;
                for (var i = 0; i < 3; i++)
                    for (var j = 0; j < 3; j++)
                        _slotValues[i, j] = _rand.Next(0, 10);
            }

            var mouseNow = Mouse.GetState();
            if (mouseNow.LeftButton == ButtonState.Pressed && _player.CanShoot(deltaTime))
                Shoot();

            for (var i = 0; i < _bullets.Count; i++)
            {
                _bullets[i].Update(deltaTime);
                if (_bullets[i].IsExpired)
                {
                    _bullets.RemoveAt(i);
                    i--;
                }
            }

            for (var i = 0; i < _enemyBullets.Count; i++)
            {
                _enemyBullets[i].Update(deltaTime);
                if (_enemyBullets[i].CollidesWith(_player) && !_player.IsInvincible)
                {
                    _player.TakeDamage(_enemyBullets[i].Damage);
                    _enemyBullets[i].IsExpired = true;
                    if (_player.Health <= 0) _screen = Screen.GameOver;
                }
                if (_enemyBullets[i].IsExpired)
                {
                    _enemyBullets.RemoveAt(i);
                    i--;
                }
            }

            for (var i = 0; i < _enemies.Count; i++)
            {
                _enemies[i].Update(deltaTime, _player.Position, decoyPos: null);

                if (_enemies[i].IsMelee && _enemies[i].CollidesWith(_player) && !_player.IsInvincible)
                {
                    _player.TakeDamage(_enemies[i].Damage);
                    if (_player.Health <= 0) _screen = Screen.GameOver;
                }

                if (_enemies[i].TryShoot(_player.Position, out var dir, out var dmg))
                    _enemyBullets.Add(new EnemyBullet(_enemies[i].Position + new Vector2(16, 16), dir, dmg));

                for (var j = 0; j < _bullets.Count; j++)
                {
                    if (_bullets[j].CollidesWith(_enemies[i]))
                    {
                        var enemyType = _enemies[i].IsMelee ? 1 : (_enemies[i].MaxHealth > 50 ? 2 : 0);
                        _enemies[i].TakeDamage(_player.Damage);
                        _bullets[j].IsExpired = true;
                        if (_enemies[i].IsDead)
                        {
                            _kills++;
                            _waveSystem.OnEnemyKilled(enemyType);
                            SpawnEchoOrbs(_enemies[i].Position, _enemies[i].EchoValue);
                            _enemies.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }
            }

            _waveSystem.Update(deltaTime, _enemies);
            if (_waveSystem.IsWaveComplete && _screen == Screen.Playing)
            {
                _screen = Screen.WaveComplete;
                _waveCompleteFlashTimer = 0f;
                _echoPoints += 10 + _waveSystem.CurrentWave * 2;
            }

            if (_waveSystem.IsWaveInProgress && _waveSystem.HasEnemiesToSpawn())
            {
                _spawnTimer += deltaTime;
                _spawnInterval = _waveSystem.GetSpawnInterval();
                if (_spawnTimer >= _spawnInterval)
                {
                    _spawnTimer = 0;
                    var type = _waveSystem.GetNextEnemyType();
                    SpawnEnemy(type);
                    _waveSystem.OnEnemySpawned(type);
                }
            }

            for (var i = 0; i < _echoOrbs.Count; i++)
            {
                _echoOrbs[i].Update(deltaTime, _player.Position);
                if (_echoOrbs[i].Collected)
                {
                    _echoPoints += _echoOrbs[i].Value;
                    _echoOrbs.RemoveAt(i);
                    i--;
                }
            }

            CheckUpgrades();
            _gameTimer += deltaTime;
            _prevKeyboard = keyboard;
            _prevMouse = mouseNow;
            base.Update(gameTime);
        }

        private void UpdateSlotMachine(float deltaTime, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Escape) && !_prevKeyboard.IsKeyDown(Keys.Escape))
            {
                _screen = Screen.Playing;
                return;
            }

            if (!_isSlotSpinning)
            {
                if (keyboard.IsKeyDown(Keys.Left) && !_prevKeyboard.IsKeyDown(Keys.Left))
                    _slotBetIndex = (_slotBetIndex + _slotBets.Length - 1) % _slotBets.Length;
                if (keyboard.IsKeyDown(Keys.Right) && !_prevKeyboard.IsKeyDown(Keys.Right))
                    _slotBetIndex = (_slotBetIndex + 1) % _slotBets.Length;
                if (keyboard.IsKeyDown(Keys.Up) && !_prevKeyboard.IsKeyDown(Keys.Up))
                {
                    var newBet = _slotBets[_slotBetIndex] + 10;
                    var bestIdx = _slotBetIndex;
                    for (var i = 0; i < _slotBets.Length; i++)
                        if (_slotBets[i] >= newBet && _slotBets[i] < _slotBets[bestIdx])
                            bestIdx = i;
                    if (_slotBets[bestIdx] < newBet) bestIdx = _slotBets.Length - 1;
                    _slotBetIndex = bestIdx;
                }
                if (keyboard.IsKeyDown(Keys.Down) && !_prevKeyboard.IsKeyDown(Keys.Down))
                {
                    var newBet = _slotBets[_slotBetIndex] - 10;
                    var bestIdx = _slotBetIndex;
                    for (var i = _slotBets.Length - 1; i >= 0; i--)
                        if (_slotBets[i] <= newBet && _slotBets[i] > _slotBets[bestIdx])
                            bestIdx = i;
                    if (_slotBets[bestIdx] > newBet) bestIdx = 0;
                    _slotBetIndex = bestIdx;
                }
            }

            if (keyboard.IsKeyDown(Keys.Space) && !_prevKeyboard.IsKeyDown(Keys.Space) && !_isSlotSpinning)
            {
                var bet = _slotBets[_slotBetIndex];
                if (_echoPoints >= bet)
                {
                    _echoPoints -= bet;
                    _isSlotSpinning = true;
                    _slotSpinTimer = 0f;
                    _slotResultText = "";
                }
                else
                {
                    _slotResultText = "НЕ ХВАТАЕТ ОЧКОВ";
                    _slotResultTimer = 1.0f;
                }
            }

            if (_isSlotSpinning)
            {
                _slotSpinTimer += deltaTime;
                for (var col = 0; col < 3; col++)
                    for (var row = 0; row < 3; row++)
                        _slotValues[row, col] = _rand.Next(0, 10);

                if (_slotSpinTimer >= SLOT_SPIN_DURATION)
                {
                    _isSlotSpinning = false;
                    for (var col = 0; col < 3; col++)
                    {
                        var colValues = new int[3];
                        for (var row = 0; row < 3; row++)
                            colValues[row] = _rand.Next(0, 10);
                        for (var row = 0; row < 3; row++)
                            _slotValues[row, col] = colValues[row];
                    }

                    var val0 = _slotValues[1, 0];
                    var val1 = _slotValues[1, 1];
                    var val2 = _slotValues[1, 2];
                    var bet = _slotBets[_slotBetIndex];
                    var win = 0;
                    string resultMsg = "";

                    if (val0 == val1 && val1 == val2)
                    {
                        win = bet * 5;
                        resultMsg = $"ДЖЕКПОТ! +{win}";
                    }
                    else if ((val1 - val0) == (val2 - val1) && (val1 - val0) != 0)
                    {
                        win = bet * 2;
                        resultMsg = $"ПРОГРЕССИЯ! +{win}";
                    }
                    else
                    {
                        win = 0;
                        resultMsg = "ПРОИГРЫШ";
                    }

                    if (win > 0)
                        _echoPoints += win;
                    _slotResultText = resultMsg;
                    _slotResultTimer = 2.0f;
                }
            }
            else
            {
                if (_slotResultTimer > 0) _slotResultTimer -= deltaTime;
                else _slotResultText = "";
            }
        }

        private void NextWave()
        {
            _enemies.Clear();
            _waveSystem.NextWave();
            _spawnTimer = 0;
            _spawnInterval = _waveSystem.GetSpawnInterval();
            _screen = Screen.Playing;
            _player.Health = Math.Min(_player.MaxHealth, _player.Health + 15);
        }

        private void TeleportToEcho()
        {
            _player.Position = _echo.Position;
            _player.GiveIFrames(0.25f);
            _echo.SnapTo(_player.Position);
            _positionHistory.Clear();
            for (var i = 0; i < ECHO_HISTORY_LENGTH; i++)
                _positionHistory.Enqueue(_player.Position);
            _echoPoints = Math.Max(0, _echoPoints - 20);
        }

        private void HealPlayer()
        {
            const int cost = 20;
            const int heal = 30;
            if (_echoPoints < cost) return;
            if (_player.Health >= _player.MaxHealth) return;
            _echoPoints -= cost;
            _player.Health = Math.Min(_player.MaxHealth, _player.Health + heal);
        }

        private void Shoot()
        {
            var mousePos = Mouse.GetState().Position.ToVector2();
            var direction = mousePos - _player.Position;
            if (direction != Vector2.Zero) direction.Normalize();

            for (var i = -1; i <= 1; i++)
            {
                var offset = new Vector2(i * 6, 0);
                var dir = direction;
                if (i != 0) dir = Vector2.Normalize(direction + new Vector2(i * 0.15f, 0));
                _bullets.Add(new Bullet(_player.Position + offset, dir, _player.Damage));
            }
            _player.ResetShootCooldown();
        }

        private void SpawnEnemy(int type)
        {
            Vector2 pos;
            var side = _rand.Next(4);
            if (side == 0) pos = new Vector2(_rand.Next(0, _screenWidth), -50);
            else if (side == 1) pos = new Vector2(_screenWidth + 50, _rand.Next(0, _screenHeight));
            else if (side == 2) pos = new Vector2(_rand.Next(0, _screenWidth), _screenHeight + 50);
            else pos = new Vector2(-50, _rand.Next(0, _screenHeight));

            _enemies.Add(new Enemy(pos, type, _waveSystem.CurrentWave));
        }

        private void SpawnEchoOrbs(Vector2 pos, int totalValue)
        {
            var count = Math.Min(3, totalValue);
            var valueEach = totalValue / Math.Max(1, count);
            for (var i = 0; i < count; i++)
                _echoOrbs.Add(new EchoOrb(pos, valueEach));
        }

        private int GetTeleportsAvailable() => _echoPoints / 20;
        private int GetEchoProgress() => Math.Min(20, _echoPoints);

        private void CheckUpgrades()
        {
            if (_kills >= _nextUpgradeKills)
            {
                var upgradeIndex = _kills / 5;
                switch (upgradeIndex % 5)
                {
                    case 0: _player.Speed += 20f; break;
                    case 1: _player.Damage += 5; break;
                    case 2: _player.ShootCooldownMax = Math.Max(0.1f, _player.ShootCooldownMax - 0.05f); break;
                    case 3: _player.Health = Math.Min(_player.MaxHealth, _player.Health + 20); break;
                    case 4: _echoPoints += 20; break;
                }
                _nextUpgradeKills += _upgradeStep;
                if (_upgradeStep < 20) _upgradeStep += 5;
            }
        }

        private void RestartGame()
        {
            _enemies.Clear();
            _bullets.Clear();
            _enemyBullets.Clear();
            _echoOrbs.Clear();
            _kills = 0;
            _echoPoints = 0;
            _spawnTimer = 0f;
            _gameTimer = 0f;
            _isMenuOpen = false;
            _screen = Screen.Playing;
            _nextUpgradeKills = 5;
            _upgradeStep = 5;

            _player = new Player(new Vector2(_screenWidth / 2, _screenHeight / 2));
            _player.LoadContent();

            _waveSystem = new WaveSystem();
            _waveSystem.StartWave();
            _spawnInterval = _waveSystem.GetSpawnInterval();

            _positionHistory.Clear();
            for (var i = 0; i < ECHO_HISTORY_LENGTH; i++)
                _positionHistory.Enqueue(_player.Position);

            _echo = new EchoFollower(TextureManager.DecoyTex, _player.Position, followDelayFrames: 60);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();

            _spriteBatch.Draw(TextureManager.BackgroundDetailed, new Rectangle(0, 0, _screenWidth, _screenHeight), Color.White);

            foreach (var e in _enemies) e.Draw(_spriteBatch);
            foreach (var orb in _echoOrbs) orb.Draw(_spriteBatch);
            foreach (var b in _bullets) b.Draw(_spriteBatch);
            foreach (var eb in _enemyBullets) eb.Draw(_spriteBatch);
            _echo?.Draw(_spriteBatch);
            _player.Draw(_spriteBatch);

            DrawWaveUI();
            _ui.Draw(_spriteBatch, _kills, _echoPoints, GetEchoProgress(), GetTeleportsAvailable(),
                _player.Health, _player.MaxHealth, _nextUpgradeKills);

            if (_screen == Screen.GameOver) DrawGameOverScreen();
            if (_screen == Screen.WaveComplete) DrawWaveCompleteScreen();
            if (_screen == Screen.Roulette) DrawSlotMachineScreen();
            if (_isMenuOpen && _screen != Screen.GameOver) DrawPauseMenu();

            var mouse = Mouse.GetState();
            _spriteBatch.Draw(_crosshairTexture, new Rectangle(mouse.X - 15, mouse.Y - 15, 31, 31), Color.White);

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawWaveUI()
        {
            var centerX = _screenWidth / 2;

            var waveText = $"ВОЛНА {_waveSystem.CurrentWave}";
            var waveSize = TinyFont.Measure(waveText, 3);
            TinyFont.Draw(_spriteBatch, waveText, new Vector2(centerX - waveSize.X / 2, _screenHeight * 1 / 100), Color.Cyan, 3);

            var recordText = $"РЕКОРД: {_waveSystem.HighScoreWave}";
            TinyFont.Draw(_spriteBatch, recordText, new Vector2(_screenWidth - TinyFont.Measure(recordText, 2).X - _screenWidth * 1 / 100, _screenHeight * 1 / 100), Color.Gold, 2);

            var killsText = $"УБИЙСТВ: {_kills}";
            TinyFont.Draw(_spriteBatch, killsText, new Vector2(_screenWidth - TinyFont.Measure(killsText, 2).X - _screenWidth * 1 / 100, _screenHeight * 5 / 100), Color.White, 2);

            var yOffset = _screenHeight * 14 / 100;
            var lineH = _screenHeight * 3 / 100;
            TinyFont.Draw(_spriteBatch, "ОСТАЛОСЬ ВРАГОВ:", new Vector2(_screenWidth * 1 / 100, yOffset), Color.White, 2);
            yOffset += lineH;
            TinyFont.Draw(_spriteBatch, $"КРАСНЫХ: {_waveSystem.EnemiesTotalByType[0]}", new Vector2(_screenWidth * 1 / 100, yOffset), new Color(255, 80, 80), 2);
            yOffset += lineH;
            TinyFont.Draw(_spriteBatch, $"ОРАНЖЕВЫХ: {_waveSystem.EnemiesTotalByType[1]}", new Vector2(_screenWidth * 1 / 100, yOffset), new Color(255, 165, 0), 2);
            yOffset += lineH;
            TinyFont.Draw(_spriteBatch, $"ФИОЛЕТОВЫХ: {_waveSystem.EnemiesTotalByType[2]}", new Vector2(_screenWidth * 1 / 100, yOffset), new Color(160, 32, 240), 2);
            yOffset += lineH;
            if (_waveSystem.IsWaveInProgress && _enemies.Count > 0)
                TinyFont.Draw(_spriteBatch, $"В БОЮ: {_enemies.Count}", new Vector2(_screenWidth * 1 / 100, yOffset), Color.Gray, 2);

            TinyFont.Draw(_spriteBatch, "SHIFT: ТЕЛЕПОРТ (20 ЭХА)  ПРОБЕЛ: ЛЕЧЕНИЕ (20 ЭХА)  TAB: РУЛЕТКА  ESC: МЕНЮ",
                new Vector2(_screenWidth * 1 / 100, _screenHeight - _screenHeight * 6 / 100), new Color(150, 150, 150), 1);
            TinyFont.Draw(_spriteBatch, "ОЧКИ ЭХА ТРАТЯТСЯ НА ТЕЛЕПОРТ И ЛЕЧЕНИЕ",
                new Vector2(_screenWidth * 1 / 100, _screenHeight - _screenHeight * 4 / 100), new Color(100, 200, 200), 1);
        }

        private void DrawGameOverScreen()
        {
            var centerX = _screenWidth / 2;
            _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(0, 0, _screenWidth, _screenHeight), new Color(0, 0, 0, 220));

            var panelW = _screenWidth * 50 / 100;
            var panelH = _screenHeight * 55 / 100;
            var panelX = (_screenWidth - panelW) / 2;
            var panelY = _screenHeight * 20 / 100;
            _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(panelX, panelY, panelW, panelH), new Color(10, 16, 18, 240));

            var title = "ИГРА ОКОНЧЕНА";
            TinyFont.Draw(_spriteBatch, title, new Vector2(centerX - TinyFont.Measure(title, 4).X / 2, panelY + _screenHeight * 5 / 100), new Color(255, 80, 80), 4);

            var wavesText = $"ПРОЙДЕНО ВОЛН: {_waveSystem.CurrentWave - 1}";
            TinyFont.Draw(_spriteBatch, wavesText, new Vector2(centerX - TinyFont.Measure(wavesText, 3).X / 2, panelY + _screenHeight * 14 / 100), Color.Cyan, 3);

            var killsText = $"УБИТО ВРАГОВ: {_kills}";
            TinyFont.Draw(_spriteBatch, killsText, new Vector2(centerX - TinyFont.Measure(killsText, 3).X / 2, panelY + _screenHeight * 22 / 100), Color.Yellow, 3);

            var echoText = $"ОЧКОВ ЭХА: {_echoPoints}";
            TinyFont.Draw(_spriteBatch, echoText, new Vector2(centerX - TinyFont.Measure(echoText, 2).X / 2, panelY + _screenHeight * 30 / 100), Color.Cyan, 2);

            if (_waveSystem.CurrentWave - 1 == _waveSystem.HighScoreWave && _waveSystem.HighScoreWave > 0)
            {
                var rec = "НОВЫЙ РЕКОРД!";
                TinyFont.Draw(_spriteBatch, rec, new Vector2(centerX - TinyFont.Measure(rec, 3).X / 2, panelY + _screenHeight * 38 / 100), Color.Gold, 3);
            }
            else if (_waveSystem.HighScoreWave > 0)
            {
                var rec = $"РЕКОРД: {_waveSystem.HighScoreWave} ВОЛН";
                TinyFont.Draw(_spriteBatch, rec, new Vector2(centerX - TinyFont.Measure(rec, 2).X / 2, panelY + _screenHeight * 38 / 100), new Color(200, 200, 100), 2);
            }

            _gameOverButtonHover = _gameOverRestartButton.Contains(Mouse.GetState().Position);
            var btnColor = _gameOverButtonHover ? new Color(255, 80, 80, 150) : new Color(255, 80, 80, 80);
            _spriteBatch.Draw(TextureManager.Pixel, _gameOverRestartButton, btnColor);
            var btnText = "НАЧАТЬ ЗАНОВО";
            TinyFont.Draw(_spriteBatch, btnText, new Vector2(_gameOverRestartButton.X + (_gameOverRestartButton.Width - TinyFont.Measure(btnText, 3).X) / 2, _gameOverRestartButton.Y + (_gameOverRestartButton.Height - TinyFont.Measure(btnText, 3).Y) / 2), Color.White, 3);

            var hint = "R ИЛИ ENTER ДЛЯ РЕСТАРТА";
            TinyFont.Draw(_spriteBatch, hint, new Vector2(centerX - TinyFont.Measure(hint, 2).X / 2, panelY + panelH - _screenHeight * 5 / 100), new Color(150, 150, 150), 2);
        }

        private void DrawWaveCompleteScreen()
        {
            var centerX = _screenWidth / 2;
            _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(0, 0, _screenWidth, _screenHeight), new Color(0, 0, 0, 200));

            var panelW = _screenWidth * 50 / 100;
            var panelH = _screenHeight * 65 / 100;
            var panelX = (_screenWidth - panelW) / 2;
            var panelY = _screenHeight * 15 / 100;
            _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(panelX, panelY, panelW, panelH), new Color(10, 16, 18, 240));
            var alpha = 0.5f + (float)Math.Sin(_waveCompleteFlashTimer * 8) * 0.5f;
            _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(panelX, panelY, panelW, 2), new Color(0, 255, 255, alpha));

            var title = $"ВОЛНА {_waveSystem.CurrentWave - 1} ЗАВЕРШЕНА!";
            TinyFont.Draw(_spriteBatch, title, new Vector2(centerX - TinyFont.Measure(title, 3).X / 2, panelY + _screenHeight * 5 / 100), Color.Cyan, 3);

            var nextText = $"СЛЕДУЮЩАЯ ВОЛНА: {_waveSystem.CurrentWave}";
            TinyFont.Draw(_spriteBatch, nextText, new Vector2(centerX - TinyFont.Measure(nextText, 2).X / 2, panelY + _screenHeight * 15 / 100), Color.Yellow, 2);

            var killsText = $"УБИЙСТВ: {_kills}";
            TinyFont.Draw(_spriteBatch, killsText, new Vector2(centerX - TinyFont.Measure(killsText, 2).X / 2, panelY + _screenHeight * 22 / 100), Color.White, 2);

            var echoText = $"ОЧКОВ ЭХА: {_echoPoints}";
            TinyFont.Draw(_spriteBatch, echoText, new Vector2(centerX - TinyFont.Measure(echoText, 2).X / 2, panelY + _screenHeight * 28 / 100), Color.Cyan, 2);

            var nextCol = _nextWaveButtonHover ? new Color(0, 255, 255, 150) : new Color(0, 255, 255, 80);
            _spriteBatch.Draw(TextureManager.Pixel, _nextWaveButton, nextCol);
            var nextBtn = "СЛЕДУЮЩАЯ ВОЛНА";
            TinyFont.Draw(_spriteBatch, nextBtn, new Vector2(_nextWaveButton.X + (_nextWaveButton.Width - TinyFont.Measure(nextBtn, 3).X) / 2, _nextWaveButton.Y + (_nextWaveButton.Height - TinyFont.Measure(nextBtn, 3).Y) / 2), Color.White, 3);

            var restartCol = _restartWaveButtonHover ? new Color(255, 80, 80, 150) : new Color(255, 80, 80, 80);
            _spriteBatch.Draw(TextureManager.Pixel, _restartWaveButton, restartCol);
            var restartBtn = "НАЧАТЬ ЗАНОВО";
            TinyFont.Draw(_spriteBatch, restartBtn, new Vector2(_restartWaveButton.X + (_restartWaveButton.Width - TinyFont.Measure(restartBtn, 3).X) / 2, _restartWaveButton.Y + (_restartWaveButton.Height - TinyFont.Measure(restartBtn, 3).Y) / 2), Color.White, 3);

            var hint = "ENTER - ДАЛЕЕ    R - РЕСТАРТ    ESC - ЗАКРЫТЬ";
            TinyFont.Draw(_spriteBatch, hint, new Vector2(centerX - TinyFont.Measure(hint, 2).X / 2, panelY + panelH - _screenHeight * 8 / 100), new Color(150, 150, 150), 2);
        }

        private void DrawSlotMachineScreen()
        {
            var centerX = _screenWidth / 2;
            _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(0, 0, _screenWidth, _screenHeight), new Color(0, 0, 0, 220));

            var panelW = _screenWidth * 70 / 100;
            var panelH = _screenHeight * 75 / 100;
            var panelX = (_screenWidth - panelW) / 2;
            var panelY = _screenHeight * 10 / 100;
            _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(panelX, panelY, panelW, panelH), new Color(10, 16, 18, 240));
            _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(panelX, panelY, panelW, 2), new Color(255, 215, 0, 160));

            var title = "СЛОТ 3x3";
            TinyFont.Draw(_spriteBatch, title, new Vector2(centerX - TinyFont.Measure(title, 4).X / 2, panelY + _screenHeight * 2 / 100), Color.Gold, 4);

            var pointsText = $"ОЧКОВ: {_echoPoints}";
            TinyFont.Draw(_spriteBatch, pointsText, new Vector2(centerX - TinyFont.Measure(pointsText, 2).X / 2, panelY + _screenHeight * 8 / 100), Color.Cyan, 2);

            var cellSize = _screenWidth * 7 / 100;
            var cellSpacing = _screenWidth * 1 / 100;
            var tableWidth = 3 * cellSize + 2 * cellSpacing;
            var startX = centerX - tableWidth / 2;
            var tableY = panelY + _screenHeight * 16 / 100;
            var fontSize = 3;

            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var x = startX + col * (cellSize + cellSpacing);
                    var y = tableY + row * (cellSize + cellSpacing);
                    var bgColor = (row == 1) ? new Color(0, 40, 50, 200) : new Color(20, 20, 30, 200);
                    _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(x, y, cellSize, cellSize), bgColor);
                    _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(x, y, cellSize, 2), Color.Gray);
                    _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(x, y + cellSize - 2, cellSize, 2), Color.Gray);
                    _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(x, y, 2, cellSize), Color.Gray);
                    _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(x + cellSize - 2, y, 2, cellSize), Color.Gray);

                    var digit = _slotValues[row, col].ToString();
                    var digitSize = TinyFont.Measure(digit, fontSize);
                    Color digitColor;
                    switch (_slotValues[row, col])
                    {
                        case 0: digitColor = Color.White; break;
                        case 1: digitColor = Color.Cyan; break;
                        case 2: digitColor = Color.LimeGreen; break;
                        case 3: digitColor = Color.Yellow; break;
                        case 4: digitColor = Color.Orange; break;
                        case 5: digitColor = Color.HotPink; break;
                        case 6: digitColor = Color.Purple; break;
                        case 7: digitColor = Color.LightBlue; break;
                        case 8: digitColor = Color.Gold; break;
                        case 9: digitColor = Color.Tomato; break;
                        default: digitColor = Color.White; break;
                    }
                    TinyFont.Draw(_spriteBatch, digit,
                        new Vector2(x + (cellSize - digitSize.X) / 2, y + (cellSize - digitSize.Y) / 2),
                        digitColor, fontSize);
                }
            }

            var bottomY = panelY + panelH - _screenHeight * 14 / 100;
            var betLabel = "СТАВКА";
            var betValue = _slotBets[_slotBetIndex].ToString();
            var betLabelSize = TinyFont.Measure(betLabel, 3);
            var betValueSize = TinyFont.Measure(betValue, 3);
            var arrowScale = 3;
            var arrowSize = TinyFont.Measure("<", arrowScale);
            var totalWidth = (int)(betLabelSize.X + arrowSize.X * 2 + betValueSize.X + 20);
            var startBetX = centerX - totalWidth / 2;
            TinyFont.Draw(_spriteBatch, betLabel, new Vector2(startBetX, bottomY), Color.White, 3);
            TinyFont.Draw(_spriteBatch, "<", new Vector2(startBetX + betLabelSize.X + 5, bottomY), new Color(0, 255, 255), arrowScale);
            TinyFont.Draw(_spriteBatch, betValue, new Vector2(startBetX + betLabelSize.X + arrowSize.X + 10, bottomY), Color.Yellow, 3);
            TinyFont.Draw(_spriteBatch, ">", new Vector2(startBetX + betLabelSize.X + arrowSize.X + betValueSize.X + 15, bottomY), new Color(0, 255, 255), arrowScale);
            bottomY += _screenHeight * 5 / 100;

            var allHints = "СТАВКА ЧЕРЕЗ СТРЕЛКИ     ПРОБЕЛ: КРУТИТЬ     ESC: ВЫЙТИ";
            TinyFont.Draw(_spriteBatch, allHints, new Vector2(centerX - TinyFont.Measure(allHints, 2).X / 2, bottomY), new Color(200, 200, 200), 2);
            bottomY += _screenHeight * 4 / 100;

            var rules = "ДЖЕКПОТ: 3 ОДИНАКОВЫХ (x5)   |   ПРОГРЕССИЯ (x2)";
            TinyFont.Draw(_spriteBatch, rules, new Vector2(centerX - TinyFont.Measure(rules, 2).X / 2, bottomY), new Color(255, 215, 0, 200), 2);

            if (!string.IsNullOrWhiteSpace(_slotResultText) && _slotResultTimer > 0)
                TinyFont.Draw(_spriteBatch, _slotResultText, new Vector2(centerX - TinyFont.Measure(_slotResultText, 2).X / 2, bottomY + _screenHeight * 4 / 100), Color.Yellow, 2);
        }

        private void DrawPauseMenu()
        {
            var centerX = _screenWidth / 2;
            _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(0, 0, _screenWidth, _screenHeight), new Color(0, 0, 0, 180));

            var panelW = _screenWidth * 35 / 100;
            var panelH = _screenHeight * 50 / 100;
            var panelX = (_screenWidth - panelW) / 2;
            var panelY = _screenHeight * 25 / 100;
            _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(panelX, panelY, panelW, panelH), new Color(10, 16, 18, 240));
            _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(panelX, panelY, panelW, 2), new Color(0, 255, 255, 160));

            var title = "МЕНЮ";
            TinyFont.Draw(_spriteBatch, title, new Vector2(centerX - TinyFont.Measure(title, 4).X / 2, panelY + _screenHeight * 5 / 100), Color.Cyan, 4);

            var btnW = _screenWidth * 25 / 100;
            var btnH = _screenHeight * 7 / 100;
            var btnX = (_screenWidth - btnW) / 2;
            var yOff = panelY + _screenHeight * 15 / 100;
            var resumeBtn = new Rectangle(btnX, yOff, btnW, btnH);
            var restartBtn = new Rectangle(btnX, yOff + _screenHeight * 9 / 100, btnW, btnH);
            var quitBtn = new Rectangle(btnX, yOff + _screenHeight * 18 / 100, btnW, btnH);

            var mouse = Mouse.GetState();
            var resumeCol = resumeBtn.Contains(mouse.Position) ? new Color(0, 255, 255, 120) : new Color(0, 255, 255, 60);
            var restartCol = restartBtn.Contains(mouse.Position) ? new Color(255, 80, 80, 120) : new Color(255, 80, 80, 60);
            var quitCol = quitBtn.Contains(mouse.Position) ? new Color(200, 50, 50, 120) : new Color(200, 50, 50, 60);

            _spriteBatch.Draw(TextureManager.Pixel, resumeBtn, resumeCol);
            _spriteBatch.Draw(TextureManager.Pixel, restartBtn, restartCol);
            _spriteBatch.Draw(TextureManager.Pixel, quitBtn, quitCol);

            var resumeText = "ПРОДОЛЖИТЬ";
            var restartText = "НАЧАТЬ ЗАНОВО";
            var quitText = "ВЫХОД";

            TinyFont.Draw(_spriteBatch, resumeText, new Vector2(resumeBtn.X + (resumeBtn.Width - TinyFont.Measure(resumeText, 3).X) / 2, resumeBtn.Y + (resumeBtn.Height - TinyFont.Measure(resumeText, 3).Y) / 2), Color.White, 3);
            TinyFont.Draw(_spriteBatch, restartText, new Vector2(restartBtn.X + (restartBtn.Width - TinyFont.Measure(restartText, 3).X) / 2, restartBtn.Y + (restartBtn.Height - TinyFont.Measure(restartText, 3).Y) / 2), Color.White, 3);
            TinyFont.Draw(_spriteBatch, quitText, new Vector2(quitBtn.X + (quitBtn.Width - TinyFont.Measure(quitText, 3).X) / 2, quitBtn.Y + (quitBtn.Height - TinyFont.Measure(quitText, 3).Y) / 2), Color.White, 3);

            var hint = "ENTER - ПРОДОЛЖИТЬ    R - РЕСТАРТ    Q - ВЫХОД";
            TinyFont.Draw(_spriteBatch, hint, new Vector2(centerX - TinyFont.Measure(hint, 2).X / 2, panelY + panelH - _screenHeight * 8 / 100), new Color(150, 150, 150), 2);
        }
    }
}