using System.IO;
using System.Text;
using LumberJack.Core;
using LumberJack.Trees;
using LumberJack.Wood;
using UnityEditor;
using UnityEngine;

namespace LumberJack.Editor
{
    /// <summary>
    /// Teste automático do corte das árvores: corta árvores de verdade várias vezes seguidas em diagonal, no tronco
    /// e nos galhos, e confere se a face do corte fecha certo (área da tampa = área do buraco) e se os anéis cabem
    /// na textura (senão ela repete e aparecem vários círculos). Menu "Lumber Jack > Testes > Corte das árvores",
    /// ou criando o arquivo de comando em Temp. Roda no editor, sem Play.
    /// </summary>
    [InitializeOnLoad]
    public static class CutCapTestRunner
    {
        private const string CommandPath = "Temp/LumberJack/run-cut-cap-test";
        private const string ReportPath = "Logs/cut-cap-test.txt";
        private const string PendingKey = "LumberJack.CutCapTest.Pending";
        private const double PollSeconds = 1.0;
        private const int TrunkIndex = 0;
        private const int CapSubmesh = 1;
        private const int SeedsPerSpecies = 4;
        private const int CutsPerTree = 30;
        private const uint RandomSeed = 12345u;
        private const float TrunkCutChance = 0.5f;
        // Mesmo limite de inclinação do machado nos pedaços caídos
        private const float MaxTilt = 60f;
        // Corte entre 20% e 80% do trecho do galho que o pedaço ainda tem
        private const float MinCutAlong = 0.2f;
        private const float CutAlongRange = 0.6f;
        private const float OnPlaneDistance = 0.00001f;
        // Área (m²) que a tampa pode sobrar ou faltar sem aparecer na tela (1 cm²)
        private const float VisibleAreaError = 0.0001f;
        // Anel mais longe que isso do centro da textura (1 = borda) aparece cortado ou repetido
        private const float MaxRingReach = 1.15f;
        // As duas metades somam o volume do pedaço cortado (massa e preço saem dele)
        private const float MaxVolumeError = 0.1f;


        private static double _nextPoll;

        static CutCapTestRunner()
        {
            EditorApplication.update += HandleUpdate;
        }

        [MenuItem("Lumber Jack/Testes/Corte das árvores")]
        public static void RequestRun()
        {
            // Atualiza os scripts antes: o teste roda depois da recompilação (o pedido sobrevive ao reload)
            SessionState.SetBool(PendingKey, true);
            AssetDatabase.Refresh();
        }

        private static void HandleUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + PollSeconds;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            if (File.Exists(CommandPath))
            {
                File.Delete(CommandPath);
                RequestRun();
                return;
            }

            if (!SessionState.GetBool(PendingKey, false)) return;
            SessionState.EraseBool(PendingKey);
            Run();
        }

        private static void Run()
        {
            TreeSpeciesPresets.EnsureDefaults();
            string[] speciesPaths = { TreeSpeciesPresets.PinePath, TreeSpeciesPresets.OakPath, TreeSpeciesPresets.BirchPath };
            var report = new StringBuilder();
            var cutter = new TreeCutter();
            var part = new TreePartMesh();
            var random = new SeededRandom(RandomSeed);
            int cuts = 0;
            int failures = 0;
            float worstVolumeError = 0f;

            foreach (string path in speciesPaths)
            {
                var species = AssetDatabase.LoadAssetAtPath<TreeSpeciesDefinition>(path);
                for (int seed = 1; seed <= SeedsPerSpecies; seed++)
                {
                    var treeObject = new GameObject("Cut Cap Test") { hideFlags = HideFlags.HideAndDontSave };
                    try
                    {
                        var tree = treeObject.AddComponent<ProceduralTree>();
                        tree.Configure(species, seed);
                        tree.Generate();
                        report.AppendLine($"{species.name} {seed}: volume pela malha {MeshVolume.Of(tree.Mesh):F3} m³, " +
                                          $"pelo esqueleto {SkeletonVolume(tree.Skeleton):F3} m³");
                        CutRepeatedly(tree, cutter, part, ref random, species.name + " " + seed, report, ref cuts, ref failures,
                            ref worstVolumeError);
                    }
                    finally
                    {
                        Object.DestroyImmediate(treeObject);
                    }
                }
            }

            string header = $"Corte das árvores: {cuts - failures}/{cuts} cortes OK\n{CheckLog()}\n" +
                            $"Volume das metades: pior diferença {worstVolumeError * 100f:F1}%\n";
            File.WriteAllText(ReportPath, header + report);
            Debug.Log("[CutCapTest] " + header);
        }

        // A tora gerada (ProceduralLog) precisa ter o volume de um cilindro e as duas pontas com anéis
        private static string CheckLog()
        {
            const float Length = 3f;
            const float Radius = 0.3f;
            const float MinVolumeRatio = 0.85f;
            const float MaxVolumeRatio = 1.1f;
            const float EndTolerance = 0.01f;

            var species = AssetDatabase.LoadAssetAtPath<TreeSpeciesDefinition>(TreeSpeciesPresets.OakPath);
            var skeleton = new TreeSkeleton();
            Mesh log = new TreeLogBuilder().Build(species, Length, Radius, 1, skeleton);
            if (log == null) return "RUIM tora: não foi gerada";

            float ratio = MeshVolume.Of(log) / (Mathf.PI * Radius * Radius * Length);
            bool hasBottom = false;
            bool hasTop = false;
            if (log.subMeshCount > CapSubmesh)
            {
                Vector3[] vertices = log.vertices;
                foreach (int index in log.GetTriangles(CapSubmesh))
                {
                    hasBottom |= Mathf.Abs(vertices[index].y) < EndTolerance;
                    hasTop |= Mathf.Abs(vertices[index].y - Length) < EndTolerance;
                }
            }
            Object.DestroyImmediate(log);

            bool hasBothEnds = hasBottom && hasTop;
            bool isOk = ratio >= MinVolumeRatio && ratio <= MaxVolumeRatio && hasBothEnds;
            return $"{(isOk ? "OK" : "RUIM")} tora: volume {ratio:P0} do cilindro, pontas com anéis: {(hasBothEnds ? "sim" : "não")}";
        }

        // Corta várias vezes seguidas, sempre continuando na metade maior (os cortes antigos ficam no caminho)
        private static void CutRepeatedly(ProceduralTree tree, TreeCutter cutter, TreePartMesh part, ref SeededRandom random,
            string treeName, StringBuilder report, ref int cuts, ref int failures, ref float worstVolumeError)
        {
            TreeSkeleton skeleton = tree.Skeleton;
            Mesh current = tree.Mesh;
            for (int i = 0; i < CutsPerTree; i++)
            {
                part.Load(current, skeleton.BranchCount);
                // Parte dos cortes no tronco, parte num galho qualquer que o pedaço ainda tem (como o jogador mira)
                int branch = random.NextFloat() < TrunkCutChance ? TrunkIndex : random.RangeInclusive(0, skeleton.BranchCount - 1);
                if (!part.HasBranch(branch)) branch = TrunkIndex;
                if (!TreePieceColliders.TryGetCoveredRange(part, skeleton, branch, out float min, out float max)) break;

                float distance = min + (max - min) * (MinCutAlong + CutAlongRange * random.NextFloat());
                skeleton.Sample(branch, distance / skeleton.GetBranch(branch).Length, out Vector3 point, out _, out _);
                float angle = random.NextAngle();
                var viewForward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                float tilt = random.NextSigned() * MaxTilt;
                Plane plane = TreeCutter.GetCutPlane(skeleton, branch, point, viewForward, Vector3.up, tilt,
                    out Vector3 center, out float radius);
                if (!cutter.TryCut(current, part, skeleton, branch, plane, center, radius, out Mesh outer, out Mesh inner))
                {
                    continue;
                }

                cuts++;
                float areaError = Mathf.Max(CapAreaError(outer, plane), CapAreaError(inner, plane));
                float ringReach = Mathf.Max(RingReach(outer, plane), RingReach(inner, plane));
                if (areaError > VisibleAreaError || ringReach > MaxRingReach)
                {
                    failures++;
                    report.AppendLine($"RUIM {treeName} corte {i + 1} galho {branch} inclinação {tilt:F0}°: " +
                                      $"tampa erra {areaError * 10000f:F2} cm², anéis vão até {ringReach:F2} da borda da textura");
                }

                float wholeVolume = MeshVolume.Of(current);
                float outerVolume = MeshVolume.Of(outer);
                float innerVolume = MeshVolume.Of(inner);
                float volumeError = Mathf.Abs(outerVolume + innerVolume - wholeVolume) / wholeVolume;
                worstVolumeError = Mathf.Max(worstVolumeError, volumeError);
                if (volumeError > MaxVolumeError)
                {
                    failures++;
                    report.AppendLine($"RUIM {treeName} corte {i + 1}: metades somam {outerVolume + innerVolume:F3} m³, " +
                                      $"o pedaço tinha {wholeVolume:F3} m³");
                }

                bool keepOuter = outerVolume >= innerVolume;
                if (!ReferenceEquals(current, tree.Mesh)) Object.DestroyImmediate(current);
                Object.DestroyImmediate(keepOuter ? inner : outer);
                current = keepOuter ? outer : inner;
            }
            if (!ReferenceEquals(current, tree.Mesh)) Object.DestroyImmediate(current);
        }

        // Referência grosseira: πr² ao longo de cada galho (conta duas vezes a base dos galhos dentro do tronco)
        private static float SkeletonVolume(TreeSkeleton skeleton)
        {
            float volume = 0f;
            for (int branch = 0; branch < skeleton.BranchCount; branch++)
            {
                TreeBranch info = skeleton.GetBranch(branch);
                for (int node = info.FirstNode; node < info.LastNode; node++)
                {
                    TreeNode from = skeleton.GetNode(node);
                    TreeNode to = skeleton.GetNode(node + 1);
                    float radius = (from.Radius + to.Radius) * 0.5f;
                    volume += Mathf.PI * radius * radius * Vector3.Distance(from.Position, to.Position);
                }
            }
            return volume;
        }

        // Diferença entre a área da tampa e a do buraco que a casca deixou no plano (sobra = triângulo para fora,
        // falta = buraco). Frestas microscópicas não mudam a área
        private static float CapAreaError(Mesh mesh, Plane plane)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector3 holeTwiceArea = Vector3.zero;
            float capArea = 0f;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                if (IsOnPlane(plane, a) && IsOnPlane(plane, b) && IsOnPlane(plane, c))
                {
                    capArea += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                    continue;
                }

                // Borda do buraco: arestas da casca que ficam no plano, na direção do triângulo (soma de Green)
                for (int edge = 0; edge < 3; edge++)
                {
                    Vector3 from = vertices[triangles[i + edge]];
                    Vector3 to = vertices[triangles[i + (edge + 1) % 3]];
                    if (IsOnPlane(plane, from) && IsOnPlane(plane, to)) holeTwiceArea += Vector3.Cross(from, to);
                }
            }
            float holeArea = Mathf.Abs(Vector3.Dot(holeTwiceArea, plane.normal)) * 0.5f;
            return Mathf.Abs(capArea - holeArea);
        }

        // Quanto a face do corte novo vai na textura de anéis: 0 = centro, 1 = borda
        private static float RingReach(Mesh mesh, Plane plane)
        {
            if (mesh.subMeshCount <= CapSubmesh) return 0f;

            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            int[] triangles = mesh.GetTriangles(CapSubmesh);
            float reach = 0f;
            foreach (int index in triangles)
            {
                if (!IsOnPlane(plane, vertices[index])) continue;

                Vector2 offset = (uvs[index] - new Vector2(0.5f, 0.5f)) * 2f;
                reach = Mathf.Max(reach, Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y)));
            }
            return reach;
        }

        private static bool IsOnPlane(Plane plane, Vector3 point)
        {
            return Mathf.Abs(plane.GetDistanceToPoint(point)) <= OnPlaneDistance;
        }
    }
}
