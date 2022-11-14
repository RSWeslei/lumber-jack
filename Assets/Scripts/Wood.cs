using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wood : MonoBehaviour
{
    public bool isRefined = false;
    public float woodValue = 0;
    public float woodMass = 0;
    
    private void Awake() {
        woodMass = GetComponent<Rigidbody>().mass;
    }
}
