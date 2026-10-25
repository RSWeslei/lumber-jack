using System.IO;
using LumberJack.Trees;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LumberJack.Editor
{
    /// <summary>
    /// Cena de teste das árvores: Menu "Lumber Jack > Criar cena de teste de árvores".
    /// Cria as espécies de exemplo (se faltarem) e uma fileira de cada espécie com seeds diferentes.
    /// Pode rodar de novo a qualquer momento para recriar a cena.
    /// </summary>
    public static class TreeSandboxBuilder
    {
        private const string SceneFolder = "Assets/Project/Scenes/Sandbox";
        private const string ScenePath = SceneFolder + "/TreeSandbox.unity";
        private const string SandboxArtFolder = "Assets/Project/Features/Trees/Art/Sandbox";
        private const string GroundMaterialPath = SandboxArtFolder + "/SandboxGround.mat";
        private const string SkyMaterialPath = "Assets/ThirdParty/Low Poly Sky/Polyverse Skies/Core/Presets/Sky 03.mat";
        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string WoodLayerName = "Wood";

        private const int TreesPerSpecies = 5;
        private const float TreeSpacing = 7f;
        private const float RowSpacing = 12f;
        private const float GroundScale = 10f;
        private const float GroundSmoothness = 0f;

        private static readonly Color GroundColor = new(0.42f, 0.55f, 0.28f);
        private static readonly Vector3 CameraPosition = new(14f, 7f, -16f);
        private static readonly Vector3 CameraLookAt = new(14f, 4f, 12f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        [MenuItem("Lumber Jack/Criar cena de teste de árvores")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            TreeSpeciesPresets.EnsureDefaults();
            EnsureFolder(SandboxArtFolder);
            EnsureFolder(SceneFolder);
            CreateGroundMaterial();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Assets carregados só depois da cena nova: o NewScene descarrega os que não estão em uso
            string[] speciesPaths = { TreeSpeciesPresets.PinePath, TreeSpeciesPresets.OakPath, TreeSpeciesPresets.BirchPath };
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
            if (sky != null) RenderSettings.skybox = sky;

            // Grupos da raiz no padrão do projeto (CLAUDE.md > Scene hierarchy)
            Transform lighting = CreateGroup("Lighting");
            Transform player = CreateGroup("Player");
            Transform environment = CreateGroup("Environment");

            Light sun = Object.FindAnyObjectByType<Light>();
            sun.name = "Sun";
            sun.transform.SetParent(lighting, false);

            Camera camera = Object.FindAnyObjectByType<Camera>();
            camera.transform.SetParent(player, false);
            camera.transform.SetPositionAndRotation(CameraPosition, Quaternion.LookRotation(CameraLookAt - CameraPosition));

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(environment, false);
            ground.transform.localScale = new Vector3(GroundScale, 1f, GroundScale);
            ground.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);

            Transform trees = new GameObject("Trees").transform;
            trees.SetParent(environment, false);
            for (int row = 0; row < speciesPaths.Length; row++)
            {
                TreeSpeciesDefinition species = AssetDatabase.LoadAssetAtPath<TreeSpeciesDefinition>(speciesPaths[row]);
                for (int i = 0; i < TreesPerSpecies; i++)
                {
                    CreateTree(species, i + 1, new Vector3(i * TreeSpacing, 0f, row * RowSpacing), trees);
                }
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            WorldLightingSetup.Apply();
            EditorSceneManager.SaveScene(scene);
        }

        private static void CreateTree(TreeSpeciesDefinition species, int seed, Vector3 position, Transform parent)
        {
            // Camada Wood: o machado mira nela
            var treeObject = new GameObject(species.name + " " + seed) { layer = LayerMask.NameToLayer(WoodLayerName) };
            treeObject.transform.SetParent(parent, false);
            treeObject.transform.localPosition = position;
            treeObject.AddComponent<ProceduralTree>().Configure(species, seed);
            treeObject.AddComponent<ChoppableTree>();
        }

        private static Transform CreateGroup(string name)
        {
            return new GameObject(name).transform;
        }

        private static void CreateGroundMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath) != null) return;

            var material = new Material(Shader.Find(LitShader));
            material.SetColor(BaseColorId, GroundColor);
            material.SetFloat(SmoothnessId, GroundSmoothness);
            AssetDatabase.CreateAsset(material, GroundMaterialPath);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
