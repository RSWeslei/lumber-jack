using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Refiner 
{
    public class RefinerButton : Buttom, IDisplayable
    {
        [SerializeField] private bool isLeft = false;
        [SerializeField] private bool isX = false;
        private ManageRefiner refiner;

        private void Start() {
            refiner = GetComponentInParent<ManageRefiner>();
        }

        public void Display() {
            UIDisplays.Instance.ShowKeyText("Press E to change value");
            // GetComponent<MeshRenderer>().material.color = Color.green;
        }

        public void Hide() {
            // GetComponent<MeshRenderer>().material.color = Color.red;
            return;
        }

        public override void Interact()
        {
            base.Interact();
            TextMeshPro screenText = isX ? refiner.screenTextX : refiner.screenTextY;
            float value = float.Parse(screenText.text);
            float newValue = isLeft ? value - refiner.changeAmount : value + refiner.changeAmount;
            if (newValue < refiner.minAmount || newValue > refiner.maxAmount) {
                return;
            }
            screenText.text = newValue.ToString();
            refiner.ChangeWoodSize(isLeft, isX);
        }
    }
}
