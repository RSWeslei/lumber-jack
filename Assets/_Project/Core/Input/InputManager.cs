using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LumberJack.Core
{
    /// <summary>
    /// Ponto central do input do jogador (a pe e hotbar). Valores continuos (mover, olhar)
    /// sao lidos na hora; acoes pontuais viram eventos.
    /// </summary>
    // Roda antes dos outros scripts para o Instance e as acoes ja existirem no OnEnable deles
    [DefaultExecutionOrder(-100)]
    public class InputManager : MonoBehaviour
    {
        private GUIInputs _guiInputs;

        public static InputManager Instance { get; private set; }

        public PlayerInputs PlayerInputs { get; private set; }

        public Vector2 MoveInput => PlayerInputs.PlayerMovement.Move.ReadValue<Vector2>();
        public Vector2 LookInput => PlayerInputs.PlayerMovement.Look.ReadValue<Vector2>();
        public bool IsJumpHeld => PlayerInputs.PlayerMovement.Jump.IsPressed();
        public bool IsGrabHeld => PlayerInputs.PlayerActions.Grab.IsPressed();

        // Nome da tecla para mostrar na UI ("E", "Left Shift"...)
        public string InteractKeyLabel { get; private set; }
        public string GrabKeyLabel { get; private set; }
        public string JumpKeyLabel { get; private set; }
        public string SprintKeyLabel { get; private set; }

        public event Action AttackPressed;
        public event Action InteractPressed;
        public event Action GrabPressed;
        public event Action GrabReleased;
        /// <summary>Slot escolhido pelas teclas numericas (comeca em 0).</summary>
        public event Action<int> HotbarSlotSelected;
        /// <summary>+1 rolou para cima, -1 para baixo.</summary>
        public event Action<float> HotbarScrolled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Remove so o componente duplicado, nao o objeto (que e o proprio player)
                Destroy(this);
                return;
            }

            Instance = this;
            PlayerInputs = new PlayerInputs();
            _guiInputs = new GUIInputs();

            InteractKeyLabel = GetKeyLabel(PlayerInputs.PlayerActions.Interact);
            GrabKeyLabel = GetKeyLabel(PlayerInputs.PlayerActions.Grab);
            JumpKeyLabel = GetKeyLabel(PlayerInputs.PlayerMovement.Jump);
            SprintKeyLabel = GetKeyLabel(PlayerInputs.PlayerMovement.Sprint);
        }

        private void OnEnable()
        {
            if (PlayerInputs == null) return;

            PlayerInputs.PlayerActions.LeftMouse.performed += HandleAttackPerformed;
            PlayerInputs.PlayerActions.Interact.performed += HandleInteractPerformed;
            PlayerInputs.PlayerActions.Grab.performed += HandleGrabPerformed;
            PlayerInputs.PlayerActions.Grab.canceled += HandleGrabCanceled;
            _guiInputs.PlayerUI.HotbarNumbers.performed += HandleHotbarNumberPerformed;
            _guiInputs.PlayerUI.HotbarMouseScroll.performed += HandleHotbarScrollPerformed;

            PlayerInputs.Enable();
            _guiInputs.Enable();
        }

        private void OnDisable()
        {
            if (PlayerInputs == null) return;

            PlayerInputs.PlayerActions.LeftMouse.performed -= HandleAttackPerformed;
            PlayerInputs.PlayerActions.Interact.performed -= HandleInteractPerformed;
            PlayerInputs.PlayerActions.Grab.performed -= HandleGrabPerformed;
            PlayerInputs.PlayerActions.Grab.canceled -= HandleGrabCanceled;
            _guiInputs.PlayerUI.HotbarNumbers.performed -= HandleHotbarNumberPerformed;
            _guiInputs.PlayerUI.HotbarMouseScroll.performed -= HandleHotbarScrollPerformed;

            PlayerInputs.Disable();
            _guiInputs.Disable();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            PlayerInputs?.Dispose();
            _guiInputs?.Dispose();
            Instance = null;
        }

        private void HandleAttackPerformed(InputAction.CallbackContext context) => AttackPressed?.Invoke();

        private void HandleInteractPerformed(InputAction.CallbackContext context) => InteractPressed?.Invoke();

        private void HandleGrabPerformed(InputAction.CallbackContext context) => GrabPressed?.Invoke();

        private void HandleGrabCanceled(InputAction.CallbackContext context) => GrabReleased?.Invoke();

        // As teclas 1-9 estao configuradas para devolver o proprio numero
        private void HandleHotbarNumberPerformed(InputAction.CallbackContext context)
        {
            HotbarSlotSelected?.Invoke(Mathf.RoundToInt(context.ReadValue<float>()) - 1);
        }

        private void HandleHotbarScrollPerformed(InputAction.CallbackContext context)
        {
            float scroll = context.ReadValue<Vector2>().y;
            if (scroll != 0f) HotbarScrolled?.Invoke(Mathf.Sign(scroll));
        }

        private static string GetKeyLabel(InputAction action)
        {
            if (action.bindings.Count == 0) return string.Empty;
            return InputControlPath.ToHumanReadableString(action.bindings[0].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
    }
}
