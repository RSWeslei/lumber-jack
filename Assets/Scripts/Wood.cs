using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]
public class Wood : MonoBehaviour
{
    [SerializeField] public float woodValue = 0;
    public bool isRefined = false;
    [SerializeField] private WoodSO _woodSO;

    public WoodSO WoodSO  {
        get { return _woodSO; }
        set { _woodSO = value; }
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
        float price = isRefined ? _woodSO.refinedPrice : _woodSO.rawPrice;
        woodValue = Mathf.Floor(volume * price * 100);
    }

    public void GenerateWood(WoodSO woodSO) {
        this._woodSO = woodSO;
        CalculateValue();
    }
}
