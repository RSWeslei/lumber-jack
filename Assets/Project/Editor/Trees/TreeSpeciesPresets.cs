using System.IO;
using LumberJack.Core;
using LumberJack.Trees;
using UnityEditor;
using UnityEngine;

namespace LumberJack.Editor
{
    /// <summary>
    /// Cria as espécies de exemplo (pinheiro reto com anéis de galhos, carvalho bifurcado, bétula fina)
    /// e seus materiais de casca. Não sobrescreve espécies que já existem: depois de criadas, ajuste no inspector.
    /// </summary>
    public static class TreeSpeciesPresets
    {
        public const string PinePath = DataFolder + "/Pine.asset";
        public const string OakPath = DataFolder + "/Oak.asset";
        public const string BirchPath = DataFolder + "/Birch.asset";

        private const string DataFolder = "Assets/Project/Features/Trees/Data";
        private const string ArtFolder = "Assets/Project/Features/Trees/Art";
        private const string LitShader = "Universal Render Pipeline/Lit";
        private const float BarkSmoothness = 0.05f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        public static void EnsureDefaults()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(ArtFolder);
            if (AssetDatabase.LoadAssetAtPath<TreeSpeciesDefinition>(PinePath) == null) CreatePine();
            if (AssetDatabase.LoadAssetAtPath<TreeSpeciesDefinition>(OakPath) == null) CreateOak();
            if (AssetDatabase.LoadAssetAtPath<TreeSpeciesDefinition>(BirchPath) == null) CreateBirch();
            AssetDatabase.SaveAssets();
        }

        // Pinheiro: tronco alto e reto, anéis de galhos horizontais que encurtam até o topo
        private static void CreatePine()
        {
            var species = new SpeciesWriter(PinePath, "Pinheiro", CreateBark("Bark_Pine", new Color(0.42f, 0.27f, 0.18f)));
            species.Trunk(new FloatRange(10f, 14f), new FloatRange(0.25f, 0.32f), AnimationCurve.Linear(0f, 1f, 1f, 0.12f),
                maxLean: 3f, curvature: 2f, upwardPull: 0.25f, rootFlare: 0.35f, rootFlareHeight: 0.7f);
            species.Mesh(trunkSides: 7, branchSides: 4, segmentLength: 0.8f, irregularity: 0.08f, minBranchRadius: 0.02f);
            species.BranchLevel(new WhorlBranchLayout(new IntRange(4, 5), 0.9f, new FloatRange(0.25f, 0.97f), 0.3f),
                lengthRatio: new FloatRange(0.16f, 0.22f), lengthOverParent: AnimationCurve.Linear(0f, 1f, 1f, 0.15f),
                radiusRatio: new FloatRange(0.22f, 0.3f), radiusOverLength: AnimationCurve.Linear(0f, 1f, 1f, 0.15f),
                angle: new FloatRange(80f, 100f), gravity: -0.12f, curvature: 6f,
                jointSmoothing: 0.25f, collar: 0.35f);
            species.Save();
        }

        // Carvalho: tronco curto e grosso que se divide em 2-3 galhos grossos, com galhos menores neles
        private static void CreateOak()
        {
            var species = new SpeciesWriter(OakPath, "Carvalho", CreateBark("Bark_Oak", new Color(0.36f, 0.27f, 0.19f)));
            species.Trunk(new FloatRange(3f, 4.2f), new FloatRange(0.38f, 0.48f), AnimationCurve.Linear(0f, 1f, 1f, 0.7f),
                maxLean: 6f, curvature: 8f, upwardPull: 0.1f, rootFlare: 0.45f, rootFlareHeight: 0.9f);
            species.Mesh(trunkSides: 8, branchSides: 5, segmentLength: 0.6f, irregularity: 0.1f, minBranchRadius: 0.025f);
            species.BranchLevel(new ForkBranchLayout(new IntRange(2, 3), 0.35f),
                lengthRatio: new FloatRange(1f, 1.4f), lengthOverParent: AnimationCurve.Constant(0f, 1f, 1f),
                radiusRatio: new FloatRange(0.6f, 0.75f), radiusOverLength: AnimationCurve.Linear(0f, 1f, 1f, 0.35f),
                angle: new FloatRange(25f, 45f), gravity: 0.05f, curvature: 12f,
                jointSmoothing: 0.85f, collar: 0.4f);
            species.BranchLevel(new SpiralBranchLayout(new IntRange(3, 5), new FloatRange(0.35f, 0.95f), 0.4f),
                lengthRatio: new FloatRange(0.45f, 0.7f), lengthOverParent: AnimationCurve.Linear(0f, 1f, 1f, 0.6f),
                radiusRatio: new FloatRange(0.45f, 0.6f), radiusOverLength: AnimationCurve.Linear(0f, 1f, 1f, 0.2f),
                angle: new FloatRange(35f, 60f), gravity: -0.05f, curvature: 14f,
                jointSmoothing: 0.4f, collar: 0.3f);
            species.BranchLevel(new SpiralBranchLayout(new IntRange(2, 3), new FloatRange(0.4f, 0.9f), 0.5f),
                lengthRatio: new FloatRange(0.35f, 0.5f), lengthOverParent: AnimationCurve.Constant(0f, 1f, 1f),
                radiusRatio: new FloatRange(0.45f, 0.55f), radiusOverLength: AnimationCurve.Linear(0f, 1f, 1f, 0.2f),
                angle: new FloatRange(30f, 55f), gravity: 0.05f, curvature: 15f,
                jointSmoothing: 0.3f, collar: 0.25f);
            species.Save();
        }

        // Bétula: tronco fino e claro, levemente torto, galhos finos subindo
        private static void CreateBirch()
        {
            var species = new SpeciesWriter(BirchPath, "Bétula", CreateBark("Bark_Birch", new Color(0.86f, 0.84f, 0.78f)));
            species.Trunk(new FloatRange(9f, 12f), new FloatRange(0.14f, 0.19f), AnimationCurve.Linear(0f, 1f, 1f, 0.2f),
                maxLean: 7f, curvature: 5f, upwardPull: 0.08f, rootFlare: 0.2f, rootFlareHeight: 0.4f);
            species.Mesh(trunkSides: 6, branchSides: 4, segmentLength: 0.7f, irregularity: 0.06f, minBranchRadius: 0.015f);
            species.BranchLevel(new SpiralBranchLayout(new IntRange(5, 8), new FloatRange(0.45f, 0.95f), 0.3f),
                lengthRatio: new FloatRange(0.2f, 0.32f), lengthOverParent: AnimationCurve.Linear(0f, 1f, 1f, 0.4f),
                radiusRatio: new FloatRange(0.3f, 0.45f), radiusOverLength: AnimationCurve.Linear(0f, 1f, 1f, 0.2f),
                angle: new FloatRange(30f, 50f), gravity: 0.12f, curvature: 10f,
                jointSmoothing: 0.35f, collar: 0.25f);
            species.BranchLevel(new SpiralBranchLayout(new IntRange(1, 3), new FloatRange(0.4f, 0.9f), 0.4f),
                lengthRatio: new FloatRange(0.35f, 0.5f), lengthOverParent: AnimationCurve.Constant(0f, 1f, 1f),
                radiusRatio: new FloatRange(0.5f, 0.6f), radiusOverLength: AnimationCurve.Linear(0f, 1f, 1f, 0.2f),
                angle: new FloatRange(30f, 50f), gravity: 0.1f, curvature: 12f,
                jointSmoothing: 0.3f, collar: 0.2f);
            species.Save();
        }

        private static Material CreateBark(string name, Color color)
        {
            string path = ArtFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            material = new Material(Shader.Find(LitShader));
            material.SetColor(BaseColorId, color);
            material.SetFloat(SmoothnessId, BarkSmoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>
        /// Preenche os campos privados da espécie pelo SerializedObject (o mesmo caminho do inspector),
        /// sem expor setters no código do jogo.
        /// </summary>
        private sealed class SpeciesWriter
        {
            private readonly TreeSpeciesDefinition _species;
            private readonly SerializedObject _serialized;

            public SpeciesWriter(string path, string displayName, Material bark)
            {
                _species = ScriptableObject.CreateInstance<TreeSpeciesDefinition>();
                AssetDatabase.CreateAsset(_species, path);
                _serialized = new SerializedObject(_species);
                _serialized.FindProperty("_displayName").stringValue = displayName;
                _serialized.FindProperty("_barkMaterial").objectReferenceValue = bark;
                _serialized.FindProperty("_branchLevels").arraySize = 0;
            }

            public void Trunk(FloatRange height, FloatRange baseRadius, AnimationCurve radiusOverLength, float maxLean,
                float curvature, float upwardPull, float rootFlare, float rootFlareHeight)
            {
                SerializedProperty trunk = _serialized.FindProperty("_trunk");
                SetRange(trunk.FindPropertyRelative("_height"), height);
                SetRange(trunk.FindPropertyRelative("_baseRadius"), baseRadius);
                trunk.FindPropertyRelative("_radiusOverLength").animationCurveValue = radiusOverLength;
                trunk.FindPropertyRelative("_maxLean").floatValue = maxLean;
                trunk.FindPropertyRelative("_curvature").floatValue = curvature;
                trunk.FindPropertyRelative("_upwardPull").floatValue = upwardPull;
                trunk.FindPropertyRelative("_rootFlare").floatValue = rootFlare;
                trunk.FindPropertyRelative("_rootFlareHeight").floatValue = rootFlareHeight;
            }

            public void Mesh(int trunkSides, int branchSides, float segmentLength, float irregularity, float minBranchRadius)
            {
                SerializedProperty mesh = _serialized.FindProperty("_mesh");
                mesh.FindPropertyRelative("_trunkSides").intValue = trunkSides;
                mesh.FindPropertyRelative("_branchSides").intValue = branchSides;
                mesh.FindPropertyRelative("_segmentLength").floatValue = segmentLength;
                mesh.FindPropertyRelative("_irregularity").floatValue = irregularity;
                mesh.FindPropertyRelative("_minBranchRadius").floatValue = minBranchRadius;
            }

            // Elementos novos de lista vêm zerados (sem os valores padrão da classe): preenche todos os campos
            public void BranchLevel(IBranchLayout layout, FloatRange lengthRatio, AnimationCurve lengthOverParent,
                FloatRange radiusRatio, AnimationCurve radiusOverLength, FloatRange angle, float gravity, float curvature,
                float jointSmoothing, float collar)
            {
                SerializedProperty levels = _serialized.FindProperty("_branchLevels");
                levels.arraySize++;
                SerializedProperty level = levels.GetArrayElementAtIndex(levels.arraySize - 1);
                level.FindPropertyRelative("_layout").managedReferenceValue = layout;
                SetRange(level.FindPropertyRelative("_lengthRatio"), lengthRatio);
                level.FindPropertyRelative("_lengthOverParent").animationCurveValue = lengthOverParent;
                SetRange(level.FindPropertyRelative("_radiusRatio"), radiusRatio);
                level.FindPropertyRelative("_radiusOverLength").animationCurveValue = radiusOverLength;
                SetRange(level.FindPropertyRelative("_angle"), angle);
                level.FindPropertyRelative("_gravity").floatValue = gravity;
                level.FindPropertyRelative("_curvature").floatValue = curvature;
                level.FindPropertyRelative("_jointSmoothing").floatValue = jointSmoothing;
                level.FindPropertyRelative("_collar").floatValue = collar;
            }

            public void Save()
            {
                _serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(_species);
            }

            private static void SetRange(SerializedProperty range, FloatRange value)
            {
                range.FindPropertyRelative("_min").floatValue = value.Min;
                range.FindPropertyRelative("_max").floatValue = value.Max;
            }
        }
    }
}
