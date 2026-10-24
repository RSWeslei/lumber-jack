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

        public bool CanCutWood => _canCutWood;
    }
}
