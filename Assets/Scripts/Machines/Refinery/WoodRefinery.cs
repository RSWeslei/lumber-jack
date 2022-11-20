using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refiner 
{
    public class WoodRefinery : MonoBehaviour
    {
        private ManageRefiner manageRefiner;
        [SerializeField] private Transform instanciatePos;
        [SerializeField] private GameObject rawWood;

        private void Awake() {
            manageRefiner = GetComponent<ManageRefiner>();
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
            Vector3 previewSize = manageRefiner.woodPreview.transform.localScale;
            Vector3 newSize = new Vector3();
            float biggerAxis = Mathf.Max(woodSize.x, woodSize.y, woodSize.z);
            float lenght=0;
            if (biggerAxis == woodSize.x) {
                lenght = (woodSize.y - previewSize.y) + (woodSize.z - previewSize.z) + woodSize.x;
            } else if (biggerAxis == woodSize.y) {
                lenght = (woodSize.x - previewSize.y) + (woodSize.z - previewSize.z) + woodSize.y;
            } else {
                lenght = (woodSize.x - previewSize.y) + (woodSize.y - previewSize.z) + woodSize.z;
            }

            newSize.z = lenght;
            newSize.x = previewSize.z;
            newSize.y = previewSize.y;
            return newSize;
        }
    }
}
