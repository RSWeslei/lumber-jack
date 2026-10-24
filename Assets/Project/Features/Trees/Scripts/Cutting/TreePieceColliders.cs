using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Colisores de um pedaço de árvore: cápsulas no trecho do eixo de cada galho que o pedaço ainda tem
    /// (Rigidbody não aceita MeshCollider côncavo). Uma cápsula a cada dois segmentos.
    /// </summary>
    public static class TreePieceColliders
    {
        private const string ColliderName = "Collider";
        private const int NodesPerCapsule = 2;
        private const int CapsuleAxisY = 1;

        public static void Build(TreePartMesh part, TreeSkeleton skeleton, Transform parent, float minRadius, int maxColliders)
        {
            int count = 0;
            for (int b = 0; b < skeleton.BranchCount; b++)
            {
                if (!TryGetCoveredRange(part, skeleton, b, out float min, out float max)) continue;

                TreeBranch branch = skeleton.GetBranch(b);
                for (int n = branch.FirstNode; n < branch.LastNode; n += NodesPerCapsule)
                {
                    TreeNode start = skeleton.GetNode(n);
                    TreeNode end = skeleton.GetNode(Mathf.Min(n + NodesPerCapsule, branch.LastNode));
                    float from = Mathf.Max(start.Distance, min);
                    float to = Mathf.Min(end.Distance, max);
                    float radius = Mathf.Max(start.Radius, end.Radius);
                    if (to <= from || radius < minRadius) continue;

                    skeleton.Sample(b, from / branch.Length, out Vector3 fromPoint, out _, out _);
                    skeleton.Sample(b, to / branch.Length, out Vector3 toPoint, out _, out _);
                    AddCapsule(parent, fromPoint, toPoint, radius);
                    if (++count >= maxColliders) return;
                }
            }
        }

        // Trecho do eixo (distância da base, m) coberto pelos vértices desse galho no pedaço
        private static bool TryGetCoveredRange(TreePartMesh part, TreeSkeleton skeleton, int branch, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            int[] triangles = part.TrianglesOf(branch);
            for (int i = 0; i < triangles.Length; i++)
            {
                skeleton.ClosestPoint(branch, part.Vertices[triangles[i]], out _, out _, out _, out float distance);
                min = Mathf.Min(min, distance);
                max = Mathf.Max(max, distance);
            }
            return triangles.Length > 0;
        }

        private static void AddCapsule(Transform parent, Vector3 from, Vector3 to, float radius)
        {
            var colliderObject = new GameObject(ColliderName);
            colliderObject.layer = parent.gameObject.layer;
            Transform colliderTransform = colliderObject.transform;
            colliderTransform.SetParent(parent, false);
            Vector3 axis = to - from;
            colliderTransform.localPosition = (from + to) * 0.5f;
            colliderTransform.localRotation = Quaternion.FromToRotation(Vector3.up, axis);

            var capsule = colliderObject.AddComponent<CapsuleCollider>();
            capsule.direction = CapsuleAxisY;
            capsule.radius = radius;
            capsule.height = Mathf.Max(axis.magnitude, radius * 2f);
        }
    }
}
