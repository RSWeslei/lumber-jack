using System.Linq;
using PolyverseSkiesAsset;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LumberJack.Editor
{
    /// <summary>
    /// Iluminação do mundo no estilo low poly "hora dourada": Menu "Lumber Jack > Aplicar iluminação do mundo".
    /// Ambiente em gradiente (não depende de bake, então o lado sem sol nunca fica preto), sol quente,
    /// neblina, sol desenhado no céu (Polyverse Skies) e pós-processamento. Gera só os reflexos do céu
    /// (sem GI, rápido). Pode rodar de novo.
    /// </summary>
    public static class WorldLightingSetup
    {
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
            ApplySkySun(sun);
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

        // O céu (Polyverse Skies) só desenha o sol se um PolyverseSkies na cena apontar para a luz
        private static void ApplySkySun(Light sun)
        {
            if (!sun.TryGetComponent(out PolyverseSkies sky)) sky = Undo.AddComponent<PolyverseSkies>(sun.gameObject);

            Undo.RecordObject(sky, "Aplicar iluminação do mundo");
            sky.sunDirection = sun.gameObject;
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
