using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Mira do jogador, usada por tudo que depende de "para onde estou olhando" (interagir,
    /// cortar, agarrar). Primeiro um raio exato no centro da tela; se ele não acertar um alvo
    /// aceito, uma ajuda de mira procura o alvo numa esfera fina em volta do raio, sem
    /// atravessar o que o raio exato bateu (nada atrás de parede).
    /// </summary>
    // Depois da rotação da câmera (-10) e antes de quem usa a mira (0)
    [DefaultExecutionOrder(-5)]
    public class PlayerAim : MonoBehaviour
    {
        private const string IgnoreRaycastLayer = "Ignore Raycast";
        private const string PlayerLayer = "Player";
        private const int MaxAssistHits = 8;

        [Tooltip("Distância máxima (m) para interagir, cortar e agarrar")]
        [SerializeField] private float _reach = 2.5f;
        [Tooltip("Raio (m) da ajuda de mira quando o raio exato passa ao lado de um alvo")]
        [SerializeField] private float _assistRadius = 0.15f;

        private readonly RaycastHit[] _assistHits = new RaycastHit[MaxAssistHits];
        private Transform _transform;
        private int _solidMask;

        public float Reach => _reach;
        public Vector3 Origin => _transform.position;
        public Vector3 Forward => _transform.forward;

        private void Awake()
        {
            _transform = transform;
            // Tudo bloqueia a mira, menos o próprio jogador e o que o Unity já ignora
            _solidMask = ~LayerMask.GetMask(IgnoreRaycastLayer, PlayerLayer);
        }

        /// <summary>
        /// Procura um alvo nas camadas de <paramref name="targetMask"/> na direção da mira.
        /// </summary>
        public bool TryFindTarget(int targetMask, out RaycastHit hit)
        {
            Vector3 origin = _transform.position;
            Vector3 direction = _transform.forward;
            float limit = _reach;

            if (Physics.Raycast(origin, direction, out hit, _reach, _solidMask, QueryTriggerInteraction.Ignore))
            {
                if (IsInMask(hit.collider.gameObject.layer, targetMask)) return true;
                // A ajuda de mira não pode ir além do que o raio exato bateu
                limit = hit.distance;
            }

            int count = Physics.SphereCastNonAlloc(origin, _assistRadius, direction, _assistHits, limit,
                targetMask, QueryTriggerInteraction.Ignore);

            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                // distance 0 = a esfera já começou dentro do objeto; o raio exato cuidaria dele
                float distance = _assistHits[i].distance;
                if (distance <= 0f || distance >= bestDistance) continue;
                best = i;
                bestDistance = distance;
            }

            if (best < 0)
            {
                hit = default;
                return false;
            }
            hit = _assistHits[best];
            return true;
        }

        private static bool IsInMask(int layer, int mask)
        {
            return (mask & (1 << layer)) != 0;
        }
    }
}
