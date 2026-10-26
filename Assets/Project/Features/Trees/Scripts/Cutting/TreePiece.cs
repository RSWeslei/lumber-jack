using System.Collections.Generic;
using System.Text;
using LumberJack.Audio;
using LumberJack.Core;
using LumberJack.Shared;
using LumberJack.Wood;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Pedaço de árvore solto (tronco derrubado, galho cortado, tora): tem física e corta de novo com o machado.
    /// Continua no espaço local da árvore, então o esqueleto e as folhas dela valem para ele.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(RuntimeMesh))]
    public class TreePiece : MonoBehaviour, IAxeTarget, IDebugInfo
    {
        private const int MaxColliders = 48;
        private const float MinMass = 1f;
        // Freia o giro do pedaço no chão
        private const float AngularDamping = 1f;
        // Velocidade (m/s) com que os dois pedaços se afastam ao cortar
        private const float SeparationSpeed = 0.4f;
        // Pedaço abaixo disso (m) caiu para fora do mundo: some
        private const float OutOfWorldHeight = -50f;
        private const float OutOfWorldCheckSeconds = 1f;
        // Tag que a refinaria procura
        private const string WoodTag = "Wood";

        // Cortes acontecem um por vez na thread principal: um cortador para todos os pedaços
        private static readonly TreeCutter SharedCutter = new();
        // Tufos de folhas que saem com a outra metade (ou com a ponta quebrada)
        private static readonly List<int> CutClusters = new();
        private static readonly List<int> NoClusters = new();

        private readonly TreePartMesh _part = new();
        private readonly TreeFellingState _state = new();
        // Cascos dos colisores (criados em runtime): destruídos junto com o pedaço ou ao refazer os colisores
        private readonly List<Mesh> _colliderMeshes = new();
        private Mesh _mesh;
        private WoodPiece _woodPiece;
        private bool _hasWoodPiece;
        private TreeLeaves _leaves;
        private Transform _leavesTransform;
        private bool _hasLeaves;
        private AudioSource _audio;
        private TreeSoundsDefinition _sounds;
        private bool _hasSounds;
        private float _nextImpactSound;
        private int _aimBranch = -1;
        private Vector3 _aimCenter;
        private float _aimRadius;
        private Plane _lockedCut;
        private int _lockedBranch = -1;
        private Vector3 _lockedCenter;
        private float _lockedRadius;

        public Transform Transform => transform;
        public Vector3[] Vertices => _part.Vertices;

        /// <summary>Triângulos do galho do corte (o preview desenha o círculo só nele).</summary>
        public int[] Triangles => _part.TrianglesOf(_state.IsActive ? _lockedBranch : _aimBranch);

        public float CutProgress => _state.Progress;
        public Rigidbody Body { get; private set; }
        public TreeSkeleton Skeleton { get; private set; }
        public TreeSpeciesDefinition Species { get; private set; }
        public TreeFoliage Foliage { get; private set; }

        /// <summary>Tipo de madeira (preço); null = sem valor. Passa para as metades quando corta.</summary>
        public WoodDefinition Wood { get; private set; }
        public TreePartMesh Part => _part;

        /// <summary>False quando, depois de quebrar galhos, sobrou só graveto (nada que colida).</summary>
        public bool HasColliders { get; private set; } = true;

        private void OnCollisionEnter(Collision collision)
        {
            if (!_hasSounds || Time.time < _nextImpactSound) return;

            float volume = _sounds.ImpactVolume(collision.relativeVelocity.magnitude);
            if (volume <= 0f) return;

            _nextImpactSound = Time.time + _sounds.ImpactInterval;
            _sounds.Impact.Play(_audio, volume);
        }

        private void OnDestroy()
        {
            DestroyColliderMeshes();
        }

        /// <summary>Volume do pedaço passa do mínimo da espécie? (lascas menores somem)</summary>
        public static bool IsBigEnough(Mesh mesh, Transform space, TreeSpeciesDefinition species)
        {
            return VolumeOf(mesh, space) >= species.Felling.MinPieceVolume;
        }

        /// <summary>Pedaço sem folhas (tora).</summary>
        public static TreePiece Spawn(Transform source, string pieceName, Mesh mesh, TreeSkeleton skeleton,
            TreeSpeciesDefinition species, WoodDefinition wood)
        {
            return Spawn(source, pieceName, mesh, skeleton, species, wood, null, NoClusters);
        }

        /// <summary>
        /// Cria um pedaço com física no mesmo lugar e espaço local de <paramref name="source"/>, com os tufos
        /// <paramref name="leafClusters"/> da folhagem. Null se o pedaço é só galho fino (gravetos que nem
        /// colidem): esses somem, como as lascas, com as folhas deles.
        /// </summary>
        public static TreePiece Spawn(Transform source, string pieceName, Mesh mesh, TreeSkeleton skeleton,
            TreeSpeciesDefinition species, WoodDefinition wood, TreeFoliage foliage, IReadOnlyList<int> leafClusters)
        {
            var pieceObject = new GameObject(pieceName) { layer = source.gameObject.layer, tag = WoodTag };
            Transform pieceTransform = pieceObject.transform;
            pieceTransform.SetParent(source.parent, false);
            pieceTransform.SetPositionAndRotation(source.position, source.rotation);
            pieceTransform.localScale = source.localScale;

            pieceObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { species.BarkMaterial, species.CutFaceMaterial };
            TreePiece piece = pieceObject.AddComponent<TreePiece>();
            if (!piece.Initialize(mesh, skeleton, species, wood))
            {
                Destroy(pieceObject);
                return null;
            }

            piece.Foliage = foliage;
            if (foliage != null && leafClusters.Count > 0) piece.AddLeaves(leafClusters);
            return piece;
        }

        public Plane GetCutPlane(Vector3 worldPoint, Vector3 viewForward, Vector3 viewUp, float tilt)
        {
            if (_state.IsActive) return _lockedCut;

            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            _aimBranch = _part.FindBranch(localPoint);
            if (_aimBranch < 0) return default;

            return TreeCutter.GetCutPlane(Skeleton, _aimBranch, localPoint, transform.InverseTransformDirection(viewForward),
                transform.InverseTransformDirection(viewUp), tilt, out _aimCenter, out _aimRadius);
        }

        public bool CanCut(Plane localPlane)
        {
            // Corte já começado continua onde está
            if (_state.IsActive) return true;

            return _part.HasBranch(_aimBranch) &&
                   AxeCutRules.LeavesMinThickness(this, localPlane, Species.Felling.MinCutThickness);
        }

        /// <summary>Como na árvore em pé: o primeiro golpe fixa o corte e os golpes somam dano conforme a grossura.</summary>
        public void Strike(Plane localPlane, Vector3 swingDirection, float damage)
        {
            if (!CanCut(localPlane)) return;

            if (!_state.IsActive)
            {
                _lockedCut = localPlane;
                _lockedBranch = _aimBranch;
                _lockedCenter = _aimCenter;
                _lockedRadius = _aimRadius;
                // Raio no mundo: pedaço de árvore jovem (menor) pede menos golpes
                _state.Begin(Species.Felling.HealthFor(_aimRadius * transform.lossyScale.x));
            }
            if (!_state.ApplyHit(damage)) return;

            _state.Clear();
            if (!SharedCutter.TryCut(_mesh, _part, Skeleton, _lockedBranch, _lockedCut, _lockedCenter, _lockedRadius,
                    out Mesh outer, out Mesh inner)) return;

            if (_hasLeaves) _leaves.Cut(Skeleton, _lockedBranch, _lockedCut, CutClusters);
            else CutClusters.Clear();
            Vector3 separation = transform.TransformDirection(_lockedCut.normal) * SeparationSpeed;
            TreePiece outerHalf = SpawnHalf(outer, separation, CutClusters);
            TreePiece innerHalf = SpawnHalf(inner, -separation, _hasLeaves ? _leaves.Clusters : NoClusters);

            // As metades nascem encostadas no corte: não se empurram até se afastarem
            if (outerHalf != null && innerHalf != null)
            {
                CollisionSeparation.IgnoreUntilApart(outerHalf.GetComponentsInChildren<Collider>(),
                    innerHalf.GetComponentsInChildren<Collider>(), outerHalf.destroyCancellationToken);
            }

            // Some na hora para os pedaços novos não colidirem com este
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        public void AppendDebugInfo(StringBuilder text)
        {
            text.Append("Pedaço de árvore: ").AppendLine(Species.name);
            text.Append("Corte: ").Append((CutProgress * 100f).ToString("0")).AppendLine("%");
        }

        /// <summary>
        /// Quebra o galho no ponto <paramref name="localPoint"/> do eixo dele: a ponta some (galho fino que bateu
        /// no chão). False se não deu para cortar ali.
        /// </summary>
        /// <summary>Rangido do tronco começando a tombar.</summary>
        public void PlayFallSound()
        {
            if (_hasSounds) _sounds.Creak.Play(_audio);
        }

        /// <summary>Pancada do tronco deitando no chão.</summary>
        public void PlayLandSound()
        {
            if (_hasSounds) _sounds.Land.Play(_audio);
        }

        public bool BreakBranch(int branch, Vector3 localPoint, Vector3 localDirection, float radius)
        {
            var plane = new Plane(localDirection, localPoint);
            if (!SharedCutter.TryCut(_mesh, _part, Skeleton, branch, plane, localPoint, radius, out Mesh outer, out Mesh inner))
            {
                return false;
            }

            Destroy(outer);
            SetMesh(inner);
            // As folhas da ponta quebrada somem com ela
            if (_hasLeaves) _leaves.Cut(Skeleton, branch, plane, CutClusters);
            HasColliders = BuildColliders() > 0;
            UpdateMass();
            if (_hasWoodPiece) _woodPiece.RecalculateValue();
            if (_hasSounds) _sounds.BranchBreak.Play(_audio);
            return true;
        }

        // False quando o pedaço é só graveto (nenhum galho grosso o bastante para colidir)
        private bool Initialize(Mesh mesh, TreeSkeleton skeleton, TreeSpeciesDefinition species, WoodDefinition wood)
        {
            Skeleton = skeleton;
            Species = species;
            Wood = wood;
            SetMesh(mesh);
            // Graveto não colide: ficaria afundado no chão ou cairia através dele
            if (BuildColliders() == 0) return false;

            Rigidbody body = gameObject.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.angularDamping = AngularDamping;
            // Uma árvore tombando gira no máximo ~2 rad/s: o limite só corta giros absurdos
            body.maxAngularVelocity = species.Felling.MaxSpinSpeed * Mathf.Deg2Rad;
            // Se algo se atravessar mesmo assim, a física separa devagar em vez de arremessar o pedaço
            body.maxDepenetrationVelocity = species.Felling.MaxSeparationSpeed;
            Body = body;
            UpdateMass();
            _audio = SpatialAudio.AddSource(gameObject);
            _sounds = species.Sounds;
            _hasSounds = _sounds != null;

            if (wood != null)
            {
                _woodPiece = gameObject.AddComponent<WoodPiece>();
                _woodPiece.Initialize(wood, false);
                _hasWoodPiece = true;
            }

            // Pedaço pode nascer atravessando o toco ou outro pedaço (ex.: tronco que tombou por cima do toco)
            CollisionSeparation.IgnoreSpawnOverlaps(body, GetComponentsInChildren<Collider>(), 1 << gameObject.layer,
                destroyCancellationToken);
            RemoveWhenOutOfWorld();
            return true;
        }

        // Rede de segurança: pedaço que atravessou o chão some em vez de cair para sempre
        private async void RemoveWhenOutOfWorld()
        {
            try
            {
                while (transform.position.y > OutOfWorldHeight)
                {
                    await Awaitable.WaitForSecondsAsync(OutOfWorldCheckSeconds, destroyCancellationToken);
                }
            }
            catch (System.OperationCanceledException)
            {
                // O pedaço foi destruído (cortado de novo, vendido)
                return;
            }
            Destroy(gameObject);
        }

        private void AddLeaves(IReadOnlyList<int> clusters)
        {
            _leaves = TreeLeaves.Create(transform);
            _leaves.Show(Foliage, Species.Leaves, clusters);
            _leavesTransform = _leaves.transform;
            _hasLeaves = true;
        }

        private void SetMesh(Mesh mesh)
        {
            _mesh = mesh;
            GetComponent<RuntimeMesh>().Assign(mesh);
            _part.Load(mesh, Skeleton.BranchCount);
        }

        // Troca as caixas pelas do formato atual e devolve quantas criou
        private int BuildColliders()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                // Desativa antes: Destroy só acontece no fim do frame e a física rodaria com as caixas velhas
                Transform childTransform = transform.GetChild(i);
                if (_hasLeaves && ReferenceEquals(childTransform, _leavesTransform)) continue;

                GameObject child = childTransform.gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            DestroyColliderMeshes();

            TreeFellingSettings felling = Species.Felling;
            return TreePieceColliders.Build(_part, Skeleton, transform, felling.MinColliderRadius, MaxColliders,
                felling.PieceMaterial, _colliderMeshes);
        }

        private void DestroyColliderMeshes()
        {
            foreach (Mesh mesh in _colliderMeshes) Destroy(mesh);
            _colliderMeshes.Clear();
        }

        private void UpdateMass()
        {
            Body.mass = Mathf.Max(MinMass, VolumeOf(_mesh, transform) * Species.Felling.Density);
            Body.ResetCenterOfMass();
            Body.ResetInertiaTensor();
        }

        private static float VolumeOf(Mesh mesh, Transform space)
        {
            Vector3 scale = space.lossyScale;
            return MeshVolume.Of(mesh) * Mathf.Abs(scale.x * scale.y * scale.z);
        }

        // Null quando a metade é uma lasca pequena demais e some
        private TreePiece SpawnHalf(Mesh mesh, Vector3 separation, IReadOnlyList<int> leafClusters)
        {
            if (!IsBigEnough(mesh, transform, Species))
            {
                Destroy(mesh);
                return null;
            }

            TreePiece half = Spawn(transform, name, mesh, Skeleton, Species, Wood, Foliage, leafClusters);
            if (half == null) return null;

            half.Body.linearVelocity = Body.linearVelocity + separation;
            half.Body.angularVelocity = Body.angularVelocity;
            return half;
        }
    }
}
