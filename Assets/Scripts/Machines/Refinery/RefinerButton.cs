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
        private WoodRefinery
        
         refiner;
        private TextMeshPro screenText;

        private void Start() {
            refiner = GetComponentInParent<WoodRefinery
            
            >();
            screenText = isX ? refiner.screenTextX : refiner.screenTextY;
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
            TweakPreview();
        }

        private void TweakPreview() {
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
