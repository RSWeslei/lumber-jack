using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Colisores de um pedaço de árvore: caixas no trecho do eixo de cada galho que o pedaço ainda tem
    /// (Rigidbody não aceita MeshCollider côncavo). Caixa e não cápsula: tronco deitado não rola sozinho.
    /// </summary>
    public static class TreePieceColliders
    {
        private const string ColliderName = "Collider";
        private const int NodesPerBox = 2;
        // Caixa menor que isso (m) o PhysX ignora e a lasca atravessa o chão
        private const float MinBoxSize = 0.03f;

        /// <summary>Cria as caixas e devolve quantas criou.</summary>
        public static int Build(TreePartMesh part, TreeSkeleton skeleton, Transform parent, float minRadius, int maxColliders,
            PhysicsMaterial material)
        {
            int count = 0;
            for (int b = 0; b < skeleton.BranchCount; b++)
            {
                if (!TryGetCoveredRange(part, skeleton, b, out float min, out float max)) continue;

                TreeBranch branch = skeleton.GetBranch(b);
                for (int n = branch.FirstNode; n < branch.LastNode; n += NodesPerBox)
                {
                    TreeNode start = skeleton.GetNode(n);
                    TreeNode end = skeleton.GetNode(Mathf.Min(n + NodesPerBox, branch.LastNode));
                    float from = Mathf.Max(start.Distance, min);
                    float to = Mathf.Min(end.Distance, max);
                    float radius = Mathf.Max(start.Radius, end.Radius);
                    if (to <= from || radius < minRadius) continue;

                    skeleton.Sample(b, from / branch.Length, out Vector3 fromPoint, out _, out _);
                    skeleton.Sample(b, to / branch.Length, out Vector3 toPoint, out _, out _);
                    AddBox(parent, fromPoint, toPoint, radius, material);
                    if (++count >= maxColliders) return count;
                }
            }
            return count;
        }

        /// <summary>Trecho do eixo (distância da base, m) coberto pelos vértices desse galho no pedaço.</summary>
        public static bool TryGetCoveredRange(TreePartMesh part, TreeSkeleton skeleton, int branch, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            int[] triangles = part.TrianglesOf(branch);
            foreach (int index in triangles)
            {
                skeleton.ClosestPoint(branch, part.Vertices[index], out _, out _, out _, out float distance);
                min = Mathf.Min(min, distance);
                max = Mathf.Max(max, distance);
            }
            return triangles.Length > 0;
        }

        private static void AddBox(Transform parent, Vector3 from, Vector3 to, float radius, PhysicsMaterial material)
        {
            var colliderObject = new GameObject(ColliderName);
            colliderObject.layer = parent.gameObject.layer;
            Transform colliderTransform = colliderObject.transform;
            colliderTransform.SetParent(parent, false);
            Vector3 axis = to - from;
            float length = Mathf.Max(axis.magnitude, MinBoxSize);
            colliderTransform.localPosition = (from + to) * 0.5f;
            // Giro em volta do eixo sempre o mesmo (referência fixa do esqueleto): as metades de um corte
            // herdam as mesmas caixas e não nascem com a quina enfiada no chão
            // Trecho de comprimento quase zero: qualquer direção serve, a caixa vira um cubinho
            Vector3 direction = axis.sqrMagnitude > MinBoxSize * MinBoxSize * 0.01f ? axis.normalized : Vector3.up;
            colliderTransform.localRotation = Quaternion.LookRotation(TreeSkeletonGenerator.Perpendicular(direction), direction);

            // Comprimento exato do trecho: a caixa não passa do corte nem nasce dentro do toco ou da outra metade
            BoxCollider box = colliderObject.AddComponent<BoxCollider>();
            float width = Mathf.Max(radius * 2f, MinBoxSize);
            box.size = new Vector3(width, length, width);
            box.sharedMaterial = material;
        }
    }
}
