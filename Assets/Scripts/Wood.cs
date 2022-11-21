using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]
public class Wood : MonoBehaviour
{
    public bool isRefined = false;
    [SerializeField] public float woodValue = 0;
    [SerializeField] private WoodSO woodSO;

    public WoodSO WoodSO  {
        get { return woodSO; }
        set { woodSO = value; }
    }
    void Start(){
        CalculateValue();
    }
    
    public void CalculateValue() 
    {
        float lenght = transform.localScale.x;
        float width = transform.localScale.z;
        float height = transform.localScale.y;
        
        float volume = (lenght * width * height);
        float price = isRefined ? woodSO.refinedPrice : woodSO.rawPrice;
        woodValue = Mathf.Floor(volume * price * 100);
    }

    public void GenerateWood(WoodSO woodSO) {
        this.woodSO = woodSO;
        CalculateValue();
    }
}
