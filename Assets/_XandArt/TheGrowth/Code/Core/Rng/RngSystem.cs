using System;
using System.Collections.Generic;
using TheGrowth.Core.Saves;
using UnityEngine;

namespace TheGrowth.Core.Rng
{
    /// <summary>
    /// ГСЧ сессии (система сессии, секция <c>rng</c>): один генератор <see cref="Xoshiro128"/> на сессию — раскладка, узлы
    /// <c>random</c> и <c>chance</c> скриптов, позже диалоги. Состояние пишется в сейв, поэтому после загрузки те же действия
    /// дают тот же результат. Новая игра (секции нет) — seed <see cref="NewGameSeed"/> или случайный (энтропия); секция с
    /// неверным состоянием — предупреждение и то же, что в новой игре.
    /// </summary>
    public sealed class RngSystem : SaveableSystem<RngSave>
    {
        public const string Key = "rng";

        private const string LogSource = "[Saves] Section '" + Key + "'";

        private Xoshiro128 m_Generator;

        public override string SaveKey => Key;

        /// <summary>
        /// Seed новой игры; null — случайный. Задаётся до чтения сейва (<c>configure</c> в <c>Session.Create</c>): в тестах —
        /// фиксированный. Загруженное состояние не меняет.
        /// </summary>
        public ulong? NewGameSeed { get; set; }

        private Xoshiro128 Generator => m_Generator ?? throw new InvalidOperationException($"{nameof(RngSystem)} is not read: it is used before the session is created");

        public uint NextUInt()
        {
            return Generator.NextUInt();
        }

        /// <summary>Целое от <paramref name="min"/> до <paramref name="max"/> включительно; пустой диапазон — <see cref="ArgumentException"/>.</summary>
        public int Range(int min, int max)
        {
            return Generator.Range(min, max);
        }

        /// <summary>Индекс от 0 до <paramref name="count"/> − 1; <paramref name="count"/> меньше 1 — <see cref="ArgumentOutOfRangeException"/>.</summary>
        public int Index(int count)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Count of items to choose from is at least 1");

            return Generator.Range(0, count - 1);
        }

        /// <summary>
        /// Шанс <paramref name="percent"/> из 100: одно число 0..99 меньше <paramref name="percent"/>. Число берётся всегда, поэтому
        /// смена шанса не сдвигает остальные результаты; 0 и меньше — никогда, 100 и больше — всегда.
        /// </summary>
        public bool Chance(int percent)
        {
            return Generator.Range(0, 99) < percent;
        }

        protected override RngSave Write()
        {
            return new RngSave { State = new List<uint>(Generator.GetState()) };
        }

        protected override void Read(RngSave save)
        {
            if (Xoshiro128.TryRestore(save.State, out var generator))
            {
                m_Generator = generator;
                return;
            }

            Debug.LogWarning($"{LogSource}: the generator state is invalid ({save.State?.Count ?? 0} numbers, 4 non-zero expected), a new seed is used");
            ReadDefaults();
        }

        protected override void ReadDefaults()
        {
            m_Generator = new Xoshiro128(NewGameSeed ?? CreateSeed());
        }

        /// <summary>Случайный seed: 122 случайных бита <see cref="Guid.NewGuid"/> (есть на всех платформах, включая Web).</summary>
        private static ulong CreateSeed()
        {
            var bytes = Guid.NewGuid().ToByteArray();
            return BitConverter.ToUInt64(bytes, 0) ^ BitConverter.ToUInt64(bytes, 8);
        }
    }
}
