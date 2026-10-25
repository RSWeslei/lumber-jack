using System.Collections.Generic;
using UnityEngine;

namespace LumberJack.Wood
{
    /// <summary>
    /// Volume de malha (massa e preço da madeira). Furos na malha (a base dos galhos fica aberta dentro do tronco) são
    /// fechados na conta com uma tampa no centro de cada furo: sem isso o volume muda conforme o ponto de onde se mede.
    /// </summary>
    public static class MeshVolume
    {
        // Malhas chapadas não compartilham vértices: as arestas se casam pela posição arredondada a 0,1 mm
        private const float WeldScale = 10000f;

        // Contas acontecem uma por vez na thread principal: buffers compartilhados
        private static readonly Dictionary<(Vector3Int, Vector3Int), int> OpenEdges = new();
        private static readonly Dictionary<Vector3Int, Vector3Int> Parents = new();
        private static readonly Dictionary<Vector3Int, Vector3> HoleSums = new();
        private static readonly Dictionary<Vector3Int, int> HoleCounts = new();

        public static float Of(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            // Tetraedros a partir do centro da malha (os pedaços ficam longe da origem da árvore)
            Vector3 reference = mesh.bounds.center;
            OpenEdges.Clear();
            float volume = 0f;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                volume += Tetrahedron(reference, a, b, c);
                AddEdge(a, b);
                AddEdge(b, c);
                AddEdge(c, a);
            }
            volume += CapHoles(reference);
            return Mathf.Abs(volume);
        }

        // Aresta a→b de um triângulo: se a vizinha b→a já apareceu, as duas se fecham; senão fica aberta (borda de furo)
        private static void AddEdge(Vector3 from, Vector3 to)
        {
            Vector3Int a = Key(from);
            Vector3Int b = Key(to);
            if (a == b) return;

            if (OpenEdges.TryGetValue((b, a), out int reverse))
            {
                if (reverse <= 1) OpenEdges.Remove((b, a));
                else OpenEdges[(b, a)] = reverse - 1;
                return;
            }
            OpenEdges.TryGetValue((a, b), out int count);
            OpenEdges[(a, b)] = count + 1;
        }

        // Cada furo (bordas abertas ligadas entre si) ganha um leque de triângulos até o centro dele
        private static float CapHoles(Vector3 reference)
        {
            if (OpenEdges.Count == 0) return 0f;

            Parents.Clear();
            HoleSums.Clear();
            HoleCounts.Clear();
            foreach ((Vector3Int a, Vector3Int b) in OpenEdges.Keys) Union(a, b);
            foreach (Vector3Int point in Parents.Keys)
            {
                Vector3Int root = Find(point);
                HoleSums.TryGetValue(root, out Vector3 sum);
                HoleSums[root] = sum + Position(point);
                HoleCounts.TryGetValue(root, out int count);
                HoleCounts[root] = count + 1;
            }

            float volume = 0f;
            foreach (KeyValuePair<(Vector3Int, Vector3Int), int> edge in OpenEdges)
            {
                (Vector3Int a, Vector3Int b) = edge.Key;
                Vector3Int root = Find(a);
                Vector3 center = HoleSums[root] / HoleCounts[root];
                // A tampa percorre a borda no sentido contrário ao do triângulo que a deixou aberta
                volume += Tetrahedron(reference, center, Position(b), Position(a)) * edge.Value;
            }
            return volume;
        }

        private static void Union(Vector3Int a, Vector3Int b)
        {
            if (!Parents.ContainsKey(a)) Parents[a] = a;
            if (!Parents.ContainsKey(b)) Parents[b] = b;
            Vector3Int rootA = Find(a);
            Vector3Int rootB = Find(b);
            if (rootA != rootB) Parents[rootA] = rootB;
        }

        // Sem compressão de caminho: Find roda enquanto Parents é percorrido
        private static Vector3Int Find(Vector3Int point)
        {
            while (true)
            {
                Vector3Int parent = Parents[point];
                if (parent == point) return point;
                point = parent;
            }
        }

        private static float Tetrahedron(Vector3 reference, Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Dot(a - reference, Vector3.Cross(b - reference, c - reference)) / 6f;
        }

        private static Vector3Int Key(Vector3 position)
        {
            return Vector3Int.RoundToInt(position * WeldScale);
        }

        private static Vector3 Position(Vector3Int key)
        {
            return (Vector3)key / WeldScale;
        }
    }
}
