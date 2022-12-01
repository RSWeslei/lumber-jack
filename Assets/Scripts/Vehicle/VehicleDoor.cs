using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vehicle
{
    public class VehicleDoor : MonoBehaviour, IInteractable, IDisplayable
    {
        public enum DoorSide
        {
            FrontLeft,
            FrontRight,
            BackLeft,
            BackRight
        }

        public DoorSide doorSide;
        // [SerializeField] private bool isOpen = false;
        private VehicleController vehicleController;

        private void Awake() {
            vehicleController = GetComponentInParent<VehicleController>();
        }

        public void Interact()
        {
            switch (doorSide)
            {
                case DoorSide.FrontLeft:
                    vehicleController.EnterDriverSeat();
                    break;
                case DoorSide.FrontRight:
                    break;
                case DoorSide.BackLeft:
                    break;
                case DoorSide.BackRight:
                    break;
                default:
                    break;
            }
        }

        public void Display()
        {
            UIDisplays.Instance.ShowKeyInfo($"Press {InputManager.Instance.interactKey} to enter the vehicle");
        }
    }
}
