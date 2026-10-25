using System;
using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Árvore em pé cortada a machado em qualquer galho (ou no tronco), com o círculo de corte das toras.
    /// O primeiro golpe fixa o corte e os seguintes avançam nele; no fim, a parte de fora vira um
    /// <see cref="TreePiece"/> que cai. Cortando o tronco, ela tomba em volta da borda do toco (<see cref="FallingTree"/>).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProceduralTree))]
    public class ChoppableTree : MonoBehaviour, IAxeTarget
    {
        private const int TrunkIndex = 0;
        private const int NoBranch = -1;
        private const float MinDirectionSqrMagnitude = 0.0001f;
        private const string PieceSuffix = " (cortada)";

        // Cortes acontecem um por vez na thread principal: um cortador para todas as árvores
        private static readonly TreeCutter SharedCutter = new();

        private readonly TreeFellingState _state = new();
        private readonly TreePartMesh _part = new();
        private ProceduralTree _tree;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private Collider[] _standingColliders;
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
            _standingColliders = new Collider[] { _meshCollider };
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

            TreeFellingSettings felling = _tree.Species.Felling;
            float clampedTilt = Mathf.Clamp(tilt, -felling.MaxCutTilt, felling.MaxCutTilt);
            if (_aimBranch == TrunkIndex) localPoint.y = Mathf.Max(localPoint.y, LowestTrunkCut(felling.MinStumpHeight, clampedTilt));

            return TreeCutter.GetCutPlane(_tree.Skeleton, _aimBranch, localPoint, transform.InverseTransformDirection(viewForward),
                transform.InverseTransformDirection(viewUp), clampedTilt, out _aimCenter, out _aimRadius);
        }

        public bool CanCut(Plane localPlane)
        {
            if (!_tree.IsGenerated) return false;
            // Corte já começado continua onde está
            if (_state.IsActive) return true;

            return _part.HasBranch(_aimBranch) &&
                   AxeCutRules.LeavesMinThickness(this, localPlane, _tree.Species.Felling.MinCutThickness);
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
            // Lasca pequena demais some em vez de virar um pedaço
            if (!TreePiece.IsBigEnough(outer, transform, _tree.Species))
            {
                Destroy(outer);
                return;
            }
            TreePiece piece = TreePiece.Spawn(transform, name + PieceSuffix, outer, _tree.Skeleton, _tree.Species);
            // Só graveto: some
            if (piece == null) return;

            // A árvore não segura nem empurra o pedaço enquanto ele sai dela
            CollisionSeparation.IgnoreUntilApart(_standingColliders, piece.GetComponentsInChildren<Collider>(), destroyCancellationToken);

            if (branch == TrunkIndex) FellTrunk(piece, swingDirection);
            PieceCut?.Invoke(piece);
        }

        // Centro do corte mais baixo no tronco: o lado de baixo do círculo inclinado fica no mínimo do toco
        private float LowestTrunkCut(float minStumpHeight, float tilt)
        {
            TreeSkeleton skeleton = _tree.Skeleton;
            skeleton.Sample(TrunkIndex, minStumpHeight / skeleton.GetBranch(TrunkIndex).Length, out _, out _, out float radius);
            return minStumpHeight + radius * Mathf.Tan(Mathf.Abs(tilt) * Mathf.Deg2Rad);
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

        // O tronco tomba em volta da borda do toco do lado da queda, com um primeiro empurrão
        private void FellTrunk(TreePiece piece, Vector3 swingDirection)
        {
            Vector3 cutNormal = transform.TransformDirection(_lockedCut.normal);
            Vector3 direction = Vector3.ProjectOnPlane(swingDirection, cutNormal);
            if (direction.sqrMagnitude < MinDirectionSqrMagnitude) direction = transform.forward;
            direction.Normalize();

            Vector3 pivot = transform.TransformPoint(_lockedCenter) + direction * _lockedRadius;
            Vector3 axis = Vector3.Cross(cutNormal, direction).normalized;
            FallingTree.Begin(piece, pivot, axis, _tree.Species.Felling.TipSpeed * Mathf.Deg2Rad, _standingColliders);
        }
    }
}
