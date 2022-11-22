using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Managers;

public class UIDisplays : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI[] _buyableInfoText;
    [SerializeField] private Image _buyableImage;
    [SerializeField] private Transform _buyableParent;

    [Header("UI Keys")]
    [SerializeField] private TextMeshProUGUI _interactKeyText;

    public static UIDisplays Instance { get; private set; }
    private void Awake()
     {
        if (Instance == null) 
        {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    public void DisplayBuyableInfo(SOBuyable buyable) 
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

    public void ShowKeyText(string text) 
    {
        if (!_interactKeyText.enabled) {
            _interactKeyText.text = text;
            _interactKeyText.enabled = true;
        }
    }

    public void HideKeyText()
    {
        if (_interactKeyText.enabled) 
        {
            _interactKeyText.enabled = false;
        }
    }
}
