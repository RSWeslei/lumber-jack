using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tool : MonoBehaviour, IDisplayable, IInteractable
{
    public void Display()
    {
        UIDisplays.Instance.ShowKeyInfo($"Press {InputManager.Instance.interactKey} to pick up the tool");
    }

    public void Interact()
    {
        
    }
}
