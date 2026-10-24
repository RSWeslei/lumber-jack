using LumberJack.Inventory;
using UnityEngine;

namespace LumberJack.Tools
{
    /// <summary>
    /// Item que é uma ferramenta (machado...). Define o que ela consegue fazer.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTool", menuName = "Lumber Jack/Tools/Tool")]
    public class ToolDefinition : ItemDefinition
    {
        [Tooltip("A ferramenta corta madeira (machados)")]
        [SerializeField] private bool _canCutWood;
        [Tooltip("Dano por golpe em árvores em pé")]
        [Min(0f)]
        [SerializeField] private float _chopDamage = 1f;

        public bool CanCutWood => _canCutWood;
        public float ChopDamage => _chopDamage;
    }
}
