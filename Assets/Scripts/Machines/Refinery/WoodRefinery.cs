using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Refiner 
{
    public class WoodRefinery : MonoBehaviour
    {
        [Header("Properties")]
        public float changeAmount = 0.2f;
        public float maxAmount = 3f;
        public float minAmount = 1f;
        [SerializeField] private float loseAmount = 0.1f;

        [SerializeField] private Transform _instanciatePosition;
        [SerializeField] private GameObject _rawWoodGO;
        [SerializeField] public TextMeshPro screenTextX;
        [SerializeField] public TextMeshPro screenTextY;
        [SerializeField] public GameObject woodPreviewGO;
        
        private float _woodChangeAmountY = 0;
        private float _woodChangeAmountZ = 0;
        private Vector3 _size;

        private void Awake() 
        {
            CalculateChangeAmount();
        }

        private void OnTriggerEnter(Collider other) 
        {
            if (other.gameObject.tag == "Wood") 
            {
                Wood wood = other.gameObject.GetComponent<Wood>();
                if (wood == null) {
                    return;
                }
                if (wood.isRefined) {
                    return;
                }
                RefineWood(wood.WoodSO, other.transform.localScale);
                Destroy(other.gameObject);
            }
        }

        private void RefineWood(WoodSO woodSO, Vector3 rawWoodSize) 
        {
            GameObject newWood = Instantiate(_rawWoodGO, _instanciatePosition.position, Quaternion.identity) as GameObject;
            Wood wood = newWood.GetComponent<Wood>();
            wood.transform.localScale = RecalculateWoodSize(rawWoodSize);
            GenerateWoodMesh(wood.transform.localScale, newWood);
            wood.isRefined = true;
            wood.GenerateWood(woodSO);
        }

        private Vector3 RecalculateWoodSize(Vector3 woodSize) 
        {
            Vector3 previewSize = woodPreviewGO.transform.localScale;
            Vector3 newSize = new Vector3();
            float biggerAxis = Mathf.Max(woodSize.x, woodSize.y, woodSize.z);
            float volume = woodSize.x * woodSize.y * woodSize.z;
            float lenght = lenght = volume / (previewSize.y * previewSize.z);
            lenght -= lenght * loseAmount;
            
            newSize.z = lenght;
            newSize.x = previewSize.z;
            newSize.y = previewSize.y;
            return newSize;
        }

        private void GenerateWoodMesh(Vector3 size, GameObject wood) 
        {
            MeshFilter meshFilter = wood.GetComponent<MeshFilter>();

        }

        public void ChangeWoodSize(bool isLeft, bool isX) 
        {
            if (isX) {
                _size.z += isLeft ? -_woodChangeAmountZ : _woodChangeAmountZ;
            } else {
                _size.y += isLeft ? -_woodChangeAmountY : _woodChangeAmountY;
            }
            woodPreviewGO.transform.localScale = _size;
        }

        private void CalculateChangeAmount() 
        {
            int count = 0;
            for (float i = minAmount; i < maxAmount; i+=changeAmount) {
                count++;
            }
            _size = woodPreviewGO.transform.localScale;
            _woodChangeAmountY = changeAmount / count;
            _woodChangeAmountZ = changeAmount / count;
        }
    }
}
