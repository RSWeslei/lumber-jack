using UnityEngine;

namespace LumberJack.Core
{
    /// <summary>
    /// Base para botões físicos do mundo (caixa, refinaria...). As subclasses chamam
    /// base.Interact() para manter o feedback comum quando ele existir (animação, som).
    /// </summary>
    public class InteractableButton : MonoBehaviour, IInteractable
    {
        public virtual void Interact()
        {
        }
    }
}
