using System.Collections;
using System.Collections.Generic;
using LumberJack.Wood;
using TMPro;
using UnityEngine;

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
                WoodPiece wood = other.gameObject.GetComponent<WoodPiece>();
                if (wood == null) {
                    return;
                }
                if (wood.IsRefined) {
                    return;
                }
                RefineWood(wood.Definition, wood.Volume);
                Destroy(other.gameObject);
            }
        }

        private void RefineWood(WoodDefinition woodSO, float rawWoodVolume)
        {
            GameObject newWood = Instantiate(_rawWoodGO, _instanciatePosition.position, Quaternion.identity) as GameObject;
            WoodPiece wood = newWood.GetComponent<WoodPiece>();
            wood.transform.localScale = RecalculateWoodSize(rawWoodVolume);
            GenerateWoodMesh(wood.transform.localScale, newWood);
            wood.Initialize(woodSO, true);
        }

        private Vector3 RecalculateWoodSize(float volume)
        {
            Vector3 previewSize = woodPreviewGO.transform.localScale;
            Vector3 newSize = new Vector3();
            float lenght = volume / (previewSize.y * previewSize.z);
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
