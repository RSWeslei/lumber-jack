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
        private const float AngularDamping = 0.5f;
        // Velocidade (m/s) com que os dois pedaços se afastam ao cortar
        private const float SeparationSpeed = 0.4f;

        // Cortes acontecem um por vez na thread principal: um cortador para todos os pedaços
        private static readonly TreeCutter SharedCutter = new TreeCutter();

        private readonly TreePartMesh _part = new TreePartMesh();
        private TreeSkeleton _skeleton;
        private TreeSpeciesDefinition _species;
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

        /// <summary>Cria um pedaço com física no mesmo lugar e espaço local de <paramref name="source"/>.</summary>
        public static TreePiece Spawn(Transform source, string pieceName, Mesh mesh, TreeSkeleton skeleton,
            TreeSpeciesDefinition species)
        {
            var pieceObject = new GameObject(pieceName) { layer = source.gameObject.layer };
            Transform pieceTransform = pieceObject.transform;
            pieceTransform.SetParent(source.parent, false);
            pieceTransform.SetPositionAndRotation(source.position, source.rotation);
            pieceTransform.localScale = source.localScale;

            pieceObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { species.BarkMaterial, species.CutFaceMaterial };
            var piece = pieceObject.AddComponent<TreePiece>();
            piece.Initialize(mesh, skeleton, species);
            return piece;
        }

        public Plane GetCutPlane(Vector3 worldPoint, Vector3 viewForward, Vector3 viewUp, float tilt)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            _aimBranch = _part.FindBranch(localPoint);
            if (_aimBranch < 0) return default;

            return TreeCutter.GetCutPlane(_skeleton, _aimBranch, localPoint, transform.InverseTransformDirection(viewForward),
                transform.InverseTransformDirection(viewUp), tilt, out _aimCenter, out _aimRadius);
        }

        public bool CanCut(Plane localPlane)
        {
            return _part.HasBranch(_aimBranch);
        }

        public void Strike(Plane localPlane, Vector3 swingDirection, float damage)
        {
            if (!CanCut(localPlane)) return;
            if (!SharedCutter.TryCut(_mesh, _part, _skeleton, _aimBranch, localPlane, _aimCenter, _aimRadius,
                    out Mesh outer, out Mesh inner)) return;

            Vector3 separation = transform.TransformDirection(localPlane.normal) * SeparationSpeed;
            SpawnHalf(outer, separation);
            SpawnHalf(inner, -separation);

            // Some na hora para os pedaços novos não colidirem com este
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void Initialize(Mesh mesh, TreeSkeleton skeleton, TreeSpeciesDefinition species)
        {
            _mesh = mesh;
            _skeleton = skeleton;
            _species = species;
            GetComponent<RuntimeMesh>().Assign(mesh);
            _part.Load(mesh, skeleton.BranchCount);
            TreePieceColliders.Build(_part, skeleton, transform, species.Felling.MinColliderRadius, MaxColliders);

            Rigidbody body = gameObject.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.angularDamping = AngularDamping;
            Vector3 scale = transform.lossyScale;
            float volume = MeshSlicer.GetVolume(mesh) * Mathf.Abs(scale.x * scale.y * scale.z);
            body.mass = Mathf.Max(MinMass, volume * species.Felling.Density);
            body.ResetCenterOfMass();
            body.ResetInertiaTensor();
            Body = body;

            if (species.Wood != null) gameObject.AddComponent<WoodPiece>().Initialize(species.Wood, false);
        }

        private void SpawnHalf(Mesh mesh, Vector3 separation)
        {
            TreePiece half = Spawn(transform, name, mesh, _skeleton, _species);
            half.Body.linearVelocity = Body.linearVelocity + separation;
            half.Body.angularVelocity = Body.angularVelocity;
        }
    }
}
