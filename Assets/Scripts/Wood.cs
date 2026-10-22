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

    // Volume real da malha (m3), funciona para cubos escalados e para pedacos cortados
    public float Volume {
        get {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            Vector3 scale = transform.lossyScale;
            float meshVolume = meshFilter != null && meshFilter.sharedMesh != null ? MeshSlicer.GetVolume(meshFilter.sharedMesh) : 1f;
            return meshVolume * Mathf.Abs(scale.x * scale.y * scale.z);
        }
    }

    void Start(){
        CalculateValue();
    }

    public void CalculateValue()
    {
        if (_woodSO == null) return;
        float price = isRefined ? _woodSO.refinedPrice : _woodSO.rawPrice;
        woodValue = Mathf.Floor(Volume * price * 100);
    }

    public void GenerateWood(WoodSO woodSO) {
        this._woodSO = woodSO;
        CalculateValue();
    }

    // Usado quando um pedaco e cortado: o pedaco novo herda o tipo de madeira
    public void CopyFrom(Wood other) {
        isRefined = other.isRefined;
        GenerateWood(other._woodSO);
    }
}
