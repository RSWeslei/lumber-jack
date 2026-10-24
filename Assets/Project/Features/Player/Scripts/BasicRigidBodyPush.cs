using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Empurra rigidbodies quando o CharacterController esbarra neles.
    /// </summary>
    public class BasicRigidBodyPush : MonoBehaviour
    {
        // Abaixo disso o contato é com algo sob os pés, que não deve ser empurrado
        private const float MinPushDirectionY = -0.3f;

        [SerializeField] private LayerMask _pushLayers;
        [SerializeField] private bool _canPush;
        [Range(0.5f, 5f)]
        [SerializeField] private float _strength = 1.1f;

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (_canPush) PushRigidBody(hit);
        }

        private void PushRigidBody(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic) return;

            int bodyLayerMask = 1 << body.gameObject.layer;
            if ((bodyLayerMask & _pushLayers.value) == 0) return;

            if (hit.moveDirection.y < MinPushDirectionY) return;

            Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            body.AddForce(pushDirection * _strength, ForceMode.Impulse);
        }
    }
}
