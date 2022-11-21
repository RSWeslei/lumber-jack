using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    [SerializeField] private bool _isLeftHand;
    private GameObject _currentEquippedItemGO;

    public void LoadItem(Item item) 
    {
        UnloadItemAndDestroy();

        if (item?.itemPrefab == null) 
        {
            UnloadItem();
            return;
        }
        
        GameObject itemObject = Instantiate(item.itemPrefab, transform);
        itemObject.transform.localPosition = Vector3.zero;
        itemObject.transform.localRotation = Quaternion.identity;
        itemObject.transform.localScale = Vector3.one;

        itemObject.AddComponent<EquippedItemSway>();

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
        if (_currentEquippedItemGO != null){
            _currentEquippedItemGO.SetActive(false);
        }
    }

    public void UnloadItemAndDestroy()
    {
        if (_currentEquippedItemGO != null){
            Destroy(_currentEquippedItemGO);
        }
    }
}
