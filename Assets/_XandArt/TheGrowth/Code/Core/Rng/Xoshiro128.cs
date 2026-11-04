using System;
using System.Collections.Generic;

namespace TheGrowth.Core.Rng
{
    /// <summary>
    /// Генератор xoshiro128** 1.1 (Blackman, Vigna): состояние — четыре <c>uint</c>, только целочисленная арифметика без
    /// переполнения-исключений, поэтому последовательность одинакова на всех платформах. Seed (64 бита) разворачивается в
    /// состояние двумя шагами SplitMix64. Нулевое состояние недопустимо (генератор выдаёт одни нули).
    /// </summary>
    public sealed class Xoshiro128
    {
        public const int StateLength = 4;

        private uint m_S0;
        private uint m_S1;
        private uint m_S2;
        private uint m_S3;

        public Xoshiro128(ulong seed)
        {
            var a = SplitMix64(ref seed);
            var b = SplitMix64(ref seed);
            m_S0 = (uint)a;
            m_S1 = (uint)(a >> 32);
            m_S2 = (uint)b;
            m_S3 = (uint)(b >> 32);
            if ((m_S0 | m_S1 | m_S2 | m_S3) == 0)
                m_S0 = 1;
        }

        private Xoshiro128(uint s0, uint s1, uint s2, uint s3)
        {
            m_S0 = s0;
            m_S1 = s1;
            m_S2 = s2;
            m_S3 = s3;
        }

        /// <summary>Генератор из сохранённого состояния (<see cref="GetState"/>); длина не 4 или все нули — false.</summary>
        public static bool TryRestore(IReadOnlyList<uint> state, out Xoshiro128 generator)
        {
            generator = null;
            if (state is not { Count: StateLength } || (state[0] | state[1] | state[2] | state[3]) == 0)
                return false;

            generator = new Xoshiro128(state[0], state[1], state[2], state[3]);
            return true;
        }

        /// <summary>Состояние для сейва: продолжение с него даёт ту же последовательность.</summary>
        public uint[] GetState()
        {
            return new[] { m_S0, m_S1, m_S2, m_S3 };
        }

        public uint NextUInt()
        {
            var result = RotateLeft(m_S1 * 5, 7) * 9;
            var t = m_S1 << 9;
            m_S2 ^= m_S0;
            m_S3 ^= m_S1;
            m_S1 ^= m_S2;
            m_S0 ^= m_S3;
            m_S2 ^= t;
            m_S3 = RotateLeft(m_S3, 11);
            return result;
        }

        /// <summary>
        /// Равномерное целое от <paramref name="min"/> до <paramref name="max"/> включительно: отбрасывание значений ниже
        /// 2³² mod n (без смещения). <paramref name="min"/> больше <paramref name="max"/> — <see cref="ArgumentException"/>.
        /// </summary>
        public int Range(int min, int max)
        {
            if (min > max)
                throw new ArgumentException($"The range {min}..{max} is empty", nameof(max));

            var range = (uint)((long)max - min + 1);
            if (range == 0)
                return (int)NextUInt();

            var threshold = (uint)((1UL << 32) % range);
            while (true)
            {
                var value = NextUInt();
                if (value >= threshold)
                    return (int)(min + (long)(value % range));
            }
        }

        private static uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }

        private static ulong SplitMix64(ref ulong state)
        {
            state += 0x9E3779B97F4A7C15UL;
            var z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
