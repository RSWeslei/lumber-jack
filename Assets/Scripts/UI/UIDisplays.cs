using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Managers;

public class UIDisplays : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI[] buyableInfo;
    [SerializeField] private Image buyableImage;
    [SerializeField] private Transform buyableParent;

    [Header("UI Keys")]
    [SerializeField] private TextMeshProUGUI interactKey;

    public static UIDisplays Instance { get; private set; }
    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    public void DisplayBuyableInfo(SOBuyable buyable) {
        buyableInfo[0].text = buyable.itemName;
        buyableInfo[1].text = "$ " + buyable.buyPrice.ToString("F2");
        buyableInfo[2].text = buyable.description;
        // buyableInfo[3].text = buyable.category.ToString();
        // buyableImage = buyable.icon;
        UIManager.Instance.ToggleElement(buyableParent.gameObject, true);
    }

    public void HideBuyableInfo() {
        if (gameObject.activeSelf) {
            UIManager.Instance.ToggleElement(buyableParent.gameObject, false);
        }
    }

    public void ShowKeyText(string text) {
        if (!interactKey.enabled) {
            interactKey.text = text;
            interactKey.enabled = true;
        }
    }

    public void HideKeyText(){
        if (interactKey.enabled) {
            interactKey.enabled = false;
        }
    }
}
