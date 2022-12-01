using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StealDetector : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Buyable"))
        {
            Buyable buyable = other.gameObject.GetComponent<Buyable>();
            if (buyable != null && !buyable.IsBought)
            {
                Destroy(other.gameObject);
            }
        }
    }
}
