using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Só para a cena de teste: corta galhos, derruba as árvores com corte inclinado (umas caem sobre as outras),
    /// corta os pedaços caídos duas vezes e mede se eles assentam (giro e quique). Escreve um relatório e sai do Play.
    /// </summary>
    public class TreeFellingTestDriver : MonoBehaviour
    {
        private const string ReportPath = "Logs/tree-physics-test.txt";

        [Header("Roteiro")]
        [SerializeField] private float _startDelay = 1f;
        [Tooltip("Altura (m) do golpe no tronco")]
        [SerializeField] private float _cutHeight = 1f;
        [Tooltip("Dano por golpe (alto = corta num golpe)")]
        [SerializeField] private float _damage = 10000f;
        [Tooltip("Galhos cortados de cada árvore antes de derrubar")]
        [SerializeField] private int _branchesToCut = 2;
        [Tooltip("Segundos entre cortar os galhos e derrubar")]
        [SerializeField] private float _fellDelay = 2f;
        [Tooltip("Inclinação (graus) do corte do tronco, alternando o lado")]
        [SerializeField] private float _fellTilt = 25f;
        [Tooltip("Segundos depois de derrubar para cortar os pedaços caídos (duas rodadas)")]
        [SerializeField] private float _fallenCutDelay = 6f;
        [SerializeField] private float _secondCutDelay = 3f;
        [Tooltip("Inclinação (graus) dos cortes nos pedaços caídos")]
        [SerializeField] private float _fallenTilt = 40f;
        [Tooltip("Duração total do teste (s)")]
        [SerializeField] private float _duration = 22f;
        [SerializeField] private bool _exitPlayModeWhenDone = true;

        [Header("Critérios (medidos depois que o pedaço já devia ter caído)")]
        [Tooltip("Segundos de vida do pedaço antes de começar a medir")]
        [SerializeField] private float _settleTime = 3.5f;
        [Tooltip("Giro máximo aceito depois de cair (rad/s)")]
        [SerializeField] private float _maxSettledSpin = 0.6f;
        [Tooltip("Velocidade para cima máxima aceita depois de cair (m/s): mais que isso é quique")]
        [SerializeField] private float _maxSettledUpSpeed = 0.5f;
        [Tooltip("No fim do teste o pedaço precisa estar mais devagar que isto (m/s e rad/s)")]
        [SerializeField] private float _restSpeed = 0.15f;
        [Tooltip("Quanto (m) o pé do pedaço pode subir acima de onde nasceu (o chute do tronco ao cair)")]
        [SerializeField] private float _maxRise = 0.5f;

        private readonly Dictionary<TreePiece, PieceStats> _stats = new Dictionary<TreePiece, PieceStats>();
        private float _startTime;
        private int _strikes;
        private int _blockedStrikes;

        // Teste roda sozinho: Start async porque é o começo do roteiro
        private async void Start()
        {
            // O teste pode rodar com o editor sem foco
            Application.runInBackground = true;
            _startTime = Time.time;
            try
            {
                await RunAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Saiu do Play no meio do teste
            }
            catch (Exception exception)
            {
                // Erro no teste: registra e sai do Play em vez de deixar o editor parado
                File.WriteAllText(ReportPath, "ERRO no teste: " + exception);
                Debug.LogException(exception);
                if (_exitPlayModeWhenDone) ExitPlayMode();
            }
        }

        private async Awaitable RunAsync(CancellationToken token)
        {
            await SampleUntil(_startDelay, token);
            CutBranches();
            float fellTime = _startDelay + _fellDelay;
            await SampleUntil(fellTime, token);
            FellAllTrees();
            await SampleUntil(fellTime + _fallenCutDelay, token);
            CutFallenPieces(_fallenTilt);
            await SampleUntil(fellTime + _fallenCutDelay + _secondCutDelay, token);
            CutFallenPieces(-_fallenTilt);
            await SampleUntil(_duration, token);

            WriteReport();
            if (_exitPlayModeWhenDone) ExitPlayMode();
        }

        private async Awaitable SampleUntil(float testTime, CancellationToken token)
        {
            while (Time.time - _startTime < testTime)
            {
                await Awaitable.FixedUpdateAsync(token);
                Sample();
            }
        }

        // Corta galhos do primeiro nível a 40% do comprimento deles
        private void CutBranches()
        {
            ChoppableTree[] trees = FindObjectsByType<ChoppableTree>();
            for (int i = 0; i < trees.Length; i++)
            {
                TreeSkeleton skeleton = trees[i].GetComponent<ProceduralTree>().Skeleton;
                int cut = 0;
                for (int b = 1; b < skeleton.BranchCount && cut < _branchesToCut; b++)
                {
                    if (skeleton.GetBranch(b).Level != 1) continue;

                    skeleton.Sample(b, 0.4f, out Vector3 localPoint, out _, out _);
                    Strike(trees[i], trees[i].transform.TransformPoint(localPoint), Vector3.forward, 0f);
                    cut++;
                }
            }
        }

        private void FellAllTrees()
        {
            ChoppableTree[] trees = FindObjectsByType<ChoppableTree>();
            for (int i = 0; i < trees.Length; i++)
            {
                // Direções e inclinações diferentes para cobrir quedas em todos os lados
                Vector3 swing = Quaternion.AngleAxis(i * 77f, Vector3.up) * Vector3.forward;
                float tilt = i % 2 == 0 ? _fellTilt : -_fellTilt;
                Strike(trees[i], trees[i].transform.position + Vector3.up * _cutHeight, swing, tilt);
            }
        }

        private void CutFallenPieces(float tilt)
        {
            TreePiece[] pieces = FindObjectsByType<TreePiece>();
            for (int i = 0; i < pieces.Length; i++)
            {
                // O centro de massa pode ficar fora do galho (no ar entre galhos): mira então no meio de uma caixa
                if (Strike(pieces[i], pieces[i].Body.worldCenterOfMass, Vector3.forward, tilt)) continue;

                BoxCollider[] boxes = pieces[i].GetComponentsInChildren<BoxCollider>();
                Strike(pieces[i], boxes[boxes.Length / 2].bounds.center, Vector3.forward, tilt);
            }
        }

        private bool Strike(Core.IAxeTarget target, Vector3 point, Vector3 swing, float tilt)
        {
            Plane plane = target.GetCutPlane(point, swing, Vector3.up, tilt);
            _strikes++;
            if (!target.CanCut(plane))
            {
                _blockedStrikes++;
                return false;
            }
            target.Strike(plane, swing, _damage);
            return true;
        }

        // Código de teste: alocar aqui não importa
        private void Sample()
        {
            float now = Time.time;
            TreePiece[] pieces = FindObjectsByType<TreePiece>();
            for (int i = 0; i < pieces.Length; i++)
            {
                TreePiece piece = pieces[i];
                if (!_stats.TryGetValue(piece, out PieceStats stats))
                {
                    stats = new PieceStats(piece.name, now - _startTime, piece.Body.mass, piece.transform,
                        piece.GetComponentsInChildren<BoxCollider>());
                    _stats.Add(piece, stats);
                }

                Rigidbody body = piece.Body;
                stats.RecordHinge(now - _startTime, piece.TryGetComponent(out HingeJoint _));
                stats.RecordAxis(body.angularVelocity);
                stats.Record(now - _startTime, body.angularVelocity.magnitude, body.linearVelocity.y, body.linearVelocity.magnitude,
                    body.IsSleeping(), _settleTime);
            }
        }

        private void WriteReport()
        {
            var report = new StringBuilder();
            int total = 0;
            int passed = 0;
            foreach (KeyValuePair<TreePiece, PieceStats> entry in _stats)
            {
                PieceStats stats = entry.Value;
                // Pedaço cortado de novo (destruído) só conta o chute do pé na queda; as metades contam o resto
                bool isGone = entry.Key == null;
                if (isGone && stats.MaxRise <= _maxRise) continue;
                if (!isGone && !stats.HasSettleSamples) continue;

                bool isOk = stats.MaxSpin <= _maxSettledSpin && stats.MaxUpSpeed <= _maxSettledUpSpeed &&
                            stats.FinalSpeed <= _restSpeed && stats.FinalSpin <= _restSpeed && stats.MaxRise <= _maxRise;
                if (isGone) isOk = false;
                total++;
                if (isOk) passed++;
                report.AppendLine($"{(isOk ? "OK  " : "RUIM")} {stats.Name,-28} massa {stats.Mass,7:F0} kg  colisores {stats.Colliders,2}  " +
                                  $"giro_max {stats.MaxSpin,5:F2} rad/s (nasceu {stats.SpawnTime,4:F1}s, pico {stats.MaxSpinTime,4:F1}s)  subida_max {stats.MaxUpSpeed,5:F2} m/s  " +
                                  $"final {stats.FinalSpeed,5:F2} m/s {stats.FinalSpin,5:F2} rad/s  {(stats.IsSleeping ? "dormindo" : "acordado")}  " +
                                  $"pé subiu {stats.MaxRise,4:F2} m em {stats.MaxRiseTime,4:F1}s{(isGone ? " (cortado depois)" : string.Empty)}" +
                                  (stats.HingeReleaseTime > 0f ? $"  dobradiça soltou {stats.HingeReleaseTime:F1}s" : string.Empty) +
                                  $"  na subida: giro {stats.RiseSpin:F2} rad/s a {stats.RiseSpinAngle:F0}° do tronco, tronco a {stats.RiseTrunkTilt:F0}° do chão");
                if (!isOk && !isGone) report.AppendLine("     encostado em: " + DescribeContacts(entry.Key));
            }

            string header = $"Teste de queda de árvores: {passed}/{total} OK ({DateTime.Now:HH:mm:ss}), " +
                            $"cortes bloqueados {_blockedStrikes}/{_strikes}\n" +
                            $"Critérios depois de {_settleTime:F0}s de vida: giro <= {_maxSettledSpin} rad/s, subida <= {_maxSettledUpSpeed} m/s, " +
                            $"final <= {_restSpeed}, pé subindo <= {_maxRise} m\n";
            File.WriteAllText(ReportPath, header + report);
            Debug.Log("[TreeTest] " + header);
        }

        // Diagnóstico: o que está encostando nos colisores do pedaço (e se a colisão com ele está ignorada)
        private static string DescribeContacts(TreePiece piece)
        {
            var names = new HashSet<string>();
            Collider[] own = piece.GetComponentsInChildren<Collider>();
            for (int i = 0; i < own.Length; i++)
            {
                if (!(own[i] is BoxCollider box)) continue;

                Transform t = box.transform;
                Vector3 halfExtents = Vector3.Scale(box.size, t.lossyScale) * 0.5f + Vector3.one * 0.03f;
                Collider[] hits = Physics.OverlapBox(t.TransformPoint(box.center), halfExtents, t.rotation, Physics.AllLayers,
                    QueryTriggerInteraction.Ignore);
                for (int h = 0; h < hits.Length; h++)
                {
                    if (ReferenceEquals(hits[h].attachedRigidbody, piece.Body)) continue;

                    string owner = hits[h].attachedRigidbody != null ? hits[h].attachedRigidbody.name : hits[h].name;
                    names.Add(owner + (Physics.GetIgnoreCollision(own[i], hits[h]) ? " (ignorado)" : string.Empty));
                }
            }
            return $"y={piece.transform.position.y:F2} " + string.Join(", ", names);
        }

        private static void ExitPlayMode()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
        }

        private sealed class PieceStats
        {
            private readonly float _spawnTime;
            // Primeira caixa = começo do galho mais baixo (o pé do tronco derrubado); guarda o ponto, não a caixa
            // (as caixas são recriadas quando galhos quebram na queda)
            private readonly Transform _piece;
            private readonly Vector3 _localFoot;
            private readonly float _footStartHeight;

            public PieceStats(string name, float spawnTime, float mass, Transform piece, BoxCollider[] boxes)
            {
                Name = name;
                _spawnTime = spawnTime;
                Mass = mass;
                Colliders = boxes.Length;
                _piece = piece;
                _localFoot = piece.InverseTransformPoint(boxes[0].bounds.center);
                _footStartHeight = boxes[0].bounds.center.y;
            }

            public string Name { get; }
            public float Mass { get; }
            public int Colliders { get; }
            public bool HasSettleSamples { get; private set; }
            public float SpawnTime => _spawnTime;
            public float MaxSpin { get; private set; }
            public float MaxSpinTime { get; private set; }
            public float MaxUpSpeed { get; private set; }
            public float FinalSpeed { get; private set; }
            public float FinalSpin { get; private set; }
            public bool IsSleeping { get; private set; }
            public float MaxRise { get; private set; }
            public float MaxRiseTime { get; private set; }
            public float HingeReleaseTime { get; private set; }
            public float RiseSpin { get; private set; }
            public float RiseSpinAngle { get; private set; }
            public float RiseTrunkTilt { get; private set; }
            private Vector3 _angularVelocity;

            public void RecordAxis(Vector3 angularVelocity)
            {
                _angularVelocity = angularVelocity;
            }
            private bool _hadHinge;

            public void RecordHinge(float now, bool hasHinge)
            {
                if (_hadHinge && !hasHinge) HingeReleaseTime = now;
                _hadHinge = hasHinge;
            }

            public void Record(float now, float spin, float upSpeed, float speed, bool isSleeping, float settleTime)
            {
                FinalSpeed = speed;
                FinalSpin = spin;
                IsSleeping = isSleeping;
                // Desde o nascimento: o chute do pé acontece durante a queda
                float rise = _piece.TransformPoint(_localFoot).y - _footStartHeight;
                if (rise > MaxRise)
                {
                    MaxRise = rise;
                    MaxRiseTime = now;
                    Vector3 trunk = _piece.up;
                    RiseSpin = _angularVelocity.magnitude;
                    float angle = Vector3.Angle(_angularVelocity, trunk);
                    RiseSpinAngle = Mathf.Min(angle, 180f - angle);
                    RiseTrunkTilt = 90f - Vector3.Angle(trunk, Vector3.up);
                }
                if (now - _spawnTime < settleTime) return;

                HasSettleSamples = true;
                if (spin > MaxSpin)
                {
                    MaxSpin = spin;
                    MaxSpinTime = now;
                }
                MaxUpSpeed = Mathf.Max(MaxUpSpeed, upSpeed);
            }
        }
    }
}
