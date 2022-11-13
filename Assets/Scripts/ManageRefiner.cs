using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Refiner 
{
    public class ManageRefiner : MonoBehaviour 
    {
        [SerializeField] public TextMeshPro screenTextX;
        [SerializeField] public TextMeshPro screenTextY;
        [SerializeField] public GameObject woodPreview;

        [HideInInspector] public float changeAmount = 0.2f;
        [HideInInspector] public float maxAmount = 3f;
        [HideInInspector] public float minAmount = 1f;

        private int changesCount = 0;
        private float woodChangeAmountY = 0;
        private float woodChangeAmountZ = 0;
        private Vector3 size;

        private void Start() {
            int count = 0;
            for (float i = minAmount; i < maxAmount; i+=changeAmount) {
                count++;
            }
            changesCount = count;
            size = woodPreview.transform.localScale;
            woodChangeAmountY = changeAmount / changesCount;
            woodChangeAmountZ = changeAmount / changesCount;
        }

        public void ChangeWoodSize(bool isLeft, bool isX) {
            if (isX) {
                size.z += isLeft ? -woodChangeAmountZ : woodChangeAmountZ;
            } else {
                size.y += isLeft ? -woodChangeAmountY : woodChangeAmountY;
            }
            woodPreview.transform.localScale = size;
        }
    }
}
