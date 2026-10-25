using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Tronco derrubado caindo: tomba em volta da borda do toco com a gravidade calculada (sem PhysX, sem quique).
    /// Galho fino que bate em algo quebra e some; quando uma parte grossa bate, a árvore deitou e vira física normal.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TreePiece))]
    public class FallingTree : MonoBehaviour
    {
        private const int TrunkIndex = 0;
        // Passos da busca binária pelo ângulo exato em que a parte grossa encosta
        private const int ContactSearchSteps = 6;
        // Redes de segurança: nada na frente (penhasco) ou a queda nunca termina
        private const float MaxFallAngle = 120f * Mathf.Deg2Rad;
        private const float MaxFallSeconds = 10f;
        private const int MaxHits = 16;
        private const int NoSegment = -1;

        // Consultas acontecem uma por vez na thread principal: um buffer para todas as árvores
        private static readonly Collider[] HitBuffer = new Collider[MaxHits];

        private readonly TreeFallMotion _motion = new();
        private readonly List<Segment> _segments = new();
        private TreePiece _piece;
        private Rigidbody _body;
        private Collider[] _ignored;
        private int _layerMask;
        private float _scale;
        private float _breakRadius;
        private float _landingSpinKeep;
        // Por galho: trecho a quebrar (o mais perto da base que bateu) e galhos que não deu para quebrar
        private int[] _breakAt;
        private bool[] _isUnbreakable;

        /// <summary>
        /// Começa a queda do tronco recém-cortado em volta do eixo <paramref name="axis"/> que passa por
        /// <paramref name="pivot"/> (mundo). <paramref name="ignored"/> são os colisores do toco.
        /// </summary>
        public static void Begin(TreePiece piece, Vector3 pivot, Vector3 axis, float startSpeed, Collider[] ignored)
        {
            FallingTree fall = piece.gameObject.AddComponent<FallingTree>();
            fall.Run(piece, pivot, axis, startSpeed, ignored);
        }

        private void Awake()
        {
            // O jogador não segura a árvore (dano a quem está embaixo fica para depois)
            _layerMask = Physics.AllLayers & ~LayerMask.GetMask("Player");
        }

        private void Run(TreePiece piece, Vector3 pivot, Vector3 axis, float startSpeed, Collider[] ignored)
        {
            _piece = piece;
            _body = piece.Body;
            _ignored = ignored;
            _scale = transform.lossyScale.x;
            TreeFellingSettings felling = piece.Species.Felling;
            _breakRadius = felling.BranchBreakRadius;
            _landingSpinKeep = felling.LandingSpinKeep;
            int branchCount = piece.Skeleton.BranchCount;
            _breakAt = new int[branchCount];
            _isUnbreakable = new bool[branchCount];

            // Corpo cinemático só aceita a detecção contínua especulativa
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _body.isKinematic = true;
            _motion.Begin(pivot, axis, _body.position, _body.rotation, _body.worldCenterOfMass,
                InertiaPerMass(_body, pivot, axis), startSpeed, Physics.gravity);
            BuildSegments();
            Fall();
        }

        private async void Fall()
        {
            try
            {
                await FallAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Cortaram o tronco no meio da queda: as metades caem com física normal
            }
        }

        private async Awaitable FallAsync(CancellationToken token)
        {
            for (float time = 0f; time < MaxFallSeconds; time += Time.fixedDeltaTime)
            {
                await Awaitable.FixedUpdateAsync(token);
                float previous = _motion.Angle;
                _motion.Step(Time.fixedDeltaTime);

                if (Touches(_motion.Angle, false))
                {
                    float landing = FindContactAngle(previous, _motion.Angle);
                    BreakTouchingBranches(landing);
                    Land(landing);
                    return;
                }

                BreakTouchingBranches(_motion.Angle);
                _motion.GetPose(_motion.Angle, out Vector3 position, out Quaternion rotation);
                _body.MovePosition(position);
                _body.MoveRotation(rotation);
                if (_motion.Angle >= MaxFallAngle) break;
            }
            Land(_motion.Angle);
        }

        // Deitou: põe na pose do contato e devolve para a física com só um resto do giro
        private void Land(float angle)
        {
            if (!_piece.HasColliders)
            {
                Destroy(gameObject);
                return;
            }

            _motion.GetPose(angle, out Vector3 position, out Quaternion rotation);
            _body.position = position;
            _body.rotation = rotation;
            _body.isKinematic = false;
            _body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            Vector3 spin = _motion.Axis * (_motion.Speed * _landingSpinKeep);
            _body.angularVelocity = spin;
            _body.linearVelocity = Vector3.Cross(spin, _body.worldCenterOfMass - _motion.Pivot);
            Destroy(this);
        }

        // Ângulo em que uma parte grossa encosta pela primeira vez, entre um ângulo livre e um com contato
        private float FindContactAngle(float free, float touching)
        {
            for (int i = 0; i < ContactSearchSteps; i++)
            {
                float middle = (free + touching) * 0.5f;
                if (Touches(middle, false)) touching = middle;
                else free = middle;
            }
            return free;
        }

        private void BreakTouchingBranches(float angle)
        {
            for (int b = 0; b < _breakAt.Length; b++) _breakAt[b] = NoSegment;
            if (!Touches(angle, true)) return;

            bool hasBroken = false;
            for (int b = 0; b < _breakAt.Length; b++)
            {
                int index = _breakAt[b];
                if (index == NoSegment) continue;

                Segment segment = _segments[index];
                if (_piece.BreakBranch(b, segment.From, segment.Direction, segment.Radius)) hasBroken = true;
                // Não cortou (ex.: o galho todo está no ponto): deixa atravessar em vez de travar a queda
                else _isUnbreakable[b] = true;
            }
            if (hasBroken) BuildSegments();
        }

        // Galhos finos: só anotam onde quebrar (onlyThin). Partes grossas: devolvem true no primeiro contato
        private bool Touches(float angle, bool onlyThin)
        {
            _motion.GetPose(angle, out Vector3 position, out Quaternion rotation);
            Matrix4x4 pose = Matrix4x4.TRS(position, rotation, Vector3.one * _scale);
            bool hasThinContact = false;
            for (int i = 0; i < _segments.Count; i++)
            {
                Segment segment = _segments[i];
                if (segment.IsThin != onlyThin || _isUnbreakable[segment.Branch]) continue;
                if (!Overlaps(pose, segment)) continue;
                if (!onlyThin) return true;

                hasThinContact = true;
                int current = _breakAt[segment.Branch];
                if (current == NoSegment || segment.Distance < _segments[current].Distance) _breakAt[segment.Branch] = i;
            }
            return hasThinContact;
        }

        private bool Overlaps(Matrix4x4 pose, Segment segment)
        {
            Vector3 from = pose.MultiplyPoint3x4(segment.From);
            Vector3 to = pose.MultiplyPoint3x4(segment.To);
            float radius = segment.Radius * _scale;
            // As pontas da cápsula ficam dentro do trecho: a base do tronco não "encosta" no chão ao lado do toco
            Vector3 along = to - from;
            float length = along.magnitude;
            if (length > radius * 2f)
            {
                Vector3 inset = along * (radius / length);
                from += inset;
                to -= inset;
            }
            else
            {
                from = to = (from + to) * 0.5f;
            }

            int count = Physics.OverlapCapsuleNonAlloc(from, to, radius, HitBuffer, _layerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (IsObstacle(HitBuffer[i])) return true;
            }
            return false;
        }

        // Chão, cenário e árvores em pé seguram a queda; o próprio tronco, o toco e coisas soltas (são empurradas) não
        private bool IsObstacle(Collider hit)
        {
            Rigidbody hitBody = hit.attachedRigidbody;
            // attachedRigidbody devolve null de verdade quando não há corpo: ReferenceEquals basta
            if (!ReferenceEquals(hitBody, null) && (ReferenceEquals(hitBody, _body) || !hitBody.isKinematic)) return false;

            return _ignored.All(ignored => !ReferenceEquals(hit, ignored));
        }

        // Trechos do eixo entre nós, de todos os galhos que o tronco ainda tem (até os finos, que não têm colisor)
        private void BuildSegments()
        {
            _segments.Clear();
            TreeSkeleton skeleton = _piece.Skeleton;
            for (int b = 0; b < skeleton.BranchCount; b++)
            {
                if (!TreePieceColliders.TryGetCoveredRange(_piece.Part, skeleton, b, out float min, out float max)) continue;

                TreeBranch branch = skeleton.GetBranch(b);
                for (int n = branch.FirstNode; n < branch.LastNode; n++)
                {
                    TreeNode start = skeleton.GetNode(n);
                    TreeNode end = skeleton.GetNode(n + 1);
                    float from = Mathf.Max(start.Distance, min);
                    float to = Mathf.Min(end.Distance, max);
                    float span = end.Distance - start.Distance;
                    if (to <= from || span <= 0f) continue;

                    Vector3 fromPoint = Vector3.Lerp(start.Position, end.Position, (from - start.Distance) / span);
                    Vector3 toPoint = Vector3.Lerp(start.Position, end.Position, (to - start.Distance) / span);
                    float radius = Mathf.Max(start.Radius, end.Radius);
                    bool isThin = b != TrunkIndex && radius * _scale < _breakRadius;
                    _segments.Add(new Segment(b, fromPoint, toPoint, start.Direction, radius, from, isThin));
                }
            }
        }

        // Momento de inércia em volta do eixo pelo pivô, por unidade de massa (eixos principais + Steiner)
        private static float InertiaPerMass(Rigidbody body, Vector3 pivot, Vector3 axis)
        {
            Vector3 localAxis = Quaternion.Inverse(body.rotation * body.inertiaTensorRotation) * axis.normalized;
            Vector3 tensor = body.inertiaTensor;
            float aroundCenter = tensor.x * localAxis.x * localAxis.x + tensor.y * localAxis.y * localAxis.y +
                                 tensor.z * localAxis.z * localAxis.z;
            Vector3 offset = Vector3.ProjectOnPlane(body.worldCenterOfMass - pivot, axis);
            return aroundCenter / body.mass + offset.sqrMagnitude;
        }

        private readonly struct Segment
        {
            public Segment(int branch, Vector3 from, Vector3 to, Vector3 direction, float radius, float distance, bool isThin)
            {
                Branch = branch;
                From = from;
                To = to;
                Direction = direction;
                Radius = radius;
                Distance = distance;
                IsThin = isThin;
            }

            public int Branch { get; }
            public Vector3 From { get; }
            public Vector3 To { get; }
            public Vector3 Direction { get; }
            public float Radius { get; }
            // Distância (m) da base do galho até o começo do trecho
            public float Distance { get; }
            public bool IsThin { get; }
        }
    }
}
