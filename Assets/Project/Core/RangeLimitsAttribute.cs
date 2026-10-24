using UnityEngine;

namespace LumberJack.Core
{
    /// <summary>
    /// Limites do slider de min/max de um <see cref="FloatRange"/> ou <see cref="IntRange"/> no inspector.
    /// </summary>
    public class RangeLimitsAttribute : PropertyAttribute
    {
        public RangeLimitsAttribute(float lower, float upper)
        {
            Lower = lower;
            Upper = upper;
        }

        public float Lower { get; }
        public float Upper { get; }
    }
}
