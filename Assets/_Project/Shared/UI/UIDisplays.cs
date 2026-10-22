using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Managers;
using System;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class UIDisplays : MonoBehaviour
{
    [Header("UI Elements")]
    public UIDocument m_menuUIDocument;
    private VisualElement m_keyInfoParent;
    private Label m_keyInfoText;
    [SerializeField] private TextMeshProUGUI[] _buyableInfoText;
    [SerializeField] private Image _buyableImage;
    [SerializeField] private Transform _buyableParent;

    public static UIDisplays Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
        m_keyInfoParent = m_menuUIDocument.rootVisualElement.Q<VisualElement>("KeyInfoParent");
        m_keyInfoText = m_keyInfoParent.Q<Label>("KeyInfo");
    }

    public void ShowKeyInfo(String keyInfo) 
    {
        m_keyInfoText.text = keyInfo;
        m_keyInfoParent.style.display = DisplayStyle.Flex;
    }

    public void HideKeyInfo()
    {
        m_keyInfoParent.style.display = DisplayStyle.None;
    }

    public void DisplayBuyableInfo(BuyableSO buyable) 
    {
        _buyableInfoText[0].text = buyable.itemName;
        _buyableInfoText[1].text = "$ " + buyable.buyPrice.ToString("F2");
        _buyableInfoText[2].text = buyable.description;
        // _buyableInfoText[3].text = buyable.category.ToString();
        // _buyableImage = buyable.icon;
        UIManager.Instance.ToggleElement(_buyableParent.gameObject, true);
    }

    public void HideBuyableInfo() 
    {
        if (gameObject.activeSelf) 
        {
            UIManager.Instance.ToggleElement(_buyableParent.gameObject, false);
        }
    }


    // public void ShowKeyText(string text) 
    // {
    //     // if (!_interactKeyText.enabled) {
    //     //     _interactKeyText.text = text;
    //     //     _interactKeyText.enabled = true;
    //     // }
    // }

    // public void HideKeyText()
    // {
    //     // if (_interactKeyText.enabled) 
    //     // {
    //     //     _interactKeyText.enabled = false;
    //     // }
    // }
}
