using UnityEngine;

namespace LumberJack.Machines
{
    /// <summary>
    /// Esteira: a superfície fica parada, mas empurra o que estiver em cima para frente.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Conveyor : MonoBehaviour
    {
        [Tooltip("m/s")]
        [SerializeField] private float _speed = 0.2f;

        private Rigidbody _body;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            // Volta a esteira um pouco e move de volta com MovePosition: a esteira não sai do lugar,
            // mas a física entende que ela andou para frente e arrasta os objetos em cima
            Vector3 position = _body.position;
            _body.position += Vector3.back * (_speed * Time.fixedDeltaTime);
            _body.MovePosition(position);
        }
    }
}
