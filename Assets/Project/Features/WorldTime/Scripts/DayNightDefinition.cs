using LumberJack.Core;
using UnityEngine;

namespace LumberJack.WorldTime
{
    /// <summary>
    /// Visual do dia e da noite pela altura do sol: céu, sol, lua, estrelas, luzes, luz ambiente e neblina.
    /// As curvas usam a altura do sol em graus; os gradientes vão de noite (esquerda) a dia (direita) pela faixa
    /// <see cref="SkyRange"/>, então o meio deles é a aurora e o crepúsculo.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDayNight", menuName = "Lumber Jack/World Time/Day Night")]
    public class DayNightDefinition : ScriptableObject
    {
        [Tooltip("Gira a bússola do mundo (graus): onde fica o norte no mapa")]
        [SerializeField] private float _northYaw;
        [Tooltip("Altura do sol (graus) do começo e do fim dos gradientes: abaixo é noite, acima é dia")]
        [RangeLimits(-30f, 60f)]
        [SerializeField] private FloatRange _skyRange = new(-15f, 30f);

        [Header("Sol (curvas pela altura do sol, graus)")]
        [SerializeField] private AnimationCurve _sunIntensity = AnimationCurve.Linear(-2f, 0f, 30f, 1.3f);
        [Tooltip("Temperatura da luz do sol (Kelvin): baixa = alaranjada")]
        [SerializeField] private AnimationCurve _sunTemperature = AnimationCurve.Linear(0f, 2000f, 40f, 5800f);
        [Tooltip("Brilho do disco do sol no céu")]
        [SerializeField] private AnimationCurve _sunDisc = AnimationCurve.Linear(-3f, 0f, 2f, 1.5f);

        [Header("Lua e estrelas (curvas pela altura do sol, graus)")]
        [SerializeField] private AnimationCurve _moonIntensity = AnimationCurve.Linear(-8f, 0.12f, -2f, 0f);
        [SerializeField] private Color _moonColor = new(0.6f, 0.7f, 1f);
        [Tooltip("Brilho do disco da lua no céu")]
        [SerializeField] private AnimationCurve _moonDisc = AnimationCurve.Linear(-6f, 1f, 4f, 0f);
        [SerializeField] private AnimationCurve _stars = AnimationCurve.Linear(-12f, 2f, -2f, 0f);

        [Header("Céu (gradientes noite → dia)")]
        [SerializeField] private Gradient _skyTop = new();
        [SerializeField] private Gradient _skyHorizon = new();
        [SerializeField] private Gradient _skyGround = new();
        [SerializeField] private Gradient _sunColor = new();
        [SerializeField] private Gradient _cloudLight = new();
        [SerializeField] private Gradient _cloudShadow = new();

        [Header("Luz ambiente e neblina (gradientes noite → dia)")]
        [SerializeField] private Gradient _ambientSky = new();
        [SerializeField] private Gradient _ambientEquator = new();
        [SerializeField] private Gradient _ambientGround = new();
        [SerializeField] private Gradient _fog = new();

        public float NorthYaw => _northYaw;
        public FloatRange SkyRange => _skyRange;
        public AnimationCurve SunIntensity => _sunIntensity;
        public AnimationCurve SunTemperature => _sunTemperature;
        public AnimationCurve SunDisc => _sunDisc;
        public AnimationCurve MoonIntensity => _moonIntensity;
        public Color MoonColor => _moonColor;
        public AnimationCurve MoonDisc => _moonDisc;
        public AnimationCurve Stars => _stars;
        public Gradient SkyTop => _skyTop;
        public Gradient SkyHorizon => _skyHorizon;
        public Gradient SkyGround => _skyGround;
        public Gradient SunColor => _sunColor;
        public Gradient CloudLight => _cloudLight;
        public Gradient CloudShadow => _cloudShadow;
        public Gradient AmbientSky => _ambientSky;
        public Gradient AmbientEquator => _ambientEquator;
        public Gradient AmbientGround => _ambientGround;
        public Gradient Fog => _fog;

        private void OnValidate()
        {
            _skyRange = _skyRange.Clamped(-30f, 60f);
        }
    }
}
