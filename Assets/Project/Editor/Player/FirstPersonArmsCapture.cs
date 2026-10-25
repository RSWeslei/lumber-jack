using System.IO;
using LumberJack.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LumberJack.Editor
{
    /// <summary>
    /// Fotos dos braços em primeira pessoa para conferir pose e golpe sem jogar: Menu "Lumber Jack > Testes >
    /// Fotos dos braços" (ou o arquivo de comando em Temp). Entra no Play da cena aberta, fotografa parado, na
    /// preparação e na batida de um golpe e sai do Play. As fotos ficam em Logs/.
    /// </summary>
    [InitializeOnLoad]
    public static class FirstPersonArmsCapture
    {
        private const string CommandPath = "Temp/LumberJack/run-arms-capture";
        private const string PendingKey = "LumberJack.ArmsCapture.Pending";
        private const string CapturingKey = "LumberJack.ArmsCapture.Capturing";
        private const double PollSeconds = 0.5;
        private const string IdlePath = "Logs/arms-idle.png";
        private const string WindUpPath = "Logs/arms-windup.png";
        private const string StrikePath = "Logs/arms-strike.png";
        private const string DonePath = "Logs/arms-capture.txt";
        private const string DiagnosticsPath = "Logs/arms-diagnostics.txt";
        private const string HandViewPath = "Logs/arms-hand-view.png";
        private const string HandPalmPath = "Logs/arms-hand-palm.png";
        private const string HandSidePath = "Logs/arms-hand-side.png";
        private const string ToolSocketName = "ToolSocket";
        // Altura da pegada no cabo (a mesma do FirstPersonArmsSetup)
        private const float GripHeight = 0.1f;
        private const float CloseUpDistance = 0.3f;
        private const float CloseUpFov = 40f;
        private const float CloseUpNearClip = 0.01f;
        private const int ShotWidth = 1280;
        private const int ShotHeight = 720;
        private const int DepthBits = 24;

        // Momentos (s desde o Play) das fotos: o golpe começa em SwingAt e segue os tempos do clipe
        private const double IdleAt = 1.5;
        private const double SwingAt = 2.5;
        private const double WindUpAfterSwing = 0.08;
        private const double StrikeAfterSwing = 0.17;
        private const double ExitAfterSwing = 1.5;

        private static double _nextPoll;
        private static int _step;

        static FirstPersonArmsCapture()
        {
            EditorApplication.update += HandleUpdate;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem("Lumber Jack/Testes/Fotos dos braços")]
        public static void RequestRun()
        {
            SessionState.SetBool(PendingKey, true);
            AssetDatabase.Refresh();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(CapturingKey, false)) return;

            // Sem isso o jogo não avança com o editor em segundo plano (Animator e IK parados nas fotos)
            Application.runInBackground = true;
            _step = 0;
        }

        private static void HandleUpdate()
        {
            if (EditorApplication.isPlaying && SessionState.GetBool(CapturingKey, false))
            {
                // Tempo do jogo: zera no Play (estáticos do editor zeram no recarregamento do código)
                Capture(Time.time);
                return;
            }

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
            if (SceneManager.GetActiveScene().isDirty)
            {
                File.WriteAllText(DonePath, "ABORTADO: a cena aberta tem alterações não salvas.\n");
                return;
            }
            SessionState.SetBool(CapturingKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static void Capture(double time)
        {
            switch (_step)
            {
                case 0 when time >= IdleAt:
                    SaveShot(IdlePath);
                    SaveHandCloseUps();
                    File.WriteAllText(DiagnosticsPath, Describe());
                    _step++;
                    break;
                case 1 when time >= SwingAt:
                    Object.FindAnyObjectByType<FirstPersonArms>().Swing(1f);
                    _step++;
                    break;
                case 2 when time >= SwingAt + WindUpAfterSwing:
                    SaveShot(WindUpPath);
                    _step++;
                    break;
                case 3 when time >= SwingAt + StrikeAfterSwing:
                    SaveShot(StrikePath);
                    _step++;
                    break;
                case 4 when time >= SwingAt + ExitAfterSwing:
                    SessionState.EraseBool(CapturingKey);
                    // No editor isso é a configuração do projeto: o jogo não roda em segundo plano
                    PlayerSettings.runInBackground = false;
                    File.WriteAllText(DonePath, "OK\n");
                    EditorApplication.ExitPlaymode();
                    break;
            }
        }

        // Onde estão mãos, pegadas e malhas em relação à câmera (confere se o IK levou as mãos ao cabo)
        private static string Describe()
        {
            Transform camera = Object.FindAnyObjectByType<PlayerAim>().transform;
            Transform arms = Object.FindAnyObjectByType<FirstPersonArms>().transform;
            var text = new System.Text.StringBuilder();
            foreach (string name in new[] { "upper_arm.R", "forearm.R", "hand.R", "hand.L", "ToolSocket" })
            {
                Transform found = FindDeep(arms, name);
                text.AppendLine(found == null ? $"{name}: não achado" : $"{name}: {camera.InverseTransformPoint(found.position)} ativo={found.gameObject.activeInHierarchy} camada={found.gameObject.layer}");
            }
            foreach (Renderer renderer in arms.GetComponentsInChildren<Renderer>(true))
            {
                text.AppendLine($"render {renderer.name}: centro {camera.InverseTransformPoint(renderer.bounds.center)} tamanho {renderer.bounds.size} " +
                                $"ligado={renderer.enabled} visível={renderer.isVisible} camada={renderer.gameObject.layer} materiais={renderer.sharedMaterials.Length}");
                if (!(renderer is SkinnedMeshRenderer skinned)) continue;

                // Malha deformada de verdade (pose atual), em relação à câmera
                var baked = new Mesh();
                skinned.BakeMesh(baked, true);
                Vector3 min = Vector3.positiveInfinity;
                Vector3 max = Vector3.negativeInfinity;
                foreach (Vector3 vertex in baked.vertices)
                {
                    Vector3 local = camera.InverseTransformPoint(skinned.transform.TransformPoint(vertex));
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                }
                Object.DestroyImmediate(baked);
                text.AppendLine($"   pele: de {min} até {max}, ossos={skinned.bones.Length} raiz={(skinned.rootBone != null ? skinned.rootBone.name : "-")} " +
                                $"qualidade={skinned.quality} atualiza fora da tela={skinned.updateWhenOffscreen}");
            }
            return text.ToString();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // Mão no cabo de perto: como o jogador vê, pelo lado da palma e de lado
        private static void SaveHandCloseUps()
        {
            Transform view = Object.FindAnyObjectByType<PlayerAim>().transform;
            Transform arms = Object.FindAnyObjectByType<FirstPersonArms>().transform;
            Vector3 grip = FindDeep(arms, ToolSocketName).TransformPoint(Vector3.up * GripHeight);
            Vector3 lookDirection = (grip - view.position).normalized;

            var cameraObject = new GameObject("HandCloseUpCamera");
            var closeUp = cameraObject.AddComponent<Camera>();
            closeUp.cullingMask = 1 << arms.gameObject.layer;
            closeUp.clearFlags = CameraClearFlags.SolidColor;
            closeUp.backgroundColor = Color.gray;
            closeUp.fieldOfView = CloseUpFov;
            closeUp.nearClipPlane = CloseUpNearClip;
            SaveCloseUp(closeUp, grip, lookDirection, view.up, HandViewPath);
            SaveCloseUp(closeUp, grip, -lookDirection, view.up, HandPalmPath);
            SaveCloseUp(closeUp, grip, -view.right, view.up, HandSidePath);
            Object.DestroyImmediate(cameraObject);
        }

        private static void SaveCloseUp(Camera closeUp, Vector3 target, Vector3 direction, Vector3 up, string path)
        {
            closeUp.transform.SetPositionAndRotation(target - direction * CloseUpDistance, Quaternion.LookRotation(direction, up));
            SaveShot(closeUp, path);
        }

        private static void SaveShot(string path)
        {
            SaveShot(Object.FindAnyObjectByType<PlayerAim>().GetComponent<Camera>(), path);
        }

        // Renderiza a câmera (com as câmeras empilhadas) numa textura: não depende da janela Game
        private static void SaveShot(Camera camera, string path)
        {
            var target = new RenderTexture(ShotWidth, ShotHeight, DepthBits);
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(ShotWidth, ShotHeight, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, ShotWidth, ShotHeight), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);
        }
    }
}
