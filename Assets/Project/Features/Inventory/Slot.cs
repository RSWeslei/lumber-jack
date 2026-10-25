using UnityEngine.UIElements;

namespace LumberJack.Inventory
{
    /// <summary>
    /// Um espaço da hotbar (elemento do UI Toolkit) que mostra o ícone do item guardado,
    /// a tecla do slot e o tracinho de selecionado (o visual fica todo no Hotbar.uss).
    /// </summary>
    [UxmlElement]
    public partial class Slot : VisualElement
    {
        private const string SlotClass = "hotbar__slot";
        private const string FilledClass = "hotbar__slot--filled";
        private const string IconClass = "hotbar__slot-icon";
        private const string KeyClass = "hotbar__slot-key";
        private const string IndicatorClass = "hotbar__slot-indicator";
        private const string IconName = "icon";

        private readonly Image _icon;
        private readonly Label _key;

        public ItemDefinition Item { get; private set; }
        public bool IsEmpty => Item == null;

        public Slot()
        {
            AddToClassList(SlotClass);
            pickingMode = PickingMode.Ignore;

            _icon = new Image { name = IconName, pickingMode = PickingMode.Ignore };
            _icon.AddToClassList(IconClass);
            Add(_icon);

            _key = new Label { pickingMode = PickingMode.Ignore };
            _key.AddToClassList(KeyClass);
            Add(_key);

            var indicator = new VisualElement { pickingMode = PickingMode.Ignore };
            indicator.AddToClassList(IndicatorClass);
            Add(indicator);
        }

        /// <summary>Texto da tecla que seleciona este slot (canto superior esquerdo).</summary>
        public void SetKeyLabel(string keyLabel)
        {
            _key.text = keyLabel;
        }

        public void SetItem(ItemDefinition item)
        {
            Item = item;
            _icon.image = item.Icon;
            EnableInClassList(FilledClass, true);
        }
    }
}
