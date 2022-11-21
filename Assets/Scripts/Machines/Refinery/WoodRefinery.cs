using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Refiner 
{
    public class WoodRefinery : MonoBehaviour
    {
        [Header("Properties")]
        [SerializeField] private float loseAmount = 0.1f;
        public float changeAmount = 0.2f;
        public float maxAmount = 3f;
        public float minAmount = 1f;

        [SerializeField] private Transform instanciatePos;
        [SerializeField] private GameObject rawWood;
        [SerializeField] public TextMeshPro screenTextX;
        [SerializeField] public TextMeshPro screenTextY;
        [SerializeField] public GameObject woodPreview;
        

        private float woodChangeAmountY = 0;
        private float woodChangeAmountZ = 0;
        private Vector3 size;

        private void Awake() {
            CalculateChangeAmount();
        }

        private void OnTriggerEnter(Collider other) 
        {
            if (other.gameObject.tag == "Wood") {
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
            GameObject newWood = Instantiate(rawWood, instanciatePos.position, Quaternion.identity) as GameObject;
            Wood wood = newWood.GetComponent<Wood>();
            wood.transform.localScale = RecalculateWoodSize(rawWoodSize);
            wood.isRefined = true;
            wood.GenerateWood(woodSO);
        }

        private Vector3 RecalculateWoodSize(Vector3 woodSize) 
        {
            Vector3 previewSize = woodPreview.transform.localScale;
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

        public void ChangeWoodSize(bool isLeft, bool isX) {
            if (isX) {
                size.z += isLeft ? -woodChangeAmountZ : woodChangeAmountZ;
            } else {
                size.y += isLeft ? -woodChangeAmountY : woodChangeAmountY;
            }
            woodPreview.transform.localScale = size;
        }

        private void CalculateChangeAmount() {
            int count = 0;
            for (float i = minAmount; i < maxAmount; i+=changeAmount) {
                count++;
            }
            size = woodPreview.transform.localScale;
            woodChangeAmountY = changeAmount / count;
            woodChangeAmountZ = changeAmount / count;
        }
    }
}
