using LumberJack.Inventory;
using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Segura o item equipado na hotbar: instancia o prefab do item como filho e o deixa
    /// sem fisica e sem colisao.
    /// </summary>
    public class Hand : MonoBehaviour
    {
        private GameObject _equippedObject;

        public ItemDefinition EquippedItem { get; private set; }
        public bool IsItemEquipped => _equippedObject != null;

        public void LoadItem(ItemDefinition item)
        {
            UnloadItemAndDestroy();

            if (item == null || item.Prefab == null)
            {
                UnloadItem();
                return;
            }

            EquippedItem = item;

            GameObject itemObject = Instantiate(item.Prefab, transform);
            itemObject.transform.localPosition = Vector3.zero;
            itemObject.transform.localRotation = Quaternion.identity;
            itemObject.transform.localScale = Vector3.one;
            itemObject.AddComponent<EquippedItemSway>();

            Rigidbody itemBody = itemObject.GetComponent<Rigidbody>();
            if (itemBody != null) itemBody.isKinematic = true;

            // Item na mao nao colide nem bloqueia os raycasts da camera
            foreach (Collider itemCollider in itemObject.GetComponentsInChildren<Collider>())
            {
                itemCollider.enabled = false;
            }

            _equippedObject = itemObject;
        }

        public Animator GetEquippedAnimator()
        {
            return IsItemEquipped ? _equippedObject.GetComponent<Animator>() : null;
        }

        public void UnloadItem()
        {
            EquippedItem = null;
            if (_equippedObject != null) _equippedObject.SetActive(false);
        }

        public void UnloadItemAndDestroy()
        {
            EquippedItem = null;
            if (_equippedObject != null) Destroy(_equippedObject);
        }
    }
}
