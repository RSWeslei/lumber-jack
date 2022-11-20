using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wood : MonoBehaviour
{
    public bool isRefined = false;
    public float woodValue = 0;
    [SerializeField] private WoodSO woodSO;

    public WoodSO WoodSO  {
        get { return woodSO; }
        set { woodSO = value; }
    }

    private void Start() {
        CalculateValue();
    }

    public void CalculateValue() 
    {
        Collider collider = GetComponent<Collider>();
        float lenght = collider.bounds.size.x;
        float width = collider.bounds.size.z;
        float height = collider.bounds.size.y;
        
        float volume = (lenght * width * height) * 100;
        float price = isRefined ? woodSO.refinedPrice : woodSO.rawPrice;
        woodValue = Mathf.Floor(volume * price);
    }

    public void GenerateWood(WoodSO woodSO) {
        this.woodSO = woodSO;
        CalculateValue();
    }
}
