using UnityEngine;

namespace LumberJack.Core
{
    /// <summary>Regras de corte iguais para tudo o que o machado corta (toras, árvores em pé, pedaços caídos).</summary>
    public static class AxeCutRules
    {
        /// <summary>Os dois lados do corte ficam com pelo menos <paramref name="minThickness"/> (m) da parte mirada?</summary>
        public static bool LeavesMinThickness(IAxeTarget target, Plane localPlane, float minThickness)
        {
            Vector3[] vertices = target.Vertices;
            int[] triangles = target.Triangles;
            if (triangles.Length == 0) return false;

            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (int index in triangles)
            {
                float distance = localPlane.GetDistanceToPoint(vertices[index]);
                min = Mathf.Min(min, distance);
                max = Mathf.Max(max, distance);
            }
            float scale = target.Transform.lossyScale.x;
            return -min * scale >= minThickness && max * scale >= minThickness;
        }
    }
}
