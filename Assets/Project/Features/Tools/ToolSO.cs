using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LumberJack.Inventory;

[CreateAssetMenu(fileName = "New Tool", menuName = "Inventory/Tool")]
public class ToolSO : ItemDefinition
{
    [Tooltip("Ferramenta corta madeira (machados)")]
    public bool canCutWood;
}
