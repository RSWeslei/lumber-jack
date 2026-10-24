using System;
using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Árvore em pé cortada a machado em qualquer galho (ou no tronco), com o círculo de corte das toras.
    /// O primeiro golpe fixa o corte e os seguintes avançam nele; no fim, a parte de fora vira um
    /// <see cref="TreePiece"/> que cai. Cortando o tronco, ela tomba presa à borda do toco até inclinar.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProceduralTree))]
    public class ChoppableTree : MonoBehaviour, IAxeTarget
    {
        private const int TrunkIndex = 0;
        private const int NoBranch = -1;
        // A dobradiça do tronco solta quando ele já inclinou isso, ou depois desse tempo
        private const float HingeReleaseAngle = 40f;
        private const float MaxHingeSeconds = 5f;
        // Um galho cortado não colide com a árvore por esse tempo, para não ser empurrado por ela ao cair
        private const float BranchClearSeconds = 1f;
        private const float MinDirectionSqrMagnitude = 0.0001f;
        private const string PieceSuffix = " (cortada)";

        // Cortes acontecem um por vez na thread principal: um cortador para todas as árvores
        private static readonly TreeCutter SharedCutter = new TreeCutter();

        private readonly TreeFellingState _state = new TreeFellingState();
        private readonly TreePartMesh _part = new TreePartMesh();
        private ProceduralTree _tree;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        // Malha que está em pé agora: a gerada ou a que sobrou depois de cortes
        private Mesh _standingMesh;
        private Mesh _ownedMesh;
        private bool _hasOwnedMesh;
        private int _aimBranch = NoBranch;
        private Vector3 _aimCenter;
        private float _aimRadius;
        private Plane _lockedCut;
        private int _lockedBranch = NoBranch;
        private Vector3 _lockedCenter;
        private float _lockedRadius;

        public Transform Transform => transform;
        public Vector3[] Vertices => _part.Vertices;

        /// <summary>Triângulos do galho do corte (o preview desenha o círculo só nele).</summary>
        public int[] Triangles => _part.TrianglesOf(_state.IsActive ? _lockedBranch : _aimBranch);

        public float CutProgress => _state.Progress;

        /// <summary>A cada golpe, com o progresso do corte (0 a 1).</summary>
        public event Action<float> Chopped;

        /// <summary>Quando um corte termina, com o pedaço que caiu.</summary>
        public event Action<TreePiece> PieceCut;

        private void Awake()
        {
            _tree = GetComponent<ProceduralTree>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
        }

        private void OnEnable()
        {
            _tree.Generated += HandleGenerated;
            // O ProceduralTree pode ter gerado antes de este componente se inscrever
            if (_tree.IsGenerated) HandleGenerated();
        }

        private void OnDisable()
        {
            _tree.Generated -= HandleGenerated;
        }

        private void OnDestroy()
        {
            if (_hasOwnedMesh) Destroy(_ownedMesh);
        }

        public Plane GetCutPlane(Vector3 worldPoint, Vector3 viewForward, Vector3 viewUp, float tilt)
        {
            if (_state.IsActive) return _lockedCut;

            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            _aimBranch = _part.FindBranch(localPoint);
            if (_aimBranch == NoBranch) return default;

            float maxTilt = _tree.Species.Felling.MaxCutTilt;
            return TreeCutter.GetCutPlane(_tree.Skeleton, _aimBranch, localPoint, transform.InverseTransformDirection(viewForward),
                transform.InverseTransformDirection(viewUp), Mathf.Clamp(tilt, -maxTilt, maxTilt), out _aimCenter, out _aimRadius);
        }

        public bool CanCut(Plane localPlane)
        {
            return _tree.IsGenerated && (_state.IsActive || _part.HasBranch(_aimBranch));
        }

        public void Strike(Plane localPlane, Vector3 swingDirection, float damage)
        {
            if (!CanCut(localPlane)) return;

            // O primeiro golpe fixa o corte; os próximos avançam no mesmo lugar
            if (!_state.IsActive)
            {
                _lockedCut = localPlane;
                _lockedBranch = _aimBranch;
                _lockedCenter = _aimCenter;
                _lockedRadius = _aimRadius;
                _state.Begin(_tree.Species.Felling.HealthFor(_aimRadius));
            }

            bool isDone = _state.ApplyHit(damage);
            Chopped?.Invoke(_state.Progress);
            if (isDone) CompleteCut(swingDirection);
        }

        private void HandleGenerated()
        {
            _standingMesh = _tree.Mesh;
            _part.Load(_standingMesh, _tree.Skeleton.BranchCount);
            _state.Clear();
        }

        private void CompleteCut(Vector3 swingDirection)
        {
            int branch = _lockedBranch;
            _state.Clear();
            if (!SharedCutter.TryCut(_standingMesh, _part, _tree.Skeleton, branch, _lockedCut, _lockedCenter, _lockedRadius,
                    out Mesh outer, out Mesh inner)) return;

            ReplaceStandingMesh(inner);
            TreePiece piece = TreePiece.Spawn(transform, name + PieceSuffix, outer, _tree.Skeleton, _tree.Species);

            // A árvore não segura nem empurra o pedaço enquanto ele sai dela
            Collider[] pieceColliders = piece.GetComponentsInChildren<Collider>();
            SetStandingCollision(pieceColliders, false);

            bool isTrunk = branch == TrunkIndex;
            HingeJoint hinge = isTrunk ? TipOver(piece.Body, swingDirection) : null;
            RestoreCollisionLater(piece.Body, hinge, isTrunk, pieceColliders);
            PieceCut?.Invoke(piece);
        }

        private void ReplaceStandingMesh(Mesh mesh)
        {
            if (_hasOwnedMesh) Destroy(_ownedMesh);
            _ownedMesh = mesh;
            _hasOwnedMesh = true;
            _standingMesh = mesh;

            _meshFilter.sharedMesh = mesh;
            _meshRenderer.sharedMaterials = new[] { _tree.Species.BarkMaterial, _tree.Species.CutFaceMaterial };
            Physics.BakeMesh(mesh.GetEntityId(), false, _meshCollider.cookingOptions);
            _meshCollider.sharedMesh = mesh;
            _part.Load(mesh, _tree.Skeleton.BranchCount);
        }

        // Prende o tronco cortado na borda do toco, do lado da queda, e dá o primeiro empurrão: ele só gira
        // nesse eixo (não escorrega, não pula, não gira em volta de si) até a dobradiça soltar
        private HingeJoint TipOver(Rigidbody body, Vector3 swingDirection)
        {
            Vector3 cutNormal = transform.TransformDirection(_lockedCut.normal);
            Vector3 direction = Vector3.ProjectOnPlane(swingDirection, cutNormal);
            if (direction.sqrMagnitude < MinDirectionSqrMagnitude) direction = transform.forward;
            direction.Normalize();

            Vector3 pivot = transform.TransformPoint(_lockedCenter) + direction * _lockedRadius;
            Vector3 axis = Vector3.Cross(cutNormal, direction).normalized;

            var hinge = body.gameObject.AddComponent<HingeJoint>();
            hinge.anchor = body.transform.InverseTransformPoint(pivot);
            hinge.axis = body.transform.InverseTransformDirection(axis);

            Vector3 angularVelocity = axis * (_tree.Species.Felling.TipSpeed * Mathf.Deg2Rad);
            body.angularVelocity = angularVelocity;
            body.linearVelocity = Vector3.Cross(angularVelocity, body.worldCenterOfMass - pivot);
            return hinge;
        }

        // Tronco: solta a dobradiça quando já inclinou o bastante. Galho: espera ele sair da árvore.
        // Depois disso volta a colidir com a árvore
        private async void RestoreCollisionLater(Rigidbody body, HingeJoint hinge, bool hasHinge, Collider[] pieceColliders)
        {
            Quaternion start = body.rotation;
            float limit = hasHinge ? MaxHingeSeconds : BranchClearSeconds;
            try
            {
                for (float time = 0f; time < limit; time += Time.fixedDeltaTime)
                {
                    await Awaitable.FixedUpdateAsync(destroyCancellationToken);
                    // O pedaço pode ter sido cortado de novo (destruído) antes disso
                    if (body == null) return;
                    if (hasHinge && Quaternion.Angle(start, body.rotation) >= HingeReleaseAngle) break;
                }
            }
            catch (OperationCanceledException)
            {
                // A árvore foi destruída: não há colisão para restaurar
                return;
            }

            if (hasHinge) Destroy(hinge);
            SetStandingCollision(pieceColliders, true);
        }

        private void SetStandingCollision(Collider[] pieceColliders, bool isEnabled)
        {
            for (int i = 0; i < pieceColliders.Length; i++)
            {
                // Colisores do pedaço podem ter sido destruídos se ele foi cortado de novo
                if (pieceColliders[i] != null) Physics.IgnoreCollision(_meshCollider, pieceColliders[i], !isEnabled);
            }
        }
    }
}
