using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Monta uma tora como pedaço de árvore: tronco reto (esqueleto de um galho só) com as duas pontas cortadas
    /// pelo mesmo cortador das árvores. Assim a tora tem os mesmos anéis, golpes e colisores dos pedaços caídos.
    /// Reutilizável: mantém os buffers.
    /// </summary>
    public sealed class TreeLogBuilder
    {
        private const int TrunkIndex = 0;
        private const int TrunkLevel = 0;
        // Sobra (m) em cada ponta antes de cortar: os cortes pegam o tronco cheio, sem a ponta do galho
        private const float EndMargin = 0.2f;
        private const float NodeSpacing = 0.5f;

        private readonly TreeMeshBuilder _meshBuilder = new();
        private readonly TreeCutter _cutter = new();
        private readonly TreePartMesh _part = new();

        /// <summary>
        /// Preenche <paramref name="skeleton"/> e devolve a malha nova da tora, deitada no eixo Y local de 0 até
        /// <paramref name="length"/>. Quem chama é dono da malha. Null se o corte das pontas falhar.
        /// </summary>
        public Mesh Build(TreeSpeciesDefinition species, float length, float radius, int seed, TreeSkeleton skeleton)
        {
            BuildSkeleton(skeleton, length, radius);
            var trunk = new Mesh();
            _meshBuilder.Build(skeleton, species.Mesh, seed, trunk);

            // Corta a ponta de baixo (fica o de cima) e depois a de cima (fica o de baixo)
            Mesh withBase = CutAndKeep(trunk, skeleton, 0f, radius, true);
            if (withBase == null) return null;

            return CutAndKeep(withBase, skeleton, length, radius, false);
        }

        private static void BuildSkeleton(TreeSkeleton skeleton, float length, float radius)
        {
            skeleton.Clear();
            skeleton.BeginBranch(TrunkLevel, TreeBranch.NoParent);
            float total = length + EndMargin * 2f;
            int segments = Mathf.Max(1, Mathf.CeilToInt(total / NodeSpacing));
            for (int i = 0; i <= segments; i++)
            {
                float distance = total * i / segments;
                skeleton.AddNode(new TreeNode(new Vector3(0f, distance - EndMargin, 0f), Vector3.up, radius, distance));
            }
        }

        // Corta na altura y com o plano olhando para cima e fica com um dos lados; destrói a malha de entrada
        private Mesh CutAndKeep(Mesh mesh, TreeSkeleton skeleton, float height, float radius, bool keepAbove)
        {
            _part.Load(mesh, skeleton.BranchCount);
            var center = new Vector3(0f, height, 0f);
            bool isCut = _cutter.TryCut(mesh, _part, skeleton, TrunkIndex, new Plane(Vector3.up, center), center, radius,
                out Mesh above, out Mesh below);
            Discard(mesh);
            if (!isCut) return null;

            Discard(keepAbove ? below : above);
            return keepAbove ? above : below;
        }

        private static void Discard(Mesh mesh)
        {
            if (Application.isPlaying) Object.Destroy(mesh);
            else Object.DestroyImmediate(mesh);
        }
    }
}
