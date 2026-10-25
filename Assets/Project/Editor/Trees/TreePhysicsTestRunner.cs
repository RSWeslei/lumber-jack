using System.IO;
using LumberJack.Trees;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LumberJack.Editor
{
    /// <summary>
    /// Teste automático da física das árvores caídas: Menu "Lumber Jack > Testes > Queda de árvores", ou
    /// criando o arquivo de comando em Temp (para rodar de fora do Unity). Atualiza os scripts, monta a cena
    /// de teste, roda o Play (o <see cref="TreeFellingTestDriver"/> escreve o relatório) e volta para a cena
    /// anterior. Não mexe em nada se houver cena com alterações não salvas.
    /// </summary>
    [InitializeOnLoad]
    public static class TreePhysicsTestRunner
    {
        private const string CommandPath = "Temp/LumberJack/run-tree-physics-test";
        private const string ReportPath = "Logs/tree-physics-test.txt";
        private const string ScenePath = "Assets/Project/Scenes/Sandbox/TreePhysicsTest.unity";
        private const string PendingKey = "LumberJack.TreeTest.Pending";
        private const string ReturnSceneKey = "LumberJack.TreeTest.ReturnScene";
        private const double PollSeconds = 1.0;
        private const string WoodLayerName = "Wood";

        private const int SeedsPerSpecies = 4;
        // Perto o bastante para umas caírem sobre as outras
        private const float TreeSpacing = 7f;
        private const float GroundScale = 20f;
        private static readonly Vector3 CameraPosition = new(10f, 16f, -16f);
        private static readonly Vector3 CameraLookAt = new(10f, 0f, 7f);

        private static double _nextPoll;

        static TreePhysicsTestRunner()
        {
            EditorApplication.update += HandleUpdate;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem("Lumber Jack/Testes/Queda de árvores")]
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
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (!SceneManager.GetSceneAt(i).isDirty) continue;

                File.WriteAllText(ReportPath, "ABORTADO: há cena com alterações não salvas no editor.\n");
                Debug.LogWarning("[TreeTest] Teste não rodou: salve as cenas abertas primeiro.");
                return;
            }

            SessionState.SetString(ReturnSceneKey, SceneManager.GetActiveScene().path);
            BuildScene();
            EditorApplication.EnterPlaymode();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;

            string returnScene = SessionState.GetString(ReturnSceneKey, string.Empty);
            if (returnScene.Length == 0) return;

            SessionState.EraseString(ReturnSceneKey);
            if (returnScene != ScenePath) EditorSceneManager.OpenScene(returnScene);
        }

        private static void BuildScene()
        {
            TreeSpeciesPresets.EnsureDefaults();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Grupos da raiz no padrão do projeto (CLAUDE.md > Scene hierarchy)
            Transform systems = new GameObject("Systems").transform;
            Transform lighting = new GameObject("Lighting").transform;
            Transform player = new GameObject("Player").transform;
            Transform environment = new GameObject("Environment").transform;

            Light sun = Object.FindAnyObjectByType<Light>();
            sun.transform.SetParent(lighting, false);
            Camera camera = Object.FindAnyObjectByType<Camera>();
            camera.transform.SetParent(player, false);
            camera.transform.SetPositionAndRotation(CameraPosition, Quaternion.LookRotation(CameraLookAt - CameraPosition));

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(environment, false);
            ground.transform.localScale = new Vector3(GroundScale, 1f, GroundScale);

            // Espécies carregadas depois da cena nova: o NewScene descarrega assets que não estão em uso
            string[] speciesPaths = { TreeSpeciesPresets.PinePath, TreeSpeciesPresets.OakPath, TreeSpeciesPresets.BirchPath };
            Transform trees = new GameObject("Trees").transform;
            trees.SetParent(environment, false);
            for (int row = 0; row < speciesPaths.Length; row++)
            {
                TreeSpeciesDefinition species = AssetDatabase.LoadAssetAtPath<TreeSpeciesDefinition>(speciesPaths[row]);
                for (int i = 0; i < SeedsPerSpecies; i++)
                {
                    // Camada Wood, como no jogo
                    var tree = new GameObject(species.name + " " + (i + 1)) { layer = LayerMask.NameToLayer(WoodLayerName) };
                    tree.transform.SetParent(trees, false);
                    tree.transform.localPosition = new Vector3(i * TreeSpacing, 0f, row * TreeSpacing);
                    // Mesmas sementes do Main (Pine 11+, Oak 21+, Birch 31+): as mesmas copas que lá
                    tree.AddComponent<ProceduralTree>().Configure(species, (row + 1) * 10 + i + 1);
                    tree.AddComponent<ChoppableTree>();
                }
            }

            new GameObject("Tree Felling Test").AddComponent<TreeFellingTestDriver>().transform.SetParent(systems, false);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
