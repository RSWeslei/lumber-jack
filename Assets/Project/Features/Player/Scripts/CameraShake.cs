using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Tremida curta da câmera (batida do machado). Fica na câmera e gira por cima da pose que o FirstPersonCamera
    /// acabou de montar: o LateUpdate dele roda antes (ordem -10), então a tremida nunca acumula.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        // Deslocamentos no ruído para cada eixo girar diferente
        private const float PitchSeed = 0f;
        private const float YawSeed = 37.1f;
        private const float RollSeed = 71.3f;

        [Tooltip("Giro máximo (graus) com a tremida no máximo")]
        [SerializeField] private float _maxAngle = 2.5f;
        [Tooltip("Rapidez da tremida (ciclos por segundo)")]
        [SerializeField] private float _frequency = 18f;
        [Tooltip("Quanto da tremida some por segundo")]
        [SerializeField] private float _decay = 4f;

        private Transform _transform;
        private float _trauma;
        private float _noiseTime;

        private void Awake()
        {
            _transform = transform;
        }

        private void LateUpdate()
        {
            if (_trauma <= 0f) return;

            _noiseTime += Time.deltaTime * _frequency;
            // Ao quadrado: batidas fracas tremem bem pouco, fortes tremem bastante
            float angle = _trauma * _trauma * _maxAngle;
            _transform.rotation *= Quaternion.Euler(Noise(PitchSeed) * angle, Noise(YawSeed) * angle, Noise(RollSeed) * angle);
            _trauma = Mathf.Max(0f, _trauma - _decay * Time.deltaTime);
        }

        /// <summary>Soma uma tremida (0 a 1; o total fica no máximo 1).</summary>
        public void Shake(float strength)
        {
            _trauma = Mathf.Clamp01(_trauma + strength);
        }

        // Ruído suave entre -1 e 1
        private float Noise(float seed)
        {
            return Mathf.PerlinNoise(seed, _noiseTime) * 2f - 1f;
        }
    }
}
