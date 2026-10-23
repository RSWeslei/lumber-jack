using UnityEngine.UIElements;

namespace LumberJack.Inventory
{
    /// <summary>
    /// Um espaco da hotbar (elemento do UI Toolkit) que mostra o icone do item guardado.
    /// </summary>
    [UxmlElement]
    public partial class Slot : VisualElement
    {
        private const string SlotClass = "hotbar__slot";
        private const string IconClass = "hotbar__slot-icon";
        private const string IconName = "icon";

        private readonly Image _icon;

        public ItemDefinition Item { get; private set; }
        public bool IsEmpty => Item == null;

        public Slot()
        {
            AddToClassList(SlotClass);
            _icon = new Image { name = IconName };
            _icon.AddToClassList(IconClass);
            Add(_icon);
        }

        public void SetItem(ItemDefinition item)
        {
            Item = item;
            _icon.image = item.Icon;
        }

        public void ClearItem()
        {
            Item = null;
            _icon.image = null;
        }
    }
}
