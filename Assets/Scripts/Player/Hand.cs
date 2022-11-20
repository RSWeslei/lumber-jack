using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    private GameObject currentEquippedItem;
    [SerializeField] private bool isLeftHand;

    public void LoadItem(Item item) {
        UnloadItemAndDestroy();

        if (item?.itemPrefab == null) {
            UnloadItem();
            return;
        }
        
        GameObject itemObject = Instantiate(item.itemPrefab, transform);
        itemObject.transform.localPosition = Vector3.zero;
        itemObject.transform.localRotation = Quaternion.identity;
        itemObject.transform.localScale = Vector3.one;

        itemObject.AddComponent<EquippedItemSway>();

        currentEquippedItem = itemObject;
    }

    public bool IsItemEquiped => currentEquippedItem != null;

    public Animator GetWieldedAnimator() {
        if (!IsItemEquiped) return null;
        return currentEquippedItem.GetComponent<Animator>() ?? null;
    }

    public void UnloadItem()
    {
        if (currentEquippedItem != null){
            currentEquippedItem.SetActive(false);
        }
    }

    public void UnloadItemAndDestroy()
    {
        if (currentEquippedItem != null){
            Destroy(currentEquippedItem);
        }
    }
}
