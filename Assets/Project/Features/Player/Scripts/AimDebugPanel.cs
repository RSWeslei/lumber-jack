using System.Collections.Generic;
using System.Text;
using LumberJack.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumberJack.Player
{
    /// <summary>
    /// Painel de debug no canto da tela com os dados do que está na mira (árvore, pedaço, madeira): só no Editor
    /// e em builds de desenvolvimento. Monta o texto poucas vezes por segundo (aloca, mas é só debug).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class AimDebugPanel : MonoBehaviour
    {
        private const string LabelName = "debug-info";
        private const string WoodLayerName = "Wood";

        [SerializeField] private PlayerAim _aim;
        [Tooltip("Atualizações do texto por segundo")]
        [Min(1f)]
        [SerializeField] private float _refreshRate = 4f;

        private readonly StringBuilder _text = new();
        private readonly List<IDebugInfo> _sources = new();
        private Label _label;
        private int _woodMask;
        private float _nextRefresh;

        private void Awake()
        {
            _label = GetComponent<UIDocument>().rootVisualElement.Q<Label>(LabelName);
            _woodMask = LayerMask.GetMask(WoodLayerName);
            if (Debug.isDebugBuild) return;

            _label.style.display = DisplayStyle.None;
            enabled = false;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;

            _nextRefresh = Time.unscaledTime + 1f / _refreshRate;
            Refresh();
        }

        private void Refresh()
        {
            _text.Clear();
            if (_aim.TryFindTarget(_woodMask, out RaycastHit hit))
            {
                Rigidbody body = hit.rigidbody;
                bool hasBody = !ReferenceEquals(body, null);
                GameObject target = hasBody ? body.gameObject : hit.collider.gameObject;
                _text.AppendLine(target.name);
                if (hasBody) _text.Append("Massa: ").Append(body.mass.ToString("0.0")).AppendLine(" kg");

                target.GetComponents(_sources);
                foreach (IDebugInfo source in _sources) source.AppendDebugInfo(_text);
            }

            bool hasText = _text.Length > 0;
            _label.style.display = hasText ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasText) _label.text = _text.ToString().TrimEnd();
        }
    }
}
