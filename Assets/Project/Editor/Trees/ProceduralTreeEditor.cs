using LumberJack.Trees;
using UnityEditor;
using UnityEngine;

namespace LumberJack.Editor
{
    /// <summary>
    /// Inspector da árvore: botões de nova seed/regenerar e estatísticas da malha (com aviso de orçamento).
    /// </summary>
    [CustomEditor(typeof(ProceduralTree))]
    [CanEditMultipleObjects]
    public class ProceduralTreeEditor : UnityEditor.Editor
    {
        private const string SeedField = "_seed";
        // Orçamento de triângulos de uma árvore (CLAUDE.md > Art style)
        private const int TriangleBudget = 3000;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Nova seed")) RandomizeSeeds();
                if (GUILayout.Button("Regenerar")) Regenerate();
            }

            if (targets.Length == 1) DrawStats((ProceduralTree)target);
        }

        private void RandomizeSeeds()
        {
            foreach (Object treeObject in targets)
            {
                // Um SerializedObject por árvore: cada uma ganha sua seed (com Undo e OnValidate)
                var serializedTree = new SerializedObject(treeObject);
                serializedTree.FindProperty(SeedField).intValue = Random.Range(1, int.MaxValue);
                serializedTree.ApplyModifiedProperties();
            }
        }

        private void Regenerate()
        {
            foreach (Object treeObject in targets)
            {
                ((ProceduralTree)treeObject).Generate();
            }
        }

        private static void DrawStats(ProceduralTree tree)
        {
            if (!tree.IsGenerated)
            {
                EditorGUILayout.HelpBox("Escolha uma espécie para gerar a árvore.", MessageType.Warning);
                return;
            }

            string stats = $"Altura: {tree.Skeleton.Height:F1} m   Galhos: {tree.Skeleton.BranchCount - 1}\n" +
                           $"Vértices: {tree.VertexCount}   Triângulos: {tree.TriangleCount} / {TriangleBudget}";
            MessageType type = tree.TriangleCount > TriangleBudget ? MessageType.Warning : MessageType.Info;
            if (type == MessageType.Warning) stats += "\nAcima do orçamento: reduza lados, anéis ou níveis de galhos.";
            EditorGUILayout.HelpBox(stats, type);
        }
    }
}
