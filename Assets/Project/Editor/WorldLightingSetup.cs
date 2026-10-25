using System.IO;
using System.Linq;
using LumberJack.WorldTime;
using PolyverseSkiesAsset;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LumberJack.Editor
{
    /// <summary>
    /// Iluminação do mundo: Menu "Lumber Jack > Aplicar iluminação do mundo" (ou o arquivo de comando em Temp).
    /// Ambiente em gradiente (não depende de bake, então o lado sem sol nunca fica preto), sol, lua, neblina,
    /// céu do Polyverse Skies e pós-processamento, e o ciclo de dia e noite que muda tudo isso pelo relógio
    /// (valores em <see cref="DayNightDefinition"/>). Gera só os reflexos do céu (sem GI, rápido). Pode rodar de novo.
    /// </summary>
    [InitializeOnLoad]
    public static class WorldLightingSetup
    {
        private const string CommandPath = "Temp/LumberJack/run-world-lighting";
        private const string ReportPath = "Logs/world-lighting.txt";
        private const double PollSeconds = 1.0;
        private const string SkyPresetPath = "Assets/ThirdParty/Low Poly Sky/Polyverse Skies/Core/Presets/Sky 03.mat";
        private const string SkyMaterialFolder = "Assets/Project/Features/WorldTime/Art";
        private const string SkyMaterialPath = SkyMaterialFolder + "/WorldSky.mat";
        private const string DayNightPath = "Assets/Project/Features/WorldTime/Data/DayNight.asset";
        private const string MoonName = "Moon";
        private const string CycleName = "DayNightCycle";
        private const float MoonShadowStrength = 0.6f;

        private const string VolumeProfilePath = "Assets/Project/Settings/MainVolumeProfile.asset";
        private const string LightingSettingsPath = "Assets/Project/Settings/MainLighting.lighting";

        // Ambiente em gradiente: céu (azulado), horizonte (quente) e chão (terra)
        private static readonly Color AmbientSky = new(0.40f, 0.45f, 0.55f);
        private static readonly Color AmbientEquator = new(0.45f, 0.38f, 0.31f);
        private static readonly Color AmbientGround = new(0.20f, 0.17f, 0.14f);

        // Sol baixo e quente: sombras compridas e luz dourada
        private static readonly Vector3 SunRotation = new(38f, -35f, 0f);
        private static readonly Color SunFilter = new(1f, 0.92f, 0.80f);
        private const float SunTemperatureKelvin = 4600f;
        private const float SunIntensity = 1.25f;
        // Sombra não totalmente opaca: o ambiente ainda clareia um pouco
        private const float SunShadowStrength = 0.85f;

        private static readonly Color FogColor = new(0.86f, 0.68f, 0.52f);
        private const float FogDensity = 0.0045f;

        private const float PostExposure = 0f;
        private const float Contrast = 10f;
        private const float Saturation = 12f;
        private const float WhiteBalanceTemperature = 8f;
        private const float BloomIntensity = 0.4f;
        private const float BloomThreshold = 1.1f;

        private static double _nextPoll;

        static WorldLightingSetup()
        {
            EditorApplication.update += HandleUpdate;
        }

        [MenuItem("Lumber Jack/Aplicar iluminação do mundo")]
        public static void Apply()
        {
            Light sun = FindSun();
            if (sun == null)
            {
                Debug.LogError("Nenhuma Directional Light na cena aberta.");
                return;
            }

            ApplyAmbientAndFog(sun);
            ApplySun(sun);
            RemoveFixedSkySun(sun);
            ApplyDayNight(sun);
            ApplyPostProcessing();
            EditorSceneManager.MarkSceneDirty(sun.gameObject.scene);
            BakeReflectionsOnly();
            Debug.Log("Iluminação do mundo aplicada. Salve a cena (Ctrl+S).");
        }

        private static Light FindSun()
        {
            if (RenderSettings.sun != null) return RenderSettings.sun;

            return Object.FindObjectsByType<Light>().FirstOrDefault(light => light.type == LightType.Directional);
        }

        private static void ApplyAmbientAndFog(Light sun)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky;
            RenderSettings.ambientEquatorColor = AmbientEquator;
            RenderSettings.ambientGroundColor = AmbientGround;
            RenderSettings.sun = sun;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogDensity = FogDensity;
        }

        private static void ApplySun(Light sun)
        {
            Undo.RecordObjects(new Object[] { sun, sun.transform }, "Aplicar iluminação do mundo");
            sun.transform.rotation = Quaternion.Euler(SunRotation);
            sun.useColorTemperature = true;
            sun.colorTemperature = SunTemperatureKelvin;
            sun.color = SunFilter;
            sun.intensity = SunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = SunShadowStrength;
        }

        private static void HandleUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + PollSeconds;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!File.Exists(CommandPath)) return;

            File.Delete(CommandPath);
            Apply();
            EditorSceneManager.SaveOpenScenes();
            File.WriteAllText(ReportPath, "OK");
        }

        // O DayNightCycle passa as direções do sol e da lua para o céu: o componente do Polyverse brigaria com ele
        private static void RemoveFixedSkySun(Light sun)
        {
            if (sun.TryGetComponent(out PolyverseSkies sky)) Undo.DestroyObjectImmediate(sky);
        }

        // Lua, céu e o ciclo que move o sol pelo relógio do mundo
        private static void ApplyDayNight(Light sun)
        {
            // Cenas de teste não têm relógio: ficam com o sol parado
            var clock = Object.FindAnyObjectByType<WorldClock>();
            if (clock == null) return;

            Transform lighting = sun.transform.parent;
            Light moon = FindOrCreateMoon(lighting);
            Material sky = LoadOrCreateSky();
            DayNightDefinition definition = LoadOrCreateDayNight();
            RenderSettings.skybox = sky;

            Transform cycleTransform = lighting.Find(CycleName);
            if (cycleTransform == null)
            {
                var cycleObject = new GameObject(CycleName);
                Undo.RegisterCreatedObjectUndo(cycleObject, "Aplicar iluminação do mundo");
                cycleTransform = cycleObject.transform;
                cycleTransform.SetParent(lighting, false);
            }
            if (!cycleTransform.TryGetComponent(out DayNightCycle cycle)) cycle = Undo.AddComponent<DayNightCycle>(cycleTransform.gameObject);

            var serialized = new SerializedObject(cycle);
            serialized.FindProperty("_clock").objectReferenceValue = clock;
            serialized.FindProperty("_definition").objectReferenceValue = definition;
            serialized.FindProperty("_sun").objectReferenceValue = sun;
            serialized.FindProperty("_moon").objectReferenceValue = moon;
            serialized.FindProperty("_skyMaterial").objectReferenceValue = sky;
            serialized.ApplyModifiedProperties();
        }

        private static Light FindOrCreateMoon(Transform lighting)
        {
            Transform existing = lighting.Find(MoonName);
            Light moon = existing != null ? existing.GetComponent<Light>() : null;
            if (moon == null)
            {
                var moonObject = new GameObject(MoonName);
                Undo.RegisterCreatedObjectUndo(moonObject, "Aplicar iluminação do mundo");
                moonObject.transform.SetParent(lighting, false);
                moon = moonObject.AddComponent<Light>();
            }

            Undo.RecordObject(moon, "Aplicar iluminação do mundo");
            moon.type = LightType.Directional;
            moon.useColorTemperature = false;
            moon.intensity = 0f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = MoonShadowStrength;
            return moon;
        }

        // Cópia de um céu do pack com sol, lua e estrelas ligados (as cores são do DayNightCycle)
        private static Material LoadOrCreateSky()
        {
            var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
            if (sky == null)
            {
                EnsureFolder(SkyMaterialFolder);
                AssetDatabase.CopyAsset(SkyPresetPath, SkyMaterialPath);
                sky = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
            }

            sky.SetFloat("_EnableSun", 1f);
            sky.EnableKeyword("_ENABLESUN_ON");
            sky.SetFloat("_EnableMoon", 1f);
            sky.EnableKeyword("_ENABLEMOON_ON");
            sky.SetFloat("_EnableStars", 1f);
            sky.EnableKeyword("_ENABLESTARS_ON");
            EditorUtility.SetDirty(sky);
            return sky;
        }

        // Valores iniciais do dia e da noite; depois de criado, o asset é ajustado no inspector
        private static DayNightDefinition LoadOrCreateDayNight()
        {
            var definition = AssetDatabase.LoadAssetAtPath<DayNightDefinition>(DayNightPath);
            if (definition != null) return definition;

            definition = ScriptableObject.CreateInstance<DayNightDefinition>();
            AssetDatabase.CreateAsset(definition, DayNightPath);
            var serialized = new SerializedObject(definition);
            // Pontos: 0 = noite, 0,2 = sol 6° abaixo, 0,33 = horizonte, 0,55 = 10° de altura, 1 = dia pleno
            SetGradient(serialized, "_skyTop", (0f, Rgb(0.02f, 0.03f, 0.1f)), (0.2f, Rgb(0.1f, 0.12f, 0.35f)),
                (0.4f, Rgb(0.2f, 0.35f, 0.7f)), (0.7f, Rgb(0.18f, 0.45f, 0.95f)));
            SetGradient(serialized, "_skyHorizon", (0f, Rgb(0.04f, 0.05f, 0.12f)), (0.22f, Rgb(0.55f, 0.25f, 0.25f)),
                (0.33f, Rgb(1f, 0.55f, 0.2f)), (0.5f, Rgb(0.95f, 0.8f, 0.6f)), (0.75f, Rgb(0.7f, 0.85f, 1f)));
            SetGradient(serialized, "_skyGround", (0f, Rgb(0.02f, 0.02f, 0.05f)), (0.3f, Rgb(0.25f, 0.2f, 0.3f)),
                (0.7f, Rgb(0.55f, 0.65f, 0.8f)));
            SetGradient(serialized, "_sunColor", (0.33f, Rgb(1f, 0.5f, 0.2f)), (0.6f, Rgb(1f, 0.95f, 0.85f)));
            SetGradient(serialized, "_cloudLight", (0f, Rgb(0.15f, 0.17f, 0.25f)), (0.33f, Rgb(1f, 0.65f, 0.45f)),
                (0.65f, Rgb(1f, 1f, 1f)));
            SetGradient(serialized, "_cloudShadow", (0f, Rgb(0.05f, 0.06f, 0.12f)), (0.33f, Rgb(0.45f, 0.35f, 0.5f)),
                (0.65f, Rgb(0.55f, 0.6f, 0.75f)));
            SetGradient(serialized, "_ambientSky", (0f, Rgb(0.04f, 0.05f, 0.1f)), (0.3f, Rgb(0.25f, 0.22f, 0.35f)),
                (0.65f, AmbientSky));
            SetGradient(serialized, "_ambientEquator", (0f, Rgb(0.03f, 0.04f, 0.07f)), (0.33f, Rgb(0.5f, 0.33f, 0.25f)),
                (0.65f, AmbientEquator));
            SetGradient(serialized, "_ambientGround", (0f, Rgb(0.02f, 0.02f, 0.03f)), (0.65f, AmbientGround));
            SetGradient(serialized, "_fog", (0f, Rgb(0.05f, 0.06f, 0.1f)), (0.33f, Rgb(0.8f, 0.55f, 0.4f)),
                (0.65f, Rgb(0.72f, 0.78f, 0.85f)));
            SetCurve(serialized, "_sunIntensity", (-2f, 0f), (3f, 0.4f), (15f, 1f), (40f, 1.3f));
            SetCurve(serialized, "_sunTemperature", (0f, 1900f), (8f, 3500f), (25f, 5200f), (60f, 5800f));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void SetGradient(SerializedObject serialized, string property, params (float time, Color color)[] keys)
        {
            var colorKeys = new GradientColorKey[keys.Length];
            for (int i = 0; i < keys.Length; i++) colorKeys[i] = new GradientColorKey(keys[i].color, keys[i].time);
            var gradient = new Gradient();
            gradient.SetKeys(colorKeys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            serialized.FindProperty(property).gradientValue = gradient;
        }

        private static void SetCurve(SerializedObject serialized, string property, params (float time, float value)[] keys)
        {
            var curve = new AnimationCurve();
            foreach ((float time, float value) in keys) curve.AddKey(time, value);
            serialized.FindProperty(property).animationCurveValue = curve;
        }

        private static Color Rgb(float r, float g, float b)
        {
            return new Color(r, g, b);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            int slash = folder.LastIndexOf('/');
            EnsureFolder(folder.Substring(0, slash));
            AssetDatabase.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
        }

        private static void ApplyPostProcessing()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                Debug.LogWarning($"Perfil de pós-processamento não encontrado em {VolumeProfilePath}.");
                return;
            }

            ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.Override(PostExposure);
            color.contrast.Override(Contrast);
            color.saturation.Override(Saturation);

            WhiteBalance whiteBalance = GetOrAdd<WhiteBalance>(profile);
            whiteBalance.temperature.Override(WhiteBalanceTemperature);

            Bloom bloom = GetOrAdd<Bloom>(profile);
            bloom.intensity.Override(BloomIntensity);
            bloom.threshold.Override(BloomThreshold);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        // Novos componentes viram sub-assets do perfil para serem salvos junto
        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T component)) return component;

            component = profile.Add<T>();
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        // Sem GI: só gera a sonda de ambiente e os reflexos do céu (sem isso, superfícies lisas refletem preto)
        private static void BakeReflectionsOnly()
        {
            LightingSettings settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
            if (settings == null)
            {
                settings = new LightingSettings();
                AssetDatabase.CreateAsset(settings, LightingSettingsPath);
            }
            settings.bakedGI = false;
            settings.realtimeGI = false;
            EditorUtility.SetDirty(settings);
            Lightmapping.lightingSettings = settings;

            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            Lightmapping.BakeAsync();
        }
    }
}
