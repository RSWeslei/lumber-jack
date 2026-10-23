using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using LumberJack.Core;
public class MenuActivator : MonoBehaviour, IDisplayable
{
    [SerializeField] private GameObject document;
    [SerializeField] private string menuName;
    private VisualElement menu;

    void Start()
    {
        menu = document.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>(menuName);
    }

    public void Display()
    {
        UIDisplays.Instance.ShowKeyInfo($"Press {InputManager.Instance.InteractKeyLabel} to open the menu");
        menu.style.display = DisplayStyle.Flex;
    }
}
