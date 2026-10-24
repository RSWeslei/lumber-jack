using LumberJack.Core;
using LumberJack.Shared;
using UnityEngine;

namespace LumberJack.Vehicles
{
    public enum DoorSide
    {
        FrontLeft,
        FrontRight,
        BackLeft,
        BackRight
    }

    /// <summary>
    /// Porta do veículo. A dianteira esquerda leva ao banco do motorista; as outras ainda não fazem nada.
    /// </summary>
    public class VehicleDoor : MonoBehaviour, IInteractable, IDisplayable
    {
        [SerializeField] private DoorSide _side;

        private VehicleController _vehicle;
        // Montada uma vez: a tecla não muda durante o jogo e assim a dica não aloca string
        private string _hint;

        private void Awake()
        {
            _vehicle = GetComponentInParent<VehicleController>();
        }

        private void Start()
        {
            _hint = $"Press {InputManager.Instance.InteractKeyLabel} to enter the vehicle";
        }

        public void Interact()
        {
            if (_side == DoorSide.FrontLeft) _vehicle.EnterDriverSeat();
        }

        public void Display()
        {
            UIDisplays.Instance.ShowKeyInfo(_hint);
        }
    }
}
