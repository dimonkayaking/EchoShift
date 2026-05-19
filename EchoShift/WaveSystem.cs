using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace EchoShift
{
    public class WaveSystem
    {
        public int CurrentWave { get; private set; }
        public int HighScoreWave { get; set; }
        public int[] EnemiesTotalByType { get; private set; }
        private int[] EnemiesRemainingToSpawn { get; set; }
        public bool IsWaveInProgress { get; private set; }
        public bool IsWaveComplete { get; private set; }

        private float _waveStartTimer;
        private float _waveStartDelay = 1f;
        private readonly Random _rand = new Random();

        public WaveSystem()
        {
            CurrentWave = 1;
            EnemiesTotalByType = new int[3];
            EnemiesRemainingToSpawn = new int[3];
            IsWaveInProgress = false;
            IsWaveComplete = false;
        }

        public void StartWave()
        {
            IsWaveInProgress = true;
            IsWaveComplete = false;
            _waveStartTimer = _waveStartDelay;

            var totalEnemies = 0;
            if (CurrentWave == 1) totalEnemies = 5;
            else if (CurrentWave == 2) totalEnemies = 8;
            else if (CurrentWave == 3) totalEnemies = 12;
            else if (CurrentWave == 4) totalEnemies = 18;
            else if (CurrentWave == 5) totalEnemies = 25;
            else if (CurrentWave == 6) totalEnemies = 35;
            else if (CurrentWave == 7) totalEnemies = 45;
            else if (CurrentWave == 8) totalEnemies = 60;
            else if (CurrentWave == 9) totalEnemies = 75;
            else totalEnemies = 100 + (CurrentWave - 10) * 15;
            totalEnemies = MathHelper.Min(totalEnemies, 300);

            var planned = new int[3];
            if (CurrentWave >= 2) planned[1] = 1;
            if (CurrentWave >= 3) planned[2] = 1;

            var remaining = totalEnemies - (planned[1] + planned[2]);
            if (remaining < 0) remaining = totalEnemies;

            var red = remaining * 60 / 100;
            var orange = remaining * 30 / 100;
            var purple = remaining - red - orange;

            planned[0] += red;
            planned[1] += orange;
            planned[2] += purple;

            if (CurrentWave >= 2 && planned[1] == 0 && planned[0] > 0)
            {
                planned[1] = 1;
                planned[0]--;
            }
            if (CurrentWave >= 3 && planned[2] == 0 && planned[0] + planned[1] > 0)
            {
                planned[2] = 1;
                if (planned[0] > 0) planned[0]--;
                else planned[1]--;
            }

            for (var i = 0; i < 3; i++)
            {
                EnemiesTotalByType[i] = planned[i];
                EnemiesRemainingToSpawn[i] = planned[i];
            }
        }

        public void Update(float deltaTime, List<Enemy> enemies)
        {
            if (!IsWaveInProgress) return;
            if (_waveStartTimer > 0)
            {
                _waveStartTimer -= deltaTime;
                return;
            }
            if (enemies.Count == 0 && EnemiesTotalByType[0] == 0 && EnemiesTotalByType[1] == 0 && EnemiesTotalByType[2] == 0 && !IsWaveComplete)
                CompleteWave();
        }

        public void OnEnemyKilled(int type)
        {
            if (type >= 0 && type < EnemiesTotalByType.Length && EnemiesTotalByType[type] > 0)
                EnemiesTotalByType[type]--;
        }

        public void OnEnemySpawned(int type)
        {
            if (type >= 0 && type < EnemiesRemainingToSpawn.Length && EnemiesRemainingToSpawn[type] > 0)
                EnemiesRemainingToSpawn[type]--;
        }

        public bool HasEnemiesToSpawn()
        {
            return EnemiesRemainingToSpawn[0] > 0 || EnemiesRemainingToSpawn[1] > 0 || EnemiesRemainingToSpawn[2] > 0;
        }

        public int GetNextEnemyType()
        {
            var total = EnemiesRemainingToSpawn[0] + EnemiesRemainingToSpawn[1] + EnemiesRemainingToSpawn[2];
            if (total == 0) return 0;
            var r = _rand.Next(total);
            if (r < EnemiesRemainingToSpawn[0]) return 0;
            if (r < EnemiesRemainingToSpawn[0] + EnemiesRemainingToSpawn[1]) return 1;
            return 2;
        }

        private void CompleteWave()
        {
            IsWaveInProgress = false;
            IsWaveComplete = true;
            CurrentWave++;
            if (CurrentWave - 1 > HighScoreWave)
                HighScoreWave = CurrentWave - 1;
        }

        public void NextWave()
        {
            StartWave();
        }

        public void ResetWave()
        {
            CurrentWave = 1;
            EnemiesTotalByType = new int[3];
            EnemiesRemainingToSpawn = new int[3];
            IsWaveInProgress = false;
            IsWaveComplete = false;
            _waveStartTimer = 0;
        }

        public float GetSpawnInterval()
        {
            var interval = 2.0f - (CurrentWave - 1) * 0.12f;
            return MathHelper.Max(0.4f, interval);
        }
    }
}