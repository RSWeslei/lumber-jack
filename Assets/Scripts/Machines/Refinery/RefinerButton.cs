using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Refiner 
{
    public class RefinerButton : Buttom, IDisplayable
    {
        [SerializeField] private bool _isLeft = false;
        [SerializeField] private bool _isX = false;
        private WoodRefinery _refiner;
        private TextMeshPro _screenText;
        private string _text;

        private void Start() 
        {
            _refiner = GetComponentInParent<WoodRefinery>();
            _screenText = _isX ? _refiner.screenTextX : _refiner.screenTextY;
            _text = (_isX && _isLeft) ? $"Press {InputManager.Instance.interactKey} to decrease width" 
            : (_isX && !_isLeft) ? $"Press {InputManager.Instance.interactKey} to increase width"
            : (_isLeft) ? $"Press {InputManager.Instance.interactKey} to decrease height"
            : $"Press {InputManager.Instance.interactKey} to increase height";
        }

        public void Display() 
        {
            UIDisplays.Instance.ShowKeyInfo(_text);
            // GetComponent<MeshRenderer>().material.color = Color.green;
        }

        public override void Interact()
        {
            base.Interact();
            TweakPreview();
        }

        private void TweakPreview() {
            float value = float.Parse(_screenText.text);
            float newValue = _isLeft ? value - _refiner.changeAmount : value + _refiner.changeAmount;
            if (newValue < _refiner.minAmount || newValue > _refiner.maxAmount) {
                return;
            }
            _screenText.text = newValue.ToString();
            _refiner.ChangeWoodSize(_isLeft, _isX);
        }
    }
}
