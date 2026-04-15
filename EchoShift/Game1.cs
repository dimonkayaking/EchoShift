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
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private UI _ui;

        private Player _player;
        private List<Enemy> _enemies;
        private List<Bullet> _bullets;
        private List<EchoOrb> _echoOrbs;
        private List<EchoDecoy> _decoys;
        private int _kills;
        private int _echoCharge;     // 0..20 (текущая шкала одного заряда)
        private int _echoCharges;    // 0..5 (количество доступных зарядов)
        private float _spawnTimer;
        private float _spawnInterval;
        private float _gameTimer;
        private bool _isPaused;
        private bool _isGameOver;

        // История для отката (3 секунды при 60 FPS = 180 записей)
        private Queue<Vector2> _positionHistory;
        private Queue<int> _healthHistory;
        private const int HISTORY_LENGTH = 180;

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
            _echoOrbs = new List<EchoOrb>();
            _decoys = new List<EchoDecoy>();
            _kills = 0;
            _echoCharge = 0;
            _echoCharges = 0;
            _spawnTimer = 0f;
            _spawnInterval = 2.0f;
            _gameTimer = 0f;
            _isPaused = false;
            _isGameOver = false;

            // Очереди истории
            _positionHistory = new Queue<Vector2>();
            _healthHistory = new Queue<int>();

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
            for (int i = 0; i < HISTORY_LENGTH; i++)
            {
                _positionHistory.Enqueue(_player.Position);
                _healthHistory.Enqueue(_player.Health);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (deltaTime > 0.033f) deltaTime = 0.033f;

            KeyboardState keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.Escape))
                _isPaused = !_isPaused;

            if (_isGameOver)
            {
                if (keyboard.IsKeyDown(Keys.R))
                    RestartGame();
                return;
            }
            if (_isPaused) return;

            // Сохраняем историю позиций и здоровья
            _positionHistory.Enqueue(_player.Position);
            _healthHistory.Enqueue(_player.Health);
            if (_positionHistory.Count > HISTORY_LENGTH) _positionHistory.Dequeue();
            if (_healthHistory.Count > HISTORY_LENGTH) _healthHistory.Dequeue();

            // Обновление игрока
            _player.Update(deltaTime, Mouse.GetState(), keyboard);

            // Активация способности "Сдвиг Эха" (Пробел)
            if (keyboard.IsKeyDown(Keys.Space) && !_player.IsShiftActive && _echoCharges > 0)
            {
                ActivateEchoShift();
            }

            // Стрельба (ЛКМ)
            if (Mouse.GetState().LeftButton == ButtonState.Pressed && _player.CanShoot(deltaTime))
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

            // Обновление врагов, столкновения с пулями и игроком
            for (int i = 0; i < _enemies.Count; i++)
            {
                // Цель врага: если есть активная копия, то она, иначе игрок
                Vector2? decoyPos = _decoys.Count > 0 ? _decoys[0].Position : (Vector2?)null;
                _enemies[i].Update(deltaTime, _player.Position, decoyPos);

                // Столкновение с игроком
                if (_enemies[i].CollidesWith(_player) && !_player.IsInvincible)
                {
                    _player.TakeDamage(_enemies[i].Damage);
                    if (_player.Health <= 0)
                        _isGameOver = true;
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
                    AddEchoCharge(_echoOrbs[i].Value);
                    _echoOrbs.RemoveAt(i);
                    i--;
                }
            }

            // Обновление призрачных копий и их взрыв
            for (int i = 0; i < _decoys.Count; i++)
            {
                _decoys[i].Update(deltaTime);
                if (_decoys[i].IsExploded)
                {
                    ExplodeDecoy(_decoys[i]);
                    _decoys.RemoveAt(i);
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
            base.Update(gameTime);
        }

        private void ActivateEchoShift()
        {
            if (_positionHistory.Count < HISTORY_LENGTH) return;
            Vector2[] posArray = _positionHistory.ToArray();
            int[] healthArray = _healthHistory.ToArray();
            Vector2 oldPos = posArray[HISTORY_LENGTH - 1];
            int oldHealth = healthArray[HISTORY_LENGTH - 1];

            // Создать призрачную копию на текущей позиции
            EchoDecoy decoy = new EchoDecoy(_player.Position, TextureManager.DecoyTex, 2.0f);
            _decoys.Add(decoy);

            // Откат игрока
            _player.Position = oldPos;
            _player.Health = oldHealth;
            _player.IsShiftActive = true;

            // Потратить один заряд
            _echoCharges--;
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

        private void AddEchoCharge(int value)
        {
            _echoCharge += value;
            while (_echoCharge >= 20 && _echoCharges < 5)
            {
                _echoCharge -= 20;
                _echoCharges++;
            }
            if (_echoCharge > 20) _echoCharge = 20;
        }

        private void ExplodeDecoy(EchoDecoy decoy)
        {
            float radius = 150f;
            foreach (var enemy in _enemies)
            {
                if (Vector2.Distance(enemy.Position, decoy.Position) <= radius)
                {
                    enemy.TakeDamage(50);
                }
            }
            _enemies.RemoveAll(e => e.IsDead);
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
                case 3: _player.Health = Math.Min(200, _player.Health + 20); break;
                case 4: _echoCharges = Math.Min(5, _echoCharges + 1); break;
            }
        }

        private void RestartGame()
        {
            // Полный сброс состояния
            Initialize();
            LoadContent();
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
            // Призрачные копии
            foreach (var d in _decoys) d.Draw(_spriteBatch);
            // Игрок
            _player.Draw(_spriteBatch);

            // Интерфейс
            _ui.Draw(_spriteBatch, _kills, _echoCharge, _echoCharges, _player.Health, 200, _nextUpgradeKills);

            // Пауза и Game Over (проверяем, что шрифт загружен)
            var font = _ui.GetFont();
            if (font != null)
            {
                if (_isPaused)
                {
                    _spriteBatch.DrawString(font, "PAUSED", new Vector2(600, 360), Color.White);
                }
                if (_isGameOver)
                {
                    _spriteBatch.DrawString(font, "GAME OVER - Press R to restart", new Vector2(500, 360), Color.Red);
                }
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}