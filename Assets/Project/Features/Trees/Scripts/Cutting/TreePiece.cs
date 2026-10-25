using LumberJack.Core;
using LumberJack.Shared;
using LumberJack.Wood;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Pedaço de árvore solto (tronco derrubado, galho cortado): tem física e corta de novo com o machado,
    /// num golpe, como as toras. Continua no espaço local da árvore, então o esqueleto vale para ele.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(RuntimeMesh))]
    public class TreePiece : MonoBehaviour, IAxeTarget
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

        // Cortes acontecem um por vez na thread principal: um cortador para todos os pedaços
        private static readonly TreeCutter SharedCutter = new();

        private readonly TreePartMesh _part = new();
        private Mesh _mesh;
        private int _aimBranch = -1;
        private Vector3 _aimCenter;
        private float _aimRadius;

        public Transform Transform => transform;
        public Vector3[] Vertices => _part.Vertices;

        /// <summary>Triângulos do galho mirado (o preview desenha o círculo só nele).</summary>
        public int[] Triangles => _part.TrianglesOf(_aimBranch);

        public float CutProgress => 0f;
        public Rigidbody Body { get; private set; }
        public TreeSkeleton Skeleton { get; private set; }
        public TreeSpeciesDefinition Species { get; private set; }
        public TreePartMesh Part => _part;

        /// <summary>False quando, depois de quebrar galhos, sobrou só graveto (nada que colida).</summary>
        public bool HasColliders { get; private set; } = true;

        /// <summary>Volume do pedaço passa do mínimo da espécie? (lascas menores somem)</summary>
        public static bool IsBigEnough(Mesh mesh, Transform space, TreeSpeciesDefinition species)
        {
            return VolumeOf(mesh, space) >= species.Felling.MinPieceVolume;
        }

        /// <summary>
        /// Cria um pedaço com física no mesmo lugar e espaço local de <paramref name="source"/>. Null se o pedaço
        /// é só galho fino (gravetos que nem colidem): esses somem, como as lascas.
        /// </summary>
        public static TreePiece Spawn(Transform source, string pieceName, Mesh mesh, TreeSkeleton skeleton,
            TreeSpeciesDefinition species)
        {
            var pieceObject = new GameObject(pieceName) { layer = source.gameObject.layer };
            Transform pieceTransform = pieceObject.transform;
            pieceTransform.SetParent(source.parent, false);
            pieceTransform.SetPositionAndRotation(source.position, source.rotation);
            pieceTransform.localScale = source.localScale;

            pieceObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { species.BarkMaterial, species.CutFaceMaterial };
            TreePiece piece = pieceObject.AddComponent<TreePiece>();
            if (piece.Initialize(mesh, skeleton, species)) return piece;

            Destroy(pieceObject);
            return null;
        }

        public Plane GetCutPlane(Vector3 worldPoint, Vector3 viewForward, Vector3 viewUp, float tilt)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            _aimBranch = _part.FindBranch(localPoint);
            if (_aimBranch < 0) return default;

            return TreeCutter.GetCutPlane(Skeleton, _aimBranch, localPoint, transform.InverseTransformDirection(viewForward),
                transform.InverseTransformDirection(viewUp), tilt, out _aimCenter, out _aimRadius);
        }

        public bool CanCut(Plane localPlane)
        {
            return _part.HasBranch(_aimBranch) &&
                   AxeCutRules.LeavesMinThickness(this, localPlane, Species.Felling.MinCutThickness);
        }

        public void Strike(Plane localPlane, Vector3 swingDirection, float damage)
        {
            if (!CanCut(localPlane)) return;
            if (!SharedCutter.TryCut(_mesh, _part, Skeleton, _aimBranch, localPlane, _aimCenter, _aimRadius,
                    out Mesh outer, out Mesh inner)) return;

            Vector3 separation = transform.TransformDirection(localPlane.normal) * SeparationSpeed;
            TreePiece outerHalf = SpawnHalf(outer, separation);
            TreePiece innerHalf = SpawnHalf(inner, -separation);

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

        /// <summary>
        /// Quebra o galho no ponto <paramref name="localPoint"/> do eixo dele: a ponta some (galho fino que bateu
        /// no chão). False se não deu para cortar ali.
        /// </summary>
        public bool BreakBranch(int branch, Vector3 localPoint, Vector3 localDirection, float radius)
        {
            var plane = new Plane(localDirection, localPoint);
            if (!SharedCutter.TryCut(_mesh, _part, Skeleton, branch, plane, localPoint, radius, out Mesh outer, out Mesh inner))
            {
                return false;
            }

            Destroy(outer);
            SetMesh(inner);
            HasColliders = BuildColliders() > 0;
            UpdateMass();
            return true;
        }

        // False quando o pedaço é só graveto (nenhum galho grosso o bastante para colidir)
        private bool Initialize(Mesh mesh, TreeSkeleton skeleton, TreeSpeciesDefinition species)
        {
            Skeleton = skeleton;
            Species = species;
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

            if (species.Wood != null) gameObject.AddComponent<WoodPiece>().Initialize(species.Wood, false);

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
                GameObject child = transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            TreeFellingSettings felling = Species.Felling;
            return TreePieceColliders.Build(_part, Skeleton, transform, felling.MinColliderRadius, MaxColliders,
                felling.PieceMaterial);
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
            return MeshSlicer.GetVolume(mesh) * Mathf.Abs(scale.x * scale.y * scale.z);
        }

        // Null quando a metade é uma lasca pequena demais e some
        private TreePiece SpawnHalf(Mesh mesh, Vector3 separation)
        {
            if (!IsBigEnough(mesh, transform, Species))
            {
                Destroy(mesh);
                return null;
            }

            TreePiece half = Spawn(transform, name, mesh, Skeleton, Species);
            if (half == null) return null;

            half.Body.linearVelocity = Body.linearVelocity + separation;
            half.Body.angularVelocity = Body.angularVelocity;
            return half;
        }
    }
}
