using UnityEngine;

namespace LumberJack.Inventory
{
    /// <summary>
    /// Dados de um item que vai para a hotbar: nome, ícone e o prefab que a mão segura.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Lumber Jack/Inventory/Item")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private Texture2D _icon;
        [Tooltip("Prefab instanciado na mão quando o item está selecionado na hotbar")]
        [SerializeField] private GameObject _prefab;
        [TextArea]
        [SerializeField] private string _description;

        public string DisplayName => _displayName;
        public Texture2D Icon => _icon;
        public GameObject Prefab => _prefab;
        public string Description => _description;
    }
}
