using System;
using UnityEngine;

namespace LumberJack.WorldTime
{
    /// <summary>
    /// Dia e noite na tela: põe o sol onde o relógio diz (altura e direção) e a lua do lado oposto, e ajusta céu,
    /// luzes, luz ambiente e neblina pela altura do sol. O céu usa uma cópia do material (o asset não muda).
    /// Atualiza poucas vezes por segundo: o sol anda só ~0,25° por segundo real.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        private const float Deg = Mathf.Deg2Rad;
        // Abaixo disso o sol não ilumina: a lua vira a luz principal (sombras)
        private const float MainLightSwitchElevation = -2f;
        private const float LightOffIntensity = 0.001f;
        // Pulo no relógio maior que isso (horas: botões de teste, dormir) atualiza na hora, sem esperar o intervalo
        private const double JumpHours = 0.05;

        // Direções que o céu (Polyverse Skies) lê para desenhar o sol e a lua
        private static readonly int GlobalSunDirectionId = Shader.PropertyToID("GlobalSunDirection");
        private static readonly int GlobalMoonDirectionId = Shader.PropertyToID("GlobalMoonDirection");
        private static readonly int SkyTopId = Shader.PropertyToID("_SkyColor");
        private static readonly int SkyHorizonId = Shader.PropertyToID("_EquatorColor");
        private static readonly int SkyGroundId = Shader.PropertyToID("_GroundColor");
        private static readonly int SunColorId = Shader.PropertyToID("_SunColor");
        private static readonly int SunIntensityId = Shader.PropertyToID("_SunIntensity");
        private static readonly int MoonIntensityId = Shader.PropertyToID("_MoonIntensity");
        private static readonly int StarsIntensityId = Shader.PropertyToID("_StarsIntensity");
        private static readonly int CloudLightId = Shader.PropertyToID("_CloudsLightColor");
        private static readonly int CloudShadowId = Shader.PropertyToID("_CloudsShadowColor");

        [SerializeField] private WorldClock _clock;
        [SerializeField] private DayNightDefinition _definition;
        [SerializeField] private Light _sun;
        [SerializeField] private Light _moon;
        [Tooltip("Céu do Polyverse Skies com sol, lua e estrelas ligados")]
        [SerializeField] private Material _skyMaterial;
        [Tooltip("Tempo real (s) entre atualizações do sol, do céu e das luzes")]
        [Min(0f)]
        [SerializeField] private float _updateInterval = 0.1f;

        private Transform _sunTransform;
        private Transform _moonTransform;
        private Material _sky;
        private Material _originalSky;
        private float _nextUpdate;
        private double _lastHour = -1.0;
        // Últimos valores aplicados: só chama a engine quando mudam
        private bool _isSunOn;
        private bool _isMoonOn;
        private bool _isMoonMain;

        private void Awake()
        {
            _sunTransform = _sun.transform;
            _moonTransform = _moon.transform;
            _moon.color = _definition.MoonColor;
            _isSunOn = _sun.enabled;
            _isMoonOn = _moon.enabled;
            _isMoonMain = ReferenceEquals(RenderSettings.sun, _moon);
            _sky = new Material(_skyMaterial);
            _originalSky = RenderSettings.skybox;
            RenderSettings.skybox = _sky;
        }

        private void LateUpdate()
        {
            double hour = _clock.ExactHour;
            // Parado (pausado) não muda nada; pulo grande no relógio não espera o intervalo
            if (hour == _lastHour) return;
            bool hasJumped = Math.Abs(hour - _lastHour) > JumpHours;
            if (!hasJumped && Time.unscaledTime < _nextUpdate) return;

            _nextUpdate = Time.unscaledTime + _updateInterval;
            _lastHour = hour;
            Apply(hour);
        }

        private void OnDestroy()
        {
            RenderSettings.skybox = _originalSky;
            Destroy(_sky);
        }

        private void Apply(double hour)
        {
            double declination = _clock.Sun.Declination;
            float elevation = (float)SunCalculator.Elevation(_clock.Latitude, declination, hour);
            float azimuth = (float)SunCalculator.Azimuth(_clock.Latitude, declination, hour) + _definition.NorthYaw;
            Vector3 toSun = Direction(elevation, azimuth);
            Vector3 toMoon = -toSun;

            _sunTransform.rotation = Quaternion.LookRotation(-toSun);
            _moonTransform.rotation = Quaternion.LookRotation(-toMoon);
            Shader.SetGlobalVector(GlobalSunDirectionId, toSun);
            Shader.SetGlobalVector(GlobalMoonDirectionId, toMoon);

            float dayAmount = Mathf.InverseLerp(_definition.SkyRange.Min, _definition.SkyRange.Max, elevation);
            UpdateLights(elevation);
            UpdateSky(elevation, dayAmount);
            UpdateAmbient(dayAmount);
        }

        private void UpdateLights(float elevation)
        {
            float sunIntensity = _definition.SunIntensity.Evaluate(elevation);
            bool isSunOn = sunIntensity > LightOffIntensity;
            if (isSunOn != _isSunOn)
            {
                _isSunOn = isSunOn;
                _sun.enabled = isSunOn;
            }
            if (isSunOn)
            {
                _sun.intensity = sunIntensity;
                _sun.colorTemperature = _definition.SunTemperature.Evaluate(elevation);
            }

            float moonIntensity = _definition.MoonIntensity.Evaluate(elevation);
            bool isMoonOn = moonIntensity > LightOffIntensity;
            if (isMoonOn != _isMoonOn)
            {
                _isMoonOn = isMoonOn;
                _moon.enabled = isMoonOn;
            }
            if (isMoonOn) _moon.intensity = moonIntensity;

            // A luz principal do URP (a que faz sombra) é a do sol de dia e a da lua de noite
            bool isMoonMain = elevation <= MainLightSwitchElevation;
            if (isMoonMain == _isMoonMain) return;
            _isMoonMain = isMoonMain;
            RenderSettings.sun = isMoonMain ? _moon : _sun;
        }

        private void UpdateSky(float elevation, float dayAmount)
        {
            _sky.SetColor(SkyTopId, _definition.SkyTop.Evaluate(dayAmount));
            _sky.SetColor(SkyHorizonId, _definition.SkyHorizon.Evaluate(dayAmount));
            _sky.SetColor(SkyGroundId, _definition.SkyGround.Evaluate(dayAmount));
            _sky.SetColor(SunColorId, _definition.SunColor.Evaluate(dayAmount));
            _sky.SetColor(CloudLightId, _definition.CloudLight.Evaluate(dayAmount));
            _sky.SetColor(CloudShadowId, _definition.CloudShadow.Evaluate(dayAmount));
            _sky.SetFloat(SunIntensityId, _definition.SunDisc.Evaluate(elevation));
            _sky.SetFloat(MoonIntensityId, _definition.MoonDisc.Evaluate(elevation));
            _sky.SetFloat(StarsIntensityId, _definition.Stars.Evaluate(elevation));
        }

        private void UpdateAmbient(float dayAmount)
        {
            RenderSettings.ambientSkyColor = _definition.AmbientSky.Evaluate(dayAmount);
            RenderSettings.ambientEquatorColor = _definition.AmbientEquator.Evaluate(dayAmount);
            RenderSettings.ambientGroundColor = _definition.AmbientGround.Evaluate(dayAmount);
            RenderSettings.fogColor = _definition.Fog.Evaluate(dayAmount);
        }

        // Direção para o sol no mundo: norte = +Z, leste = +X (girada pelo norte do mapa)
        private static Vector3 Direction(float elevation, float azimuth)
        {
            float cosElevation = Mathf.Cos(elevation * Deg);
            return new Vector3(cosElevation * Mathf.Sin(azimuth * Deg), Mathf.Sin(elevation * Deg),
                cosElevation * Mathf.Cos(azimuth * Deg));
        }
    }
}
