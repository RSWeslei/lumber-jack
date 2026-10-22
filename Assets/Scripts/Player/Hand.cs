using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    [SerializeField] private bool _isLeftHand;
    private GameObject _currentEquippedItemGO;

    public Item EquippedItem { get; private set; }

    public void LoadItem(Item item)
    {
        UnloadItemAndDestroy();

        if (item?.itemPrefab == null)
        {
            UnloadItem();
            return;
        }
        EquippedItem = item;

        GameObject itemObject = Instantiate(item.itemPrefab, transform);
        itemObject.transform.localPosition = Vector3.zero;
        itemObject.transform.localRotation = Quaternion.identity;
        itemObject.transform.localScale = Vector3.one;

        itemObject.AddComponent<EquippedItemSway>();
        
        Rigidbody itemRigidbody = itemObject.GetComponent<Rigidbody>();
        if (itemRigidbody != null)
        {
            itemRigidbody.isKinematic = true;
        }

        // Item na mao nao colide nem bloqueia os raycasts da camera
        foreach (Collider itemCollider in itemObject.GetComponentsInChildren<Collider>())
        {
            itemCollider.enabled = false;
        }

        _currentEquippedItemGO = itemObject;
    }

    public bool IsItemEquiped => _currentEquippedItemGO != null;

    public Animator GetWieldedAnimator() 
    {
        if (!IsItemEquiped) return null;
        return _currentEquippedItemGO.GetComponent<Animator>() ?? null;
    }

    public void UnloadItem()
    {
        EquippedItem = null;
        if (_currentEquippedItemGO != null){
            _currentEquippedItemGO.SetActive(false);
        }
    }

    public void UnloadItemAndDestroy()
    {
        EquippedItem = null;
        if (_currentEquippedItemGO != null){
            Destroy(_currentEquippedItemGO);
        }
    }
}
