using System.Collections.Generic;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Colisores de um pedaço de árvore: um casco convexo por trecho de cada galho, feito com os vértices reais
    /// daquele trecho. Segue a casca e as faces dos cortes, até as inclinadas (Rigidbody não aceita MeshCollider
    /// côncavo, por isso um casco por trecho).
    /// </summary>
    public static class TreePieceColliders
    {
        private const string ColliderName = "Collider";
        private const int NodesPerHull = 2;
        // Trecho com menos vértices que isso, ou mais fino que isso (m), é uma lasca plana: o PhysX não monta casco
        private const int MinHullVertices = 8;
        private const float MinHullThickness = 0.005f;

        // Montagem acontece uma por vez na thread principal: buffers compartilhados
        private static readonly List<float> TriangleDistances = new();
        private static readonly List<Vector3> HullVertices = new();
        private static readonly List<int> HullTriangles = new();

        /// <summary>
        /// Cria os cascos e devolve quantos criou. As malhas criadas vão para <paramref name="createdMeshes"/>:
        /// quem chama destrói junto com o pedaço.
        /// </summary>
        public static int Build(TreePartMesh part, TreeSkeleton skeleton, Transform parent, float minRadius, int maxColliders,
            PhysicsMaterial material, List<Mesh> createdMeshes)
        {
            int count = 0;
            for (int b = 0; b < skeleton.BranchCount && count < maxColliders; b++)
            {
                int[] triangles = part.TrianglesOf(b);
                if (triangles.Length == 0) continue;

                MeasureTriangles(part, skeleton, b, triangles);
                TreeBranch branch = skeleton.GetBranch(b);
                for (int first = branch.FirstNode; first < branch.LastNode && count < maxColliders; first += NodesPerHull)
                {
                    int last = Mathf.Min(first + NodesPerHull, branch.LastNode);
                    TreeNode start = skeleton.GetNode(first);
                    TreeNode end = skeleton.GetNode(last);
                    if (Mathf.Max(start.Radius, end.Radius) < minRadius) continue;

                    // O primeiro e o último trecho pegam também o que passa das pontas (colar, ponta do galho)
                    float from = first == branch.FirstNode ? float.MinValue : start.Distance;
                    float to = last == branch.LastNode ? float.MaxValue : end.Distance;
                    CollectTriangles(part.Vertices, triangles, from, to);
                    if (HullVertices.Count < MinHullVertices || IsFlat()) continue;

                    AddHull(parent, material, createdMeshes);
                    count++;
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

        // Distância ao longo do galho do centro de cada triângulo: decide em que trecho ele entra
        private static void MeasureTriangles(TreePartMesh part, TreeSkeleton skeleton, int branch, int[] triangles)
        {
            TriangleDistances.Clear();
            Vector3[] vertices = part.Vertices;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 centroid = (vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3f;
                skeleton.ClosestPoint(branch, centroid, out _, out _, out _, out float distance);
                TriangleDistances.Add(distance);
            }
        }

        private static void CollectTriangles(Vector3[] vertices, int[] triangles, float from, float to)
        {
            HullVertices.Clear();
            HullTriangles.Clear();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                float distance = TriangleDistances[i / 3];
                if (distance < from || distance >= to) continue;

                for (int corner = 0; corner < 3; corner++)
                {
                    HullTriangles.Add(HullVertices.Count);
                    HullVertices.Add(vertices[triangles[i + corner]]);
                }
            }
        }

        // Pontos quase num plano só (ex.: só a face de um corte): mede a espessura em volta do plano por 3 pontos
        private static bool IsFlat()
        {
            Vector3 origin = HullVertices[0];
            Vector3 far = origin;
            foreach (Vector3 point in HullVertices)
            {
                if ((point - origin).sqrMagnitude > (far - origin).sqrMagnitude) far = point;
            }

            Vector3 line = (far - origin).normalized;
            Vector3 side = origin;
            float sideDistance = 0f;
            foreach (Vector3 point in HullVertices)
            {
                float distance = Vector3.Cross(point - origin, line).sqrMagnitude;
                if (distance <= sideDistance) continue;

                sideDistance = distance;
                side = point;
            }

            Vector3 normal = Vector3.Cross(far - origin, side - origin);
            if (normal.sqrMagnitude <= 0f) return true;

            normal.Normalize();
            foreach (Vector3 point in HullVertices)
            {
                if (Mathf.Abs(Vector3.Dot(point - origin, normal)) > MinHullThickness) return false;
            }
            return true;
        }

        private static void AddHull(Transform parent, PhysicsMaterial material, List<Mesh> createdMeshes)
        {
            // Casco em volta da própria origem: longe dela (metros, no espaço da árvore) o PhysX erra o contato
            // de convexos pequenos e arremessa o pedaço
            Vector3 center = Vector3.zero;
            foreach (Vector3 vertex in HullVertices) center += vertex;
            center /= HullVertices.Count;
            for (int i = 0; i < HullVertices.Count; i++) HullVertices[i] -= center;

            var mesh = new Mesh { name = ColliderName };
            mesh.SetVertices(HullVertices);
            mesh.SetTriangles(HullTriangles, 0);

            var colliderObject = new GameObject(ColliderName) { layer = parent.gameObject.layer };
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.localPosition = center;
            MeshCollider meshCollider = colliderObject.AddComponent<MeshCollider>();
            meshCollider.convex = true;
            // Cozinha antes de atribuir: o MeshCollider usa o casco pronto em vez de montar na hora
            Physics.BakeMesh(mesh.GetEntityId(), true, meshCollider.cookingOptions);
            meshCollider.sharedMesh = mesh;
            meshCollider.sharedMaterial = material;
            createdMeshes.Add(mesh);
        }
    }
}
