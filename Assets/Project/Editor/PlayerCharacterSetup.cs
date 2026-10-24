using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LumberJack.Editor
{
    /// <summary>
    /// Monta o personagem do jogador: Menu "Lumber Jack > Configurar personagem do jogador".
    /// Corpo (Kenney) no lugar da cápsula, só projetando sombra (em primeira pessoa você vê a sombra,
    /// os outros jogadores verão o corpo). Pode rodar de novo.
    /// </summary>
    public static class PlayerCharacterSetup
    {
        private const string PlayerPrefabPath = "Assets/Project/Features/Player/Prefabs/PlayerCapsule.prefab";
        private const string ArtFolder = "Assets/Project/Features/Player/Art";
        private const string BodyModelPath = "Assets/ThirdParty/Kenney/AnimatedCharacters1/Model/characterMedium.fbx";
        private const string BodySkinPath = "Assets/ThirdParty/Kenney/AnimatedCharacters1/Skins/survivorMaleB.png";
        private const string BodyMaterialPath = ArtFolder + "/PlayerBody.mat";
        // Pose parada do corpo: primeiro frame do idle (o modelo vem em pose T)
        private const string BodyPoseClipPath = "Assets/ThirdParty/Kenney/AnimatedCharacters1/Animations/idle.fbx";

        private const string PlayerLayerName = "Player";
        private const string CapsuleVisualName = "Capsule";
        private const string BodyName = "Body";
        private const float MinModelHeight = 0.0001f;

        [MenuItem("Lumber Jack/Configurar personagem do jogador")]
        public static void Setup()
        {
            int playerLayer = LayerMask.NameToLayer(PlayerLayerName);
            if (playerLayer < 0)
            {
                Debug.LogError($"Camada '{PlayerLayerName}' não existe (Project Settings > Tags and Layers).");
                return;
            }

            EnsureFolder(ArtFolder);
            Material bodyMaterial = CreateLitMaterial(BodyMaterialPath, AssetDatabase.LoadAssetAtPath<Texture2D>(BodySkinPath), Color.white);
            SetupBody(bodyMaterial, playerLayer);
            AssetDatabase.SaveAssets();
            Debug.Log("Personagem configurado: corpo em PlayerCapsule.");
        }

        private static void SetupBody(Material material, int playerLayer)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                DestroyChild(root.transform, CapsuleVisualName);
                DestroyChild(root.transform, BodyName);

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(BodyModelPath);
                GameObject body = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                body.name = BodyName;
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;
                body.transform.localScale = Vector3.one;

                AnimationClip idle = FindClip(BodyPoseClipPath, string.Empty);
                if (idle != null) idle.SampleAnimation(body, 0f);
                if (body.TryGetComponent(out Animator bodyAnimator)) Object.DestroyImmediate(bodyAnimator);

                // Escala o modelo para a altura do CharacterController e coloca os pés na base dele
                CharacterController controller = root.GetComponent<CharacterController>();
                Bounds bounds = GetBounds(body);
                if (bounds.size.y < MinModelHeight)
                {
                    Debug.LogError($"Não consegui medir a altura do modelo {BodyModelPath}.");
                    return;
                }
                float scale = controller.height / bounds.size.y;
                body.transform.localScale = Vector3.one * scale;
                bounds = GetBounds(body);
                float feetY = root.transform.position.y + controller.center.y - controller.height / 2f;
                body.transform.position += Vector3.up * (feetY - bounds.min.y);

                foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
                SetLayerRecursively(root, playerLayer);

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                Debug.Log($"Corpo: escala {scale:F4}, altura {controller.height} m.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Material CreateLitMaterial(string path, Texture2D texture, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static AnimationClip FindClip(string modelPath, string namePrefix)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                // "__preview__" são clips internos do importador
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__") && clip.name.StartsWith(namePrefix)) return clip;
            }
            return null;
        }

        private static Bounds GetBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void DestroyChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
