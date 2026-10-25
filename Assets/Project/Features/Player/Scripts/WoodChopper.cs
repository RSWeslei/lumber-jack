using LumberJack.Audio;
using LumberJack.Core;
using LumberJack.Tools;
using LumberJack.Wood;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LumberJack.Player
{
    /// <summary>
    /// Machado equipado na hotbar: mira o que dá para cortar (com a linha do corte), toca o golpe nos braços e
    /// aplica o dano no instante em que o machado chega na madeira, com lascas e uma tremida da câmera.
    /// </summary>
    public class WoodChopper : MonoBehaviour
    {
        private const string WoodLayerName = "Wood";

        [SerializeField] private Hand _hand;
        [SerializeField] private FirstPersonArms _arms;
        [SerializeField] private Material _previewMaterial;

        [Header("Efeitos da batida")]
        [Tooltip("Partículas de lascas, criadas uma vez e reaproveitadas em cada batida")]
        [SerializeField] private ParticleSystem _chipsPrefab;
        [Tooltip("Lascas por batida")]
        [Min(0)]
        [SerializeField] private int _chipCount = 10;
        [Tooltip("Força da tremida da câmera (0 a 1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _hitShake = 0.35f;

        [Header("Sons")]
        [SerializeField] private AudioSource _audio;
        [Tooltip("Vento do machado ao começar o golpe")]
        [SerializeField] private SoundDefinition _swingSound;
        [Tooltip("Machado batendo na madeira")]
        [SerializeField] private SoundDefinition _hitSound;

        [Header("Inclinação do corte")]
        [SerializeField] private float _maxTilt = 60f;
        [Tooltip("Graus por segundo segurando a tecla de inclinar")]
        [SerializeField] private float _tiltSpeed = 60f;

        private Transform _camera;
        private PlayerAim _aim;
        private CameraShake _shake;
        private ParticleSystem _chips;
        private int _woodMask;
        private CutPreview _preview;
        private InputAction _tiltAction;
        private InputAction _straightenAction;
        private float _tilt;

        // Cache do último collider mirado: evita GetComponent a cada frame no mesmo tronco
        private Collider _lastCollider;
        private IAxeTarget _lastColliderTarget;
        private bool _lastColliderIsTarget;

        private IAxeTarget _target;
        private bool _hasTarget;
        private Plane _targetPlane;
        private bool _isTargetValid;
        private Vector3 _targetPoint;
        private Vector3 _targetNormal;

        // Alvo travado no clique: o dano entra na batida, mesmo que a mira tenha saído
        private IAxeTarget _swingTarget;
        private bool _hasSwingTarget;
        private Plane _swingPlane;
        private Vector3 _swingDirection;
        private float _swingDamage;
        private Vector3 _swingPoint;
        private Vector3 _swingNormal;

        private bool HasAxe => _hand.EquippedItem is ToolDefinition tool && tool.CanCutWood;

        private void Awake()
        {
            if (_hand == null || _arms == null || _chipsPrefab == null || _audio == null || _swingSound == null || _hitSound == null)
            {
                Debug.LogError($"{nameof(WoodChopper)} sem referência para a mão, os braços, as lascas ou os sons.", this);
                enabled = false;
                return;
            }

            _aim = GetComponentInParent<PlayerAim>();
            _camera = _aim.transform;
            _shake = GetComponentInParent<CameraShake>();
            // Na raiz: as lascas ficam no mundo enquanto o jogador anda
            _chips = Instantiate(_chipsPrefab);
            _woodMask = LayerMask.GetMask(WoodLayerName);
            _preview = CutPreview.Create(transform, _previewMaterial);

            PlayerInputs.PlayerActionsActions actions = InputManager.Instance.PlayerInputs.PlayerActions;
            _tiltAction = actions.CutTilt;
            _straightenAction = actions.CutStraighten;
        }

        private void OnEnable()
        {
            InputManager.Instance.AttackPressed += HandleAttackPressed;
            _arms.Impacted += HandleImpacted;
        }

        private void OnDisable()
        {
            InputManager.Instance.AttackPressed -= HandleAttackPressed;
            _arms.Impacted -= HandleImpacted;
        }

        private void Update()
        {
            _hasTarget = false;
            if (!HasAxe || _arms.IsSwinging)
            {
                _preview.Hide();
                return;
            }

            UpdateTilt();
            UpdateTarget();

            if (_hasTarget) _preview.Show(_target, _targetPlane, _isTargetValid, _target.CutProgress);
            else _preview.Hide();
        }

        private void OnDestroy()
        {
            if (_chips) Destroy(_chips.gameObject);
        }

        private void UpdateTilt()
        {
            if (_straightenAction.WasPressedThisFrame()) _tilt = 0f;
            _tilt += _tiltAction.ReadValue<float>() * _tiltSpeed * Time.deltaTime;
            _tilt = Mathf.Clamp(_tilt, -_maxTilt, _maxTilt);
        }

        private void UpdateTarget()
        {
            if (!_aim.TryFindTarget(_woodMask, out RaycastHit hit)) return;

            // ReferenceEquals compara só a referência C#, sem a checagem nativa do == do Unity
            Collider hitCollider = hit.collider;
            if (!ReferenceEquals(hitCollider, _lastCollider))
            {
                _lastCollider = hitCollider;
                _lastColliderIsTarget = TryGetAxeTarget(hitCollider, out _lastColliderTarget);
            }
            if (!_lastColliderIsTarget) return;

            _target = _lastColliderTarget;
            _hasTarget = true;
            _targetPoint = hit.point;
            _targetNormal = hit.normal;
            _targetPlane = _target.GetCutPlane(hit.point, _camera.forward, _camera.up, _tilt);
            _isTargetValid = _target.CanCut(_targetPlane);
        }

        // O alvo pode estar no próprio collider (árvore em pé) ou no corpo dele: pedaços de árvore
        // colidem com cascos filhos e o alvo fica no objeto do Rigidbody
        private static bool TryGetAxeTarget(Collider hitCollider, out IAxeTarget target)
        {
            if (hitCollider.TryGetComponent(out target)) return true;

            Rigidbody body = hitCollider.attachedRigidbody;
            // attachedRigidbody devolve null de verdade quando não há corpo: ReferenceEquals basta
            return !ReferenceEquals(body, null) && body.TryGetComponent(out target);
        }

        private void HandleAttackPressed()
        {
            if (!HasAxe || _arms.IsSwinging) return;

            // Golpe no ar também anima, só não corta nada
            ToolDefinition tool = (ToolDefinition)_hand.EquippedItem;
            _hasSwingTarget = _hasTarget && _isTargetValid;
            _swingTarget = _hasSwingTarget ? _target : null;
            _swingPlane = _targetPlane;
            _swingDirection = _camera.forward;
            _swingDamage = tool.ChopDamage;
            _swingPoint = _targetPoint;
            _swingNormal = _targetNormal;
            _arms.Swing(tool.SwingSpeed);
            _swingSound.Play(_audio);
        }

        private void HandleImpacted()
        {
            if (!_hasSwingTarget) return;

            _hasSwingTarget = false;
            // O alvo pode ter sido cortado/destruído enquanto o machado descia
            if (!(_swingTarget is Object targetObject) || targetObject == null || !_swingTarget.CanCut(_swingPlane)) return;

            _swingTarget.Strike(_swingPlane, _swingDirection, _swingDamage);
            _arms.HitStop();
            _hitSound.Play(_audio);
            _shake.Shake(_hitShake);
            // As lascas saem da madeira para fora, na direção de quem bateu
            _chips.transform.SetPositionAndRotation(_swingPoint, Quaternion.LookRotation(_swingNormal));
            _chips.Emit(_chipCount);
        }
    }
}
