using System.Collections.Generic;
using UnityEngine;

namespace LumberJack.Wood
{
    /// <summary>
    /// Desenha com um LineRenderer o contorno de onde o corte vai passar.
    /// </summary>
    public class CutPreview
    {
        private const float OutlineInflate = 1.02f;
        private const int InitialOutlineCapacity = 64;

        private readonly LineRenderer _line;
        // Reaproveitada a cada frame para não alocar
        private readonly List<Vector3> _outline = new List<Vector3>(InitialOutlineCapacity);
        private bool _isVisible;

        public Color ValidColor { get; set; } = new Color(0.2f, 1f, 0.3f);
        public Color InvalidColor { get; set; } = new Color(1f, 0.25f, 0.2f);

        public CutPreview(LineRenderer line)
        {
            _line = line;
            _line.useWorldSpace = true;
            _line.loop = true;
            _line.positionCount = 0;
            _isVisible = false;
        }

        /// <summary>
        /// Cria um LineRenderer filho de parent já configurado para o preview.
        /// </summary>
        public static CutPreview Create(Transform parent, Material material, float width = 0.012f)
        {
            GameObject lineObject = new GameObject("Cut Preview");
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return new CutPreview(line);
        }

        public void Show(CuttableWood wood, Plane localPlane, bool valid)
        {
            MeshSlicer.GetCrossSection(wood.Vertices, wood.Triangles, localPlane, _outline);
            if (_outline.Count < 3)
            {
                Hide();
                return;
            }

            // Empurra a linha um pouquinho para fora da madeira para não sumir dentro da casca
            Vector3 center = Vector3.zero;
            foreach (Vector3 point in _outline) center += point;
            center /= _outline.Count;

            Transform woodTransform = wood.transform;
            _line.positionCount = _outline.Count;
            for (int i = 0; i < _outline.Count; i++)
            {
                Vector3 local = center + (_outline[i] - center) * OutlineInflate;
                _line.SetPosition(i, woodTransform.TransformPoint(local));
            }

            Color color = valid ? ValidColor : InvalidColor;
            _line.startColor = color;
            _line.endColor = color;
            _isVisible = true;
        }

        public void Hide()
        {
            // Chamado todo frame quando não há alvo: só mexe no LineRenderer se estava visível
            if (!_isVisible) return;

            _line.positionCount = 0;
            _isVisible = false;
        }
    }
}
