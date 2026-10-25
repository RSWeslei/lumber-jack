using LumberJack.Audio;
using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Passos do jogador: um passo a cada trecho andado no chão, com o som do chão pisado (grama, madeira...),
    /// e uma pisada mais forte ao aterrissar.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class Footsteps : MonoBehaviour
    {
        private const float GroundProbeStart = 0.2f;

        [SerializeField] private AudioSource _audio;
        [SerializeField] private SurfaceSoundsDefinition _surfaces;
        [Tooltip("Distância (m) andada entre um passo e outro")]
        [Min(0.1f)]
        [SerializeField] private float _stepDistance = 2f;
        [Tooltip("Até onde (m) abaixo dos pés procura o chão")]
        [Min(0.1f)]
        [SerializeField] private float _groundProbeDistance = 0.5f;

        [Header("Aterrissagem")]
        [Tooltip("Velocidade de queda (m/s) abaixo da qual a aterrissagem não faz som")]
        [Min(0f)]
        [SerializeField] private float _minLandingSpeed = 3f;
        [Tooltip("Velocidade de queda (m/s) com a aterrissagem no volume máximo")]
        [Min(0.1f)]
        [SerializeField] private float _fullLandingSpeed = 10f;

        private PlayerMovement _movement;
        private int _groundMask;
        private float _distance;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _groundMask = ~LayerMask.GetMask("Player", "FirstPerson");
        }

        private void OnEnable()
        {
            _movement.OnLanded += HandleLanded;
        }

        private void Update()
        {
            if (!_movement.IsGrounded) return;

            _distance += _movement.HorizontalSpeed * Time.deltaTime;
            if (_distance < _stepDistance) return;

            _distance -= _stepDistance;
            PlayStep(1f);
        }

        private void OnDisable()
        {
            _movement.OnLanded -= HandleLanded;
        }

        private void HandleLanded(float fallSpeed)
        {
            // O próximo passo vem meio passo depois de tocar o chão
            _distance = _stepDistance * 0.5f;
            if (fallSpeed < _minLandingSpeed) return;

            PlayStep(Mathf.Clamp01(fallSpeed / _fullLandingSpeed));
        }

        private void PlayStep(float volume)
        {
            Vector3 origin = transform.position + Vector3.up * GroundProbeStart;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundProbeStart + _groundProbeDistance, _groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return;
            }

            SoundDefinition sound = _surfaces.SoundFor(hit.collider);
            if (sound != null) sound.Play(_audio, volume);
        }
    }
}
