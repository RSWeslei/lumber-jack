using UnityEngine;

namespace LumberJack.Core
{
    /// <summary>
    /// Base para botoes fisicos do mundo (caixa, refinaria...). As subclasses chamam
    /// base.Interact() para manter o feedback comum quando ele existir (animacao, som).
    /// </summary>
    public class InteractableButton : MonoBehaviour, IInteractable
    {
        public virtual void Interact()
        {
        }
    }
}
