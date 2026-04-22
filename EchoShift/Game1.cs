using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EchoShift
{
    public class Game1 : Game
    {
        private enum Screen
        {
            Playing,
            Roulette,
            GameOver,
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
        private int _echoPoints;     // очки, которые падают с врагов
        private float _spawnTimer;
        private float _spawnInterval;
        private float _gameTimer;
        private bool _isMenuOpen;
        private Screen _screen;

        // История для "эха" (задержка движения)
        private Queue<Vector2> _positionHistory;
        private const int ECHO_HISTORY_LENGTH = 120; // ~2 секунды при 60 FPS

        private KeyboardState _prevKeyboard;
        private MouseState _prevMouse;
        private Random _rand = new Random();

        // Рулетка
        private int _rouletteBetIndex;
        private readonly int[] _rouletteBets = new[] { 10, 20, 50, 100 };
        private string _rouletteResultText;
        private float _rouletteResultTimer;

        // Система улучшений
        private int _nextUpgradeKills = 5;
        private int _upgradeStep = 5;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            // Передаём GraphicsDevice в статический сервис (нужно для создания текстур-заглушек)
            GameServices.GraphicsDevice = GraphicsDevice;

            // Инициализируем списки и базовые переменные
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
            _rouletteBetIndex = 0;
            _rouletteResultText = "";
            _rouletteResultTimer = 0f;

            // Очереди истории
            _positionHistory = new Queue<Vector2>();
            _prevKeyboard = Keyboard.GetState();
            _prevMouse = Mouse.GetState();

            _ui = new UI();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // 1. Загружаем все текстуры через TextureManager (создаются программно)
            TextureManager.Load(Content);

            // 2. Загружаем шрифт для UI (если есть)
            _ui.LoadContent(Content);

            // 3. Теперь создаём игрока (анимации используют уже загруженные текстуры)
            _player = new Player(new Vector2(640, 360));
            _player.LoadContent();  // если нужна дополнительная загрузка (здесь пусто)

            // 4. Заполняем историю начальными значениями
            for (int i = 0; i < ECHO_HISTORY_LENGTH; i++)
            {
                _positionHistory.Enqueue(_player.Position);
            }

            // 5. Создаём "эхо", которое следует с задержкой
            _echo = new EchoFollower(TextureManager.DecoyTex, _player.Position, followDelayFrames: 60);
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (deltaTime > 0.033f) deltaTime = 0.033f;

            KeyboardState keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.Escape) && !_prevKeyboard.IsKeyDown(Keys.Escape))
            {
                // Esc всегда открывает/закрывает меню-оверлей (кроме рулетки, там Esc = назад)
                if (_screen == Screen.Playing || _screen == Screen.GameOver)
                    _isMenuOpen = !_isMenuOpen;
            }

            if (_screen == Screen.GameOver)
            {
                if (keyboard.IsKeyDown(Keys.R) && !_prevKeyboard.IsKeyDown(Keys.R))
                    RestartGame();
                if (keyboard.IsKeyDown(Keys.Enter) && !_prevKeyboard.IsKeyDown(Keys.Enter))
                    RestartGame();
                // меню в game over рисуется через оверлей
            }
            if (_screen == Screen.Roulette)
            {
                UpdateRoulette(deltaTime, keyboard);
                _prevKeyboard = keyboard;
                return;
            }
            if (_isMenuOpen)
            {
                // управление в меню (даже если нет шрифта — кнопки есть, но хоткеи работают всегда)
                if (keyboard.IsKeyDown(Keys.Enter) && !_prevKeyboard.IsKeyDown(Keys.Enter))
                {
                    if (_screen == Screen.GameOver) RestartGame();
                    else _isMenuOpen = false; // Resume
                }
                if (keyboard.IsKeyDown(Keys.R) && !_prevKeyboard.IsKeyDown(Keys.R))
                    RestartGame();
                if (keyboard.IsKeyDown(Keys.Q) && !_prevKeyboard.IsKeyDown(Keys.Q))
                {
                    if (_screen != Screen.GameOver) Exit();
                }

                // клики мышью по 3 кнопкам
                MouseState mouse = Mouse.GetState();
                bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
                if (click)
                {
                    Point p = mouse.Position;
                    var (resume, restart, quit) = GetMenuButtons();
                    if (_screen == Screen.GameOver)
                    {
                        if (restart.Contains(p)) RestartGame();
                    }
                    else
                    {
                        if (resume.Contains(p)) _isMenuOpen = false;
                        else if (restart.Contains(p)) RestartGame();
                        else if (quit.Contains(p)) Exit();
                    }
                }

                _prevKeyboard = keyboard;
                _prevMouse = Mouse.GetState();
                return;
            }

            // Сохраняем историю позиций для "эха"
            _positionHistory.Enqueue(_player.Position);
            if (_positionHistory.Count > ECHO_HISTORY_LENGTH) _positionHistory.Dequeue();

            // Обновление игрока
            _player.Update(deltaTime, Mouse.GetState(), keyboard);

            // Обновление "эха"
            _echo.Update(_positionHistory);

            // Телепорт к "эху" (Shift)
            bool shiftDown = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
            bool prevShiftDown = _prevKeyboard.IsKeyDown(Keys.LeftShift) || _prevKeyboard.IsKeyDown(Keys.RightShift);
            if (shiftDown && !prevShiftDown && GetTeleportsAvailable() > 0)
            {
                TeleportToEcho();
            }

            // Хил (Пробел)
            if (keyboard.IsKeyDown(Keys.Space) && !_prevKeyboard.IsKeyDown(Keys.Space))
            {
                HealPlayer();
            }
            if (keyboard.IsKeyDown(Keys.Tab) && !_prevKeyboard.IsKeyDown(Keys.Tab))
            {
                _screen = Screen.Roulette;
                _rouletteResultText = "";
                _rouletteResultTimer = 0f;
            }

            // Стрельба (ЛКМ)
            MouseState mouseNow = Mouse.GetState();
            if (mouseNow.LeftButton == ButtonState.Pressed && _player.CanShoot(deltaTime))
            {
                Shoot();
            }

            // Обновление пуль
            for (int i = 0; i < _bullets.Count; i++)
            {
                _bullets[i].Update(deltaTime);
                if (_bullets[i].IsExpired)
                {
                    _bullets.RemoveAt(i);
                    i--;
                }
            }

            // Обновление вражеских пуль
            for (int i = 0; i < _enemyBullets.Count; i++)
            {
                _enemyBullets[i].Update(deltaTime);
                if (_enemyBullets[i].CollidesWith(_player) && !_player.IsInvincible)
                {
                    _player.TakeDamage(_enemyBullets[i].Damage);
                    _enemyBullets[i].IsExpired = true;
                    if (_player.Health <= 0)
                    {
                        _screen = Screen.GameOver;
                        _isMenuOpen = true;
                    }
                }
                if (_enemyBullets[i].IsExpired)
                {
                    _enemyBullets.RemoveAt(i);
                    i--;
                }
            }

            // Обновление врагов, столкновения с пулями и игроком
            for (int i = 0; i < _enemies.Count; i++)
            {
                _enemies[i].Update(deltaTime, _player.Position, decoyPos: null);

                // Столкновение с игроком
                if (_enemies[i].IsMelee && _enemies[i].CollidesWith(_player) && !_player.IsInvincible)
                {
                    _player.TakeDamage(_enemies[i].Damage);
                    if (_player.Health <= 0)
                        _screen = Screen.GameOver;
                }

                // Стрельба медленных врагов
                if (_enemies[i].TryShoot(_player.Position, out Vector2 dir, out int dmg))
                {
                    _enemyBullets.Add(new EnemyBullet(_enemies[i].Position + new Vector2(16, 16), dir, dmg));
                }

                // Столкновения с пулями
                for (int j = 0; j < _bullets.Count; j++)
                {
                    if (_bullets[j].CollidesWith(_enemies[i]))
                    {
                        _enemies[i].TakeDamage(_player.Damage);
                        _bullets[j].IsExpired = true;
                        if (_enemies[i].IsDead)
                        {
                            _kills++;
                            SpawnEchoOrbs(_enemies[i].Position, _enemies[i].EchoValue);
                            _enemies.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }
            }

            // Притягивание сфер Эхо к игроку
            for (int i = 0; i < _echoOrbs.Count; i++)
            {
                _echoOrbs[i].Update(deltaTime, _player.Position);
                if (_echoOrbs[i].Collected)
                {
                    AddEchoPoints(_echoOrbs[i].Value);
                    _echoOrbs.RemoveAt(i);
                    i--;
                }
            }

            // Спавн врагов
            _spawnTimer += deltaTime;
            if (_spawnTimer >= _spawnInterval)
            {
                _spawnTimer = 0;
                SpawnEnemy();
                // Увеличиваем сложность: интервал спавна уменьшается со временем
                _spawnInterval = Math.Max(0.8f, 2.0f - _gameTimer / 120f);
            }

            // Проверка улучшений за убийства
            CheckUpgrades();

            _gameTimer += deltaTime;
            _prevKeyboard = keyboard;
            _prevMouse = mouseNow;
            base.Update(gameTime);
        }

        private void TeleportToEcho()
        {
            _player.Position = _echo.Position;
            _player.GiveIFrames(0.25f);
            _echo.SnapTo(_player.Position);

            _positionHistory.Clear();
            for (int i = 0; i < ECHO_HISTORY_LENGTH; i++)
                _positionHistory.Enqueue(_player.Position);

            // Потратить очки (20 очков за телепорт)
            _echoPoints = Math.Max(0, _echoPoints - 20);
        }

        private void HealPlayer()
        {
            // Хил за Echo-очки: 20 Echo = +30 HP
            const int cost = 20;
            const int heal = 30;
            if (_echoPoints < cost) return;
            if (_player.Health >= _player.MaxHealth) return;

            _echoPoints -= cost;
            _player.Health = Math.Min(_player.MaxHealth, _player.Health + heal);
        }

        private void Shoot()
        {
            Vector2 mousePos = Mouse.GetState().Position.ToVector2();
            Vector2 direction = mousePos - _player.Position;
            if (direction != Vector2.Zero) direction.Normalize();

            // Выпускаем 3 пули с небольшим разбросом
            for (int i = -1; i <= 1; i++)
            {
                Vector2 offset = new Vector2(i * 6, 0);
                Vector2 dir = direction;
                if (i != 0) dir = Vector2.Normalize(direction + new Vector2(i * 0.15f, 0));
                Bullet bullet = new Bullet(_player.Position + offset, dir, _player.Damage);
                _bullets.Add(bullet);
            }
            _player.ResetShootCooldown();
        }

        private void SpawnEnemy()
        {
            Random rand = new Random();
            Vector2 pos;
            int side = rand.Next(4);
            if (side == 0) pos = new Vector2(rand.Next(0, 1280), -50);
            else if (side == 1) pos = new Vector2(1280 + 50, rand.Next(0, 720));
            else if (side == 2) pos = new Vector2(rand.Next(0, 1280), 720 + 50);
            else pos = new Vector2(-50, rand.Next(0, 720));

            int type = rand.Next(3);
            Enemy enemy = new Enemy(pos, type);
            _enemies.Add(enemy);
        }

        private void SpawnEchoOrbs(Vector2 pos, int totalValue)
        {
            int count = Math.Min(3, totalValue);
            int valueEach = totalValue / count;
            for (int i = 0; i < count; i++)
            {
                _echoOrbs.Add(new EchoOrb(pos, valueEach));
            }
        }

        private void AddEchoPoints(int value)
        {
            _echoPoints += Math.Max(0, value);
        }

        private int GetTeleportsAvailable() => _echoPoints / 20;
        private int GetEchoProgress() => Math.Min(20, _echoPoints);

        private (Rectangle resume, Rectangle restart, Rectangle quit) GetMenuButtons()
        {
            if (_screen == Screen.GameOver)
            {
                Rectangle onlyRestart = new Rectangle(500, 340, 280, 60);
                return (Rectangle.Empty, onlyRestart, Rectangle.Empty);
            }

            Rectangle btnResume = new Rectangle(500, 270, 280, 50);
            Rectangle btnRestart = new Rectangle(500, 340, 280, 50);
            Rectangle btnQuit = new Rectangle(500, 410, 280, 50);
            return (btnResume, btnRestart, btnQuit);
        }

        private void UpdateRoulette(float deltaTime, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Escape) && !_prevKeyboard.IsKeyDown(Keys.Escape))
            {
                _screen = Screen.Playing;
                return;
            }

            if (keyboard.IsKeyDown(Keys.Left) && !_prevKeyboard.IsKeyDown(Keys.Left))
                _rouletteBetIndex = (_rouletteBetIndex + _rouletteBets.Length - 1) % _rouletteBets.Length;
            if (keyboard.IsKeyDown(Keys.Right) && !_prevKeyboard.IsKeyDown(Keys.Right))
                _rouletteBetIndex = (_rouletteBetIndex + 1) % _rouletteBets.Length;

            if (_rouletteResultTimer > 0)
                _rouletteResultTimer -= deltaTime;
            else
                _rouletteResultText = "";

            if (keyboard.IsKeyDown(Keys.Enter) && !_prevKeyboard.IsKeyDown(Keys.Enter))
            {
                int bet = _rouletteBets[_rouletteBetIndex];
                if (_echoPoints < bet)
                {
                    _rouletteResultText = "Not enough Echo points";
                    _rouletteResultTimer = 1.5f;
                    return;
                }

                _echoPoints -= bet;
                float roll = (float)_rand.NextDouble();
                if (roll < 0.50f)
                {
                    _rouletteResultText = $"Lost {bet}";
                }
                else if (roll < 0.80f)
                {
                    int win = bet * 2;
                    _echoPoints += win;
                    _rouletteResultText = $"Win x2: +{win}";
                }
                else if (roll < 0.95f)
                {
                    int win = bet * 3;
                    _echoPoints += win;
                    _rouletteResultText = $"Win x3: +{win}";
                }
                else
                {
                    ApplyRouletteJackpot();
                    _rouletteResultText = "JACKPOT: random upgrade!";
                }

                _rouletteResultTimer = 2.0f;
            }
        }

        private void ApplyRouletteJackpot()
        {
            int pick = _rand.Next(4);
            switch (pick)
            {
                case 0: _player.Damage += 3; break;
                case 1: _player.Speed += 30f; break;
                case 2: _player.Health = Math.Min(_player.MaxHealth, _player.Health + 40); break;
                case 3: _spawnInterval = Math.Min(2.0f, _spawnInterval + 0.15f); break; // чуть легче
            }
        }

        private void CheckUpgrades()
        {
            if (_kills >= _nextUpgradeKills)
            {
                ApplyUpgrade();
                _nextUpgradeKills += _upgradeStep;
                if (_upgradeStep < 20) _upgradeStep += 5;
            }
        }

        private void ApplyUpgrade()
        {
            int upgradeIndex = _kills / 5;
            switch (upgradeIndex % 5)
            {
                case 0: _player.Speed += 20f; break;
                case 1: _player.Damage += 5; break;
                case 2: _player.ShootCooldownMax = Math.Max(0.1f, _player.ShootCooldownMax - 0.05f); break;
                case 3: _player.Health = Math.Min(_player.MaxHealth, _player.Health + 20); break;
                case 4: _echoPoints += 20; break; // бесплатный телепорт (20 очков)
            }
        }

        private void RestartGame()
        {
            // Полный сброс состояния
            Initialize();
            LoadContent();
            _screen = Screen.Playing;
            _isMenuOpen = false;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();

            // Фон
            _spriteBatch.Draw(TextureManager.BackgroundDetailed, Vector2.Zero, Color.White);

            // Враги
            foreach (var e in _enemies) e.Draw(_spriteBatch);
            // Сферы эхо
            foreach (var orb in _echoOrbs) orb.Draw(_spriteBatch);
            // Пули
            foreach (var b in _bullets) b.Draw(_spriteBatch);
            // Вражеские пули
            foreach (var eb in _enemyBullets) eb.Draw(_spriteBatch);
            // "Эхо" игрока
            _echo?.Draw(_spriteBatch);
            // Игрок
            _player.Draw(_spriteBatch);

            // Интерфейс
            if (_screen == Screen.Playing)
                _ui.Draw(_spriteBatch, _kills, _echoPoints, GetEchoProgress(), GetTeleportsAvailable(), _player.Health, _player.MaxHealth, _nextUpgradeKills);

            // Пауза и Game Over (проверяем, что шрифт загружен)
            var font = _ui.GetFont();
            if (font != null)
            {
                if (_screen == Screen.GameOver)
                {
                    _spriteBatch.DrawString(font, "GAME OVER - Press R to restart", new Vector2(500, 360), Color.Red);
                }
                if (_screen == Screen.Roulette)
                {
                    DrawRouletteOverlay(font);
                }
            }

            if (_isMenuOpen)
            {
                DrawMenuOverlay(font);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawMenuOverlay(SpriteFont font)
        {
            // затемнение + панель
            _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(0, 0, 1280, 720), new Color(0, 0, 0, 180));
            Rectangle panel = new Rectangle(440, 180, 400, 360);
            _spriteBatch.Draw(TextureManager.Pixel, panel, new Color(10, 16, 18, 220));
            _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(panel.X, panel.Y, panel.Width, 3), new Color(0, 255, 255, 160));

            // кнопки (видимые всегда)
            var (btnResume, btnRestart, btnQuit) = GetMenuButtons();
            if (_screen != Screen.GameOver)
            {
                _spriteBatch.Draw(TextureManager.Pixel, btnResume, new Color(0, 255, 255, 40));
                _spriteBatch.Draw(TextureManager.Pixel, btnQuit, new Color(255, 80, 80, 25));
            }
            _spriteBatch.Draw(TextureManager.Pixel, btnRestart, new Color(255, 255, 255, 25));

            // подписи (если есть шрифт)
            if (font != null && _screen != Screen.GameOver)
            {
                _spriteBatch.DrawString(font, "MENU", new Vector2(600, 210), Color.Cyan);
                _spriteBatch.DrawString(font, "Enter - Resume", new Vector2(545, 285), Color.White);
                _spriteBatch.DrawString(font, "R - Restart", new Vector2(560, 355), Color.White);
                _spriteBatch.DrawString(font, "Q - Quit", new Vector2(575, 425), Color.White);
            }

            // Надписи на кнопках (работают даже без SpriteFont)
            if (_screen == Screen.GameOver)
            {
                DrawButtonText(btnRestart, "НАЧАТЬ СНОВА", Color.White, scale: 3);
            }
            else
            {
                DrawButtonText(btnResume, "ПРОДОЛЖИТЬ", Color.White, scale: 3);
                DrawButtonText(btnRestart, "НАЧАТЬ СНОВА", Color.White, scale: 3);
                DrawButtonText(btnQuit, "ВЫЙТИ", Color.White, scale: 3);
            }
        }

        private void DrawButtonText(Rectangle button, string text, Color color, int scale)
        {
            if (button == Rectangle.Empty) return;
            Vector2 size = TinyFont.Measure(text, scale);
            Vector2 pos = new Vector2(
                button.X + (button.Width - size.X) / 2f,
                button.Y + (button.Height - size.Y) / 2f
            );
            // тень/обводка для контраста
            TinyFont.Draw(_spriteBatch, text, pos + new Vector2(2, 2), new Color(0, 0, 0, 200), scale);
            TinyFont.Draw(_spriteBatch, text, pos, color, scale);
        }

        private void DrawRouletteOverlay(SpriteFont font)
        {
            // затемнение
            _spriteBatch.Draw(TextureManager.Pixel, new Rectangle(0, 0, 1280, 720), new Color(0, 0, 0, 180));

            _spriteBatch.DrawString(font, "ROULETTE", new Vector2(560, 210), Color.Gold);
            _spriteBatch.DrawString(font, $"Echo points: {_echoPoints}", new Vector2(520, 250), Color.Cyan);

            int bet = _rouletteBets[_rouletteBetIndex];
            _spriteBatch.DrawString(font, $"Bet: {bet}   (Left/Right to change)", new Vector2(450, 300), Color.White);
            _spriteBatch.DrawString(font, "Enter: Spin", new Vector2(560, 340), Color.White);
            _spriteBatch.DrawString(font, "Esc: Back", new Vector2(565, 370), Color.White);

            if (!string.IsNullOrWhiteSpace(_rouletteResultText))
                _spriteBatch.DrawString(font, _rouletteResultText, new Vector2(520, 430), Color.Yellow);
        }
    }
}