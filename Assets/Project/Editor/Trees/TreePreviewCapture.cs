using System.IO;
using LumberJack.Trees;
using UnityEditor;
using UnityEngine;

namespace LumberJack.Editor
{
    /// <summary>
    /// Foto das espécies de árvore lado a lado (duas seeds de cada), renderizada fora da cena e sem Play, para
    /// conferir forma e folhas. Menu "Lumber Jack > Testes > Foto das árvores", ou criando o arquivo de comando em Temp.
    /// </summary>
    [InitializeOnLoad]
    public static class TreePreviewCapture
    {
        private const string CommandPath = "Temp/LumberJack/run-tree-preview";
        private const string OutputPath = "Logs/tree-preview.png";
        private const string PendingKey = "LumberJack.TreePreview.Pending";
        private const double PollSeconds = 1.0;
        private const int Width = 1600;
        private const int Height = 700;
        private const int SeedsPerSpecies = 2;
        private const float TreeSpacing = 9f;
        private const float FieldOfView = 30f;
        private const float CameraHeight = 6f;
        private const float CameraDistance = 48f;
        private const float LookHeight = 6f;
        private const float FarClip = 300f;
        private const float SunIntensity = 1.3f;
        private const float FillIntensity = 0.4f;

        private static readonly Color Background = new(0.62f, 0.78f, 0.92f);
        private static readonly Color Ambient = new(0.5f, 0.55f, 0.6f);
        private static readonly Quaternion SunRotation = Quaternion.Euler(40f, -35f, 0f);

        private static double _nextPoll;

        static TreePreviewCapture()
        {
            EditorApplication.update += HandleUpdate;
        }

        [MenuItem("Lumber Jack/Testes/Foto das árvores")]
        public static void RequestCapture()
        {
            // Atualiza scripts e assets antes: a foto sai depois da recompilação (o pedido sobrevive ao reload)
            SessionState.SetBool(PendingKey, true);
            AssetDatabase.Refresh();
        }

        private static void Capture()
        {
            string[] speciesPaths = { TreeSpeciesPresets.PinePath, TreeSpeciesPresets.OakPath, TreeSpeciesPresets.BirchPath };
            var preview = new PreviewRenderUtility();
            try
            {
                int index = 0;
                foreach (string path in speciesPaths)
                {
                    var species = AssetDatabase.LoadAssetAtPath<TreeSpeciesDefinition>(path);
                    for (int seed = 1; seed <= SeedsPerSpecies; seed++)
                    {
                        var treeObject = new GameObject(species.name + " " + seed);
                        treeObject.AddComponent<ProceduralTree>().Configure(species, seed);
                        treeObject.transform.position = new Vector3(index * TreeSpacing, 0f, 0f);
                        preview.AddSingleGO(treeObject);
                        index++;
                    }
                }

                float centerX = (index - 1) * TreeSpacing * 0.5f;
                Camera camera = preview.camera;
                camera.fieldOfView = FieldOfView;
                camera.farClipPlane = FarClip;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Background;
                camera.transform.position = new Vector3(centerX, CameraHeight, -CameraDistance);
                camera.transform.LookAt(new Vector3(centerX, LookHeight, 0f));
                preview.lights[0].intensity = SunIntensity;
                preview.lights[0].transform.rotation = SunRotation;
                preview.lights[1].intensity = FillIntensity;
                preview.ambientColor = Ambient;

                preview.BeginStaticPreview(new Rect(0f, 0f, Width, Height));
                preview.Render(true);
                Texture2D image = preview.EndStaticPreview();
                File.WriteAllBytes(OutputPath, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                Debug.Log("[TreePreview] Foto salva em " + OutputPath);
            }
            finally
            {
                preview.Cleanup();
            }
        }

        private static void HandleUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + PollSeconds;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (File.Exists(CommandPath))
            {
                File.Delete(CommandPath);
                RequestCapture();
                return;
            }

            if (!SessionState.GetBool(PendingKey, false)) return;
            SessionState.EraseBool(PendingKey);
            Capture();
        }
    }
}
