using LumberJack.Inventory;
using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Segura o item equipado na hotbar: instancia o prefab do item como filho e o deixa
    /// sem física e sem colisão.
    /// </summary>
    public class Hand : MonoBehaviour
    {
        private GameObject _equippedObject;

        public ItemDefinition EquippedItem { get; private set; }
        /// <summary>Tem um item equipado na mão.</summary>
        public bool HasItem { get; private set; }

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
            // Na camada da mão: a câmera dos braços desenha o item por cima do cenário
            SetLayerRecursively(itemObject.transform, gameObject.layer);

            Rigidbody itemBody = itemObject.GetComponent<Rigidbody>();
            if (itemBody != null) itemBody.isKinematic = true;

            // Item na mão não colide nem bloqueia os raycasts da câmera
            foreach (Collider itemCollider in itemObject.GetComponentsInChildren<Collider>())
            {
                itemCollider.enabled = false;
            }

            _equippedObject = itemObject;
            HasItem = true;
        }

        public void UnloadItemAndDestroy()
        {
            EquippedItem = null;
            HasItem = false;
            if (_equippedObject != null) Destroy(_equippedObject);
        }

        private void UnloadItem()
        {
            EquippedItem = null;
            HasItem = false;
            if (_equippedObject != null) _equippedObject.SetActive(false);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root) SetLayerRecursively(child, layer);
        }
    }
}
