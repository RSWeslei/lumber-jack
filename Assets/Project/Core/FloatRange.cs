using System;
using UnityEngine;

namespace LumberJack.Core
{
    /// <summary>
    /// Intervalo [min, max] editável no inspector (com slider se tiver <see cref="RangeLimitsAttribute"/>).
    /// </summary>
    [Serializable]
    public struct FloatRange
    {
        [SerializeField] private float _min;
        [SerializeField] private float _max;

        public FloatRange(float min, float max)
        {
            _min = min;
            _max = max;
        }

        public float Min => _min;
        public float Max => _max;

        public float Lerp(float t)
        {
            return Mathf.Lerp(_min, _max, t);
        }

        /// <summary>Sorteia um valor dentro do intervalo.</summary>
        public float Pick(ref SeededRandom random)
        {
            return Lerp(random.NextFloat());
        }

        /// <summary>Garante min &lt;= max e os dois dentro de [lower, upper].</summary>
        public FloatRange Clamped(float lower, float upper)
        {
            float min = Mathf.Clamp(_min, lower, upper);
            float max = Mathf.Clamp(_max, min, upper);
            return new FloatRange(min, max);
        }
    }
}
