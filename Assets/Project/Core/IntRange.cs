using System;
using UnityEngine;

namespace LumberJack.Core
{
    /// <summary>
    /// Intervalo inteiro [min, max] (os dois inclusos) editável no inspector.
    /// </summary>
    [Serializable]
    public struct IntRange
    {
        [SerializeField] private int _min;
        [SerializeField] private int _max;

        public IntRange(int min, int max)
        {
            _min = min;
            _max = max;
        }

        public int Min => _min;
        public int Max => _max;

        /// <summary>Sorteia um valor dentro do intervalo (max incluso).</summary>
        public int Pick(ref SeededRandom random)
        {
            return random.RangeInclusive(_min, _max);
        }

        /// <summary>Garante min &lt;= max e os dois dentro de [lower, upper].</summary>
        public IntRange Clamped(int lower, int upper)
        {
            int min = Mathf.Clamp(_min, lower, upper);
            int max = Mathf.Clamp(_max, min, upper);
            return new IntRange(min, max);
        }
    }
}
