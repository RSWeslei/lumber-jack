using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Wood")]
public class WoodSO : ScriptableObject
{
    public string woodName;
    public float rawPrice;
    public float refinedPrice;
    public float hardness;

}
