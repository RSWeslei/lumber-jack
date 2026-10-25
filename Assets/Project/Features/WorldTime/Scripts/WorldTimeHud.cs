using UnityEngine;
using UnityEngine.UIElements;

namespace LumberJack.WorldTime
{
    /// <summary>
    /// Painel do tempo na HUD: hora, fase do dia, altura do sol, data, estação e horários do sol, com a barra das 24 h
    /// (noite, crepúsculo e dia) e a barra do ano (estações por mês). Os textos que mudam a cada minuto são montados
    /// uma vez só (nada de lixo para o coletor); a data e o sol só mudam quando o dia muda.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class WorldTimeHud : MonoBehaviour
    {
        private const string ClockName = "time-clock";
        private const string PhaseName = "time-phase";
        private const string ElevationName = "time-elevation";
        private const string DateName = "time-date";
        private const string SeasonName = "time-season";
        private const string SunName = "time-sun";
        private const string TwilightName = "day-bar-twilight";
        private const string DaylightName = "day-bar-day";
        private const string DayMarkerName = "day-bar-marker";
        private const string YearBarName = "year-bar";
        private const string YearMarkerName = "year-bar-marker";
        private const float FullPercent = 100f;
        private const int MinutesPerHour = 60;
        private const int MinutesPerDay = 24 * MinutesPerHour;
        private const int MaxElevation = 90;
        private const int PercentSteps = 101;

        private static readonly string[] MonthNames =
        {
            "janeiro", "fevereiro", "março", "abril", "maio", "junho",
            "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"
        };
        private static readonly string[] SeasonNames = { "Primavera", "Verão", "Outono", "Inverno" };
        private static readonly string[] PhaseNames = { "Noite", "Aurora", "Dia", "Crepúsculo" };

        // Textos prontos: "06:30", "sol a 35°", "Primavera (7%)"
        private static readonly string[] ClockTexts = BuildClockTexts();
        private static readonly string[] ElevationTexts = BuildElevationTexts();
        private static readonly string[] SeasonTexts = BuildSeasonTexts();

        [SerializeField] private WorldClock _clock;

        [Header("Cores da barra do ano")]
        [SerializeField] private Color _springColor = new(0.45f, 0.8f, 0.4f);
        [SerializeField] private Color _summerColor = new(0.95f, 0.8f, 0.3f);
        [SerializeField] private Color _autumnColor = new(0.9f, 0.5f, 0.2f);
        [SerializeField] private Color _winterColor = new(0.6f, 0.75f, 0.95f);

        private Label _clockLabel;
        private Label _phaseLabel;
        private Label _elevationLabel;
        private Label _dateLabel;
        private Label _seasonLabel;
        private Label _sunLabel;
        private VisualElement _twilight;
        private VisualElement _daylight;
        private VisualElement _dayMarker;
        private VisualElement _yearMarker;

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _clockLabel = root.Q<Label>(ClockName);
            _phaseLabel = root.Q<Label>(PhaseName);
            _elevationLabel = root.Q<Label>(ElevationName);
            _dateLabel = root.Q<Label>(DateName);
            _seasonLabel = root.Q<Label>(SeasonName);
            _sunLabel = root.Q<Label>(SunName);
            _twilight = root.Q<VisualElement>(TwilightName);
            _daylight = root.Q<VisualElement>(DaylightName);
            _dayMarker = root.Q<VisualElement>(DayMarkerName);
            _yearMarker = root.Q<VisualElement>(YearMarkerName);
            BuildYearBar(root.Q<VisualElement>(YearBarName));
        }

        private void OnEnable()
        {
            _clock.MinuteChanged += HandleMinuteChanged;
            _clock.DayChanged += HandleDayChanged;
        }

        private void Start()
        {
            HandleDayChanged();
            HandleMinuteChanged();
        }

        private void OnDisable()
        {
            _clock.MinuteChanged -= HandleMinuteChanged;
            _clock.DayChanged -= HandleDayChanged;
        }

        private void HandleMinuteChanged()
        {
            GameDate date = _clock.Date;
            SetText(_clockLabel, ClockTexts[date.HourOfDay * MinutesPerHour + date.Minute]);
            SetText(_phaseLabel, PhaseNames[(int)_clock.Phase]);
            int elevation = Mathf.Clamp(Mathf.RoundToInt((float)_clock.SunElevation), -MaxElevation, MaxElevation);
            SetText(_elevationLabel, ElevationTexts[elevation + MaxElevation]);
            int percent = Mathf.Clamp(Mathf.RoundToInt((float)_clock.SeasonProgress * FullPercent), 0, PercentSteps - 1);
            SetText(_seasonLabel, SeasonTexts[(int)_clock.Season * PercentSteps + percent]);
            _dayMarker.style.left = Length.Percent(HourPercent(date.Hour));
            _yearMarker.style.left = Length.Percent((float)date.YearFraction * FullPercent);
        }

        // Uma vez por dia do jogo: os textos novos aqui não pesam
        private void HandleDayChanged()
        {
            GameDate date = _clock.Date;
            SolarDay sun = _clock.Sun;
            _dateLabel.text = $"{date.Day + 1} de {MonthNames[date.Month]}, ano {date.Year} ·";
            _sunLabel.text = $"Nasce {ClockText(sun.Sunrise)} · põe {ClockText(sun.Sunset)} · {sun.DaylightHours:0.0} h de luz";
            SetSpan(_twilight, sun.Dawn, sun.Dusk);
            SetSpan(_daylight, sun.Sunrise, sun.Sunset);
        }

        // Um bloco por mês, com a cor da estação dele (o hemisfério muda as cores)
        private void BuildYearBar(VisualElement bar)
        {
            for (int month = 0; month < GameCalendar.MonthsPerYear; month++)
            {
                var cell = new VisualElement { pickingMode = PickingMode.Ignore };
                cell.style.flexGrow = 1f;
                cell.style.backgroundColor = SeasonColor(SeasonCalculator.SeasonOf(month, _clock.Latitude));
                bar.Insert(month, cell);
            }
        }

        private Color SeasonColor(Season season)
        {
            return season switch
            {
                Season.Spring => _springColor,
                Season.Summer => _summerColor,
                Season.Autumn => _autumnColor,
                _ => _winterColor
            };
        }

        // Os textos vêm das tabelas prontas: comparar a referência basta
        private static void SetText(Label label, string text)
        {
            if (!ReferenceEquals(label.text, text)) label.text = text;
        }

        private static void SetSpan(VisualElement span, double from, double to)
        {
            span.style.left = Length.Percent(HourPercent(from));
            span.style.width = Length.Percent(HourPercent(to) - HourPercent(from));
        }

        private static float HourPercent(double hour)
        {
            return (float)(hour / GameCalendar.HoursPerDay) * FullPercent;
        }

        private static string ClockText(double hour)
        {
            return ClockTexts[Mathf.Clamp((int)(hour * MinutesPerHour), 0, MinutesPerDay - 1)];
        }

        private static string[] BuildClockTexts()
        {
            var texts = new string[MinutesPerDay];
            for (int minute = 0; minute < MinutesPerDay; minute++) texts[minute] = $"{minute / MinutesPerHour:00}:{minute % MinutesPerHour:00}";
            return texts;
        }

        private static string[] BuildElevationTexts()
        {
            var texts = new string[MaxElevation * 2 + 1];
            for (int i = 0; i < texts.Length; i++) texts[i] = $"sol a {i - MaxElevation}°";
            return texts;
        }

        private static string[] BuildSeasonTexts()
        {
            var texts = new string[SeasonNames.Length * PercentSteps];
            for (int season = 0; season < SeasonNames.Length; season++)
            {
                for (int percent = 0; percent < PercentSteps; percent++)
                {
                    texts[season * PercentSteps + percent] = $"{SeasonNames[season]} ({percent}%)";
                }
            }
            return texts;
        }
    }
}
