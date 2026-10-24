using UnityEngine;

namespace LumberJack.Core
{
    /// <summary>
    /// Gerador aleatório determinístico (xorshift32): a mesma seed gera a mesma sequência em qualquer
    /// máquina, então todos os jogadores geram a mesma árvore/loot a partir da seed. É um struct sem
    /// alocação; passe por <c>ref</c> para a sequência avançar.
    /// </summary>
    public struct SeededRandom
    {
        private const float UIntToUnitFloat = 1f / 16777216f; // 2^24: só os 24 bits que cabem num float
        private const uint ZeroSeedReplacement = 0x9E3779B9u;

        private uint _state;

        public SeededRandom(uint seed)
        {
            // Mistura a seed (seeds próximas como 1, 2, 3 viram sequências bem diferentes); xorshift não aceita 0
            _state = Hash(seed);
            if (_state == 0u) _state = ZeroSeedReplacement;
        }

        public SeededRandom(int seed) : this(unchecked((uint)seed))
        {
        }

        public uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }

        /// <summary>Valor em [0, 1).</summary>
        public float NextFloat()
        {
            return (NextUInt() >> 8) * UIntToUnitFloat;
        }

        /// <summary>Valor em [min, max).</summary>
        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        /// <summary>Inteiro em [min, max] (max incluso).</summary>
        public int RangeInclusive(int min, int max)
        {
            if (max <= min) return min;
            return min + (int)(NextUInt() % (uint)(max - min + 1));
        }

        /// <summary>Valor em [-1, 1).</summary>
        public float NextSigned()
        {
            return NextFloat() * 2f - 1f;
        }

        /// <summary>Ângulo em radianos em [0, 2pi).</summary>
        public float NextAngle()
        {
            return NextFloat() * Mathf.PI * 2f;
        }

        /// <summary>Deriva uma seed nova e estável a partir de uma seed e um índice (ex.: seed por galho).</summary>
        public static uint Combine(uint seed, uint index)
        {
            return Hash(seed ^ (index * 0x27D4EB2Du));
        }

        // Wang hash
        private static uint Hash(uint value)
        {
            value = (value ^ 61u) ^ (value >> 16);
            value *= 9u;
            value ^= value >> 4;
            value *= 0x27D4EB2Du;
            value ^= value >> 15;
            return value;
        }
    }
}
