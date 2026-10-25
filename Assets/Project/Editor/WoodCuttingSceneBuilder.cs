using System.Collections.Generic;
using System.IO;
using LumberJack.Wood;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LumberJack.Editor
{
    /// <summary>
    /// Monta a cena de teste de corte de madeira: Menu "Lumber Jack > Criar cena de corte de madeira".
    /// Pode rodar de novo a qualquer momento para recriar tudo.
    /// </summary>
    public static class WoodCuttingSceneBuilder
    {
        private const string WoodFolder = "Assets/Project/Features/Wood";
        private const string ArtFolder = WoodFolder + "/Art";
        private const string SandboxArtFolder = ArtFolder + "/Sandbox";
        private const string SceneFolder = "Assets/Project/Scenes/Sandbox";
        private const string ScenePath = SceneFolder + "/WoodCuttingTest.unity";
        private const string PrefabFolder = WoodFolder + "/Prefabs";
        private const string LogPrefabPath = PrefabFolder + "/Log.prefab";
        private const string WoodTypePath = WoodFolder + "/Data/ElmWood.asset";
        private const string GroundMaterialPath = "Assets/ThirdParty/Gridbox Prototype Materials/Materials/URP/Prototype_512x512_Grey2.mat";

        private const float LogRadius = 0.3f;
        private const float LogLength = 3f;
        private const int LogSides = 32;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");

        [MenuItem("Lumber Jack/Criar cena de corte de madeira")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(SandboxArtFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(SceneFolder);

            Texture2D barkTexture = SaveTexture(GenerateBarkTexture(512), ArtFolder, "BarkTexture");
            Texture2D ringsTexture = SaveTexture(GenerateRingsTexture(512), ArtFolder, "RingsTexture");

            Material bark = SaveAsset(CreateLitMaterial(Color.white, barkTexture, 0.05f), ArtFolder, "Bark.mat");
            Material rings = SaveAsset(CreateLitMaterial(Color.white, ringsTexture, 0.15f), ArtFolder, "WoodRings.mat");
            Material preview = SaveAsset(new Material(Shader.Find("Sprites/Default")), ArtFolder, "CutPreview.mat");
            Material axeHandle = SaveAsset(CreateLitMaterial(new Color(0.55f, 0.36f, 0.2f), null, 0.3f), SandboxArtFolder, "AxeHandle.mat");
            Material axeHead = SaveAsset(CreateLitMaterial(new Color(0.6f, 0.62f, 0.65f), null, 0.75f, 0.9f), SandboxArtFolder, "AxeHead.mat");

            Mesh logMesh = SaveAsset(BuildLogMesh(), ArtFolder, "LogMesh.asset");
            BuildLogPrefab(logMesh, bark, rings);

            BuildSceneObjects(preview, axeHandle, axeHead);
        }

        private static void BuildSceneObjects(Material preview, Material axeHandle, Material axeHead)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Carrega o prefab só depois da cena nova: o NewScene descarrega assets que não estão
            // sendo usados e a referência antiga viraria null
            CuttableWood logPrefab = AssetDatabase.LoadAssetAtPath<CuttableWood>(LogPrefabPath);

            GameObject light = GameObject.Find("Directional Light");
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.GetComponent<Light>().shadows = LightShadows.Soft;

            // Chão e dois cavaletes para o tronco ficar em cima
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            Material groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            if (groundMaterial != null) ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;

            CreateBlock("Support Left", new Vector3(-0.9f, 0.2f, 0f), new Vector3(0.3f, 0.4f, 1f), groundMaterial);
            CreateBlock("Support Right", new Vector3(0.9f, 0.2f, 0f), new Vector3(0.3f, 0.4f, 1f), groundMaterial);

            GameObject spawner = new("Log Spawner");
            spawner.transform.position = new Vector3(0f, 0.4f + LogRadius + 0.05f, 0f);
            SetField(spawner.AddComponent<LogSpawner>(), "_logPrefab", logPrefab);

            // Câmera = jogador: anda, mira e corta
            GameObject cameraObject = GameObject.Find("Main Camera");
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 1.6f, -1.8f), Quaternion.Euler(25f, 0f, 0f));
            cameraObject.GetComponent<Camera>().nearClipPlane = 0.05f;
            cameraObject.AddComponent<FlyCamera>();

            GameObject previewObject = new("Cut Preview");
            previewObject.transform.SetParent(cameraObject.transform, false);
            LineRenderer line = previewObject.AddComponent<LineRenderer>();
            line.sharedMaterial = preview;
            line.widthMultiplier = 0.012f;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 0;

            Transform axe = BuildAxe(cameraObject.transform, axeHandle, axeHead);

            AxeCutter cutter = cameraObject.AddComponent<AxeCutter>();
            SetField(cutter, "_cutPreview", line);
            SetField(cutter, "_axe", axe);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
            Debug.Log("Cena de corte criada em " + ScenePath + ". Aperte Play para testar.");
        }

        private static Transform BuildAxe(Transform parent, Material handleMaterial, Material headMaterial)
        {
            // O pivô fica na mão; o cabo sobe (Y) e a lâmina aponta para frente (Z)
            GameObject pivot = new("Axe");
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = new Vector3(0.38f, -0.42f, 0.55f);
            pivot.transform.localRotation = Quaternion.Euler(-25f, -12f, 0f);

            GameObject handle = CreateVisual(PrimitiveType.Cylinder, "Handle", pivot.transform, handleMaterial);
            handle.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            handle.transform.localScale = new Vector3(0.035f, 0.3f, 0.035f);

            GameObject head = CreateVisual(PrimitiveType.Cube, "Head", pivot.transform, headMaterial);
            head.transform.localPosition = new Vector3(0f, 0.55f, 0.07f);
            head.transform.localScale = new Vector3(0.03f, 0.1f, 0.2f);

            GameObject blade = CreateVisual(PrimitiveType.Cube, "Blade", pivot.transform, headMaterial);
            blade.transform.localPosition = new Vector3(0f, 0.55f, 0.18f);
            blade.transform.localScale = new Vector3(0.015f, 0.17f, 0.05f);

            return pivot.transform;
        }

        private static GameObject CreateVisual(PrimitiveType type, string name, Transform parent, Material material)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = name;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(parent, false);
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return visual;
        }

        private static void CreateBlock(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = scale;
            if (material != null) block.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void BuildLogPrefab(Mesh mesh, Material bark, Material rings)
        {
            GameObject log = new("Log");
            // A malha tem o comprimento no eixo Y; deita o tronco
            log.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            log.AddComponent<MeshFilter>().sharedMesh = mesh;
            log.AddComponent<MeshRenderer>().sharedMaterials = new[] { bark, rings };

            MeshCollider meshCollider = log.AddComponent<MeshCollider>();
            meshCollider.convex = true;
            meshCollider.sharedMesh = mesh;

            Rigidbody body = log.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            CuttableWood wood = log.AddComponent<CuttableWood>();
            SetField(wood, "_ringRadius", LogRadius);

            // Madeira do jogo: tag usada pela refinaria e tipo de madeira para o preço
            log.tag = "Wood";
            SetField(log.AddComponent<WoodPiece>(), "_definition", AssetDatabase.LoadAssetAtPath<WoodDefinition>(WoodTypePath));

            PrefabUtility.SaveAsPrefabAsset(log, LogPrefabPath);
            Object.DestroyImmediate(log);
        }

        // Cilindro com o comprimento no eixo Y. Submesh 0 = casca, submesh 1 = pontas (anéis)
        private static Mesh BuildLogMesh()
        {
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<int> barkTriangles = new();
            List<int> endTriangles = new();

            float half = LogLength / 2f;
            float circumference = 2f * Mathf.PI * LogRadius;

            for (int i = 0; i <= LogSides; i++)
            {
                float angle = i / (float)LogSides * Mathf.PI * 2f;
                Vector3 point = RingPoint(angle);
                Vector3 normal = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                float u = i / (float)LogSides;

                vertices.Add(new Vector3(point.x, -half, point.z));
                normals.Add(normal);
                uvs.Add(new Vector2(u, 0f));

                vertices.Add(new Vector3(point.x, half, point.z));
                normals.Add(normal);
                uvs.Add(new Vector2(u, LogLength / circumference));
            }

            for (int i = 0; i < LogSides; i++)
            {
                int bottom0 = i * 2, top0 = i * 2 + 1, bottom1 = i * 2 + 2, top1 = i * 2 + 3;
                barkTriangles.AddRange(new[] { bottom0, top0, bottom1, bottom1, top0, top1 });
            }

            AddEndCap(vertices, normals, uvs, endTriangles, half, Vector3.up);
            AddEndCap(vertices, normals, uvs, endTriangles, -half, Vector3.down);

            Mesh mesh = new() { name = "Log" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(barkTriangles, 0);
            mesh.SetTriangles(endTriangles, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddEndCap(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles,
            float y, Vector3 normal)
        {
            int center = vertices.Count;
            vertices.Add(new Vector3(0f, y, 0f));
            normals.Add(normal);
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int i = 0; i < LogSides; i++)
            {
                Vector3 point = RingPoint(i / (float)LogSides * Mathf.PI * 2f);
                vertices.Add(new Vector3(point.x, y, point.z));
                normals.Add(normal);
                uvs.Add(new Vector2(point.x / (2f * LogRadius) + 0.5f, point.z / (2f * LogRadius) + 0.5f));
            }

            for (int i = 0; i < LogSides; i++)
            {
                int current = center + 1 + i;
                int next = center + 1 + (i + 1) % LogSides;
                Vector3 face = Vector3.Cross(vertices[current] - vertices[center], vertices[next] - vertices[center]);
                if (Vector3.Dot(face, normal) >= 0f) triangles.AddRange(new[] { center, current, next });
                else triangles.AddRange(new[] { center, next, current });
            }
        }

        // Ponto da borda do tronco, com um pouco de irregularidade para não ficar um cilindro perfeito
        private static Vector3 RingPoint(float angle)
        {
            float radius = LogRadius * (1f + 0.03f * Mathf.Sin(angle * 3f + 1f) + 0.02f * Mathf.Sin(angle * 7f + 2f));
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private static Texture2D GenerateBarkTexture(int size)
        {
            Texture2D texture = new(size, size, TextureFormat.RGB24, true);
            Color dark = new(0.22f, 0.14f, 0.08f);
            Color light = new(0.42f, 0.3f, 0.19f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Mistura duas amostras de ruído para a textura repetir sem emenda na horizontal
                    float t = x / (float)size;
                    float streaks = Mathf.Lerp(Mathf.PerlinNoise(x * 0.06f, y * 0.006f), Mathf.PerlinNoise((x - size) * 0.06f, y * 0.006f), t);
                    float detail = Mathf.Lerp(Mathf.PerlinNoise(x * 0.3f + 50f, y * 0.08f), Mathf.PerlinNoise((x - size) * 0.3f + 50f, y * 0.08f), t);
                    float value = Mathf.Clamp01(streaks * 0.75f + detail * 0.35f - 0.1f);
                    texture.SetPixel(x, y, Color.Lerp(dark, light, value * value));
                }
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D GenerateRingsTexture(int size)
        {
            Texture2D texture = new(size, size, TextureFormat.RGB24, true);
            Color early = new(0.86f, 0.7f, 0.48f);
            Color late = new(0.66f, 0.46f, 0.27f);
            Color bark = new(0.3f, 0.2f, 0.12f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // uv 0..1 -> -1..1, raio 1 = borda do tronco
                    float px = x / (float)size * 2f - 1f;
                    float py = y / (float)size * 2f - 1f;
                    float radius = Mathf.Sqrt(px * px + py * py);
                    float wobble = Mathf.PerlinNoise(px * 3f + 10f, py * 3f + 10f) * 0.08f;
                    float rings = Mathf.Repeat((radius + wobble) * 14f, 1f);
                    Color color = Color.Lerp(early, late, Mathf.SmoothStep(0f, 1f, rings) * rings);
                    color *= 0.92f + Mathf.PerlinNoise(x * 0.4f, y * 0.4f) * 0.12f;
                    if (radius > 0.93f) color = bark;
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return texture;
        }

        private static Material CreateLitMaterial(Color color, Texture texture, float smoothness, float metallic = 0f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new(shader);
            material.SetColor(BaseColorId, color);
            if (texture != null) material.SetTexture(BaseMapId, texture);
            material.SetFloat(SmoothnessId, smoothness);
            material.SetFloat(MetallicId, metallic);
            return material;
        }

        // Cria a pasta (e as pastas pai) se ainda não existir
        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static Texture2D SaveTexture(Texture2D texture, string folder, string name)
        {
            string path = folder + "/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Substitui o asset se já existir, mantendo o mesmo GUID (referências não quebram)
        private static T SaveAsset<T>(T asset, string folder, string fileName) where T : Object
        {
            string path = folder + "/" + fileName;
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }
            EditorUtility.CopySerialized(asset, existing);
            Object.DestroyImmediate(asset);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void SetField(Object target, string field, object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (value is Object reference) property.objectReferenceValue = reference;
            else if (value is float number) property.floatValue = number;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
