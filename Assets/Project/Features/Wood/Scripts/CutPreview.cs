using System.Collections.Generic;
using LumberJack.Core;
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
        private const float ProgressEdgeWidth = 0.001f;

        private static readonly Color ValidColor = new(0.2f, 1f, 0.3f);
        private static readonly Color InvalidColor = new(1f, 0.25f, 0.2f);
        private static readonly Color ProgressColor = new(1f, 0.55f, 0.1f);

        private readonly LineRenderer _line;
        // Reaproveitada a cada frame para não alocar
        private readonly List<Vector3> _outline = new(InitialOutlineCapacity);
        // Gradiente do progresso reaproveitado: só é remontado quando o progresso muda
        private readonly Gradient _progressGradient = new();
        private readonly GradientColorKey[] _progressColors = new GradientColorKey[4];
        private readonly GradientAlphaKey[] _progressAlphas = { new(1f, 0f), new(1f, 1f) };
        private bool _isVisible;
        private float _shownProgress = -1f;
        private bool _shownValid;

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
            GameObject lineObject = new("Cut Preview");
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return new CutPreview(line);
        }

        /// <summary><paramref name="progress"/> pinta essa fração do contorno com <see cref="ProgressColor"/>.</summary>
        public void Show(IAxeTarget target, Plane localPlane, bool valid, float progress = 0f)
        {
            MeshSlicer.GetCrossSection(target.Vertices, target.Triangles, localPlane, _outline);
            if (_outline.Count < 3)
            {
                Hide();
                return;
            }

            // Empurra a linha um pouquinho para fora da madeira para não sumir dentro da casca
            Vector3 center = Vector3.zero;
            foreach (Vector3 point in _outline) center += point;
            center /= _outline.Count;

            Transform woodTransform = target.Transform;
            _line.positionCount = _outline.Count;
            for (int i = 0; i < _outline.Count; i++)
            {
                Vector3 local = center + (_outline[i] - center) * OutlineInflate;
                _line.SetPosition(i, woodTransform.TransformPoint(local));
            }

            SetColors(valid, progress);
            _isVisible = true;
        }

        private void SetColors(bool valid, float progress)
        {
            if (Mathf.Approximately(progress, _shownProgress) && valid == _shownValid) return;

            _shownProgress = progress;
            _shownValid = valid;
            Color color = valid ? ValidColor : InvalidColor;
            if (progress <= 0f)
            {
                _line.colorGradient = SolidGradient(color);
                return;
            }

            // Cor do progresso até "progress" e a cor normal no resto, com a troca seca
            float edge = Mathf.Clamp01(progress);
            _progressColors[0] = new GradientColorKey(ProgressColor, 0f);
            _progressColors[1] = new GradientColorKey(ProgressColor, edge);
            _progressColors[2] = new GradientColorKey(color, Mathf.Min(1f, edge + ProgressEdgeWidth));
            _progressColors[3] = new GradientColorKey(color, 1f);
            _progressGradient.SetKeys(_progressColors, _progressAlphas);
            _line.colorGradient = _progressGradient;
        }

        private Gradient SolidGradient(Color color)
        {
            for (int i = 0; i < _progressColors.Length; i++) _progressColors[i] = new GradientColorKey(color, i / 3f);
            _progressGradient.SetKeys(_progressColors, _progressAlphas);
            return _progressGradient;
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
