using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    private GameObject currentEquippedItem;

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
