using System;
using UnityEngine;

namespace LumberJack.WorldTime
{
    /// <summary>
    /// Relógio do mundo: faz o tempo andar e mantém a data, a estação, a fase do dia e a posição do sol. Avisa por
    /// eventos quando o minuto, o dia, a estação ou a fase mudam (quem mostra ou reage não precisa olhar todo quadro).
    /// </summary>
    public class WorldClock : MonoBehaviour
    {
        private const double MinutesPerHour = 60.0;

        [SerializeField] private WorldTimeDefinition _definition;
        [Tooltip("Multiplica a velocidade do tempo (para testar)")]
        [Min(0f)]
        [SerializeField] private float _timeScale = 1f;
        [SerializeField] private bool _isPaused;

        private GameCalendar _calendar;
        private WorldTimeState _state;
        private long _minuteIndex = -1;
        private long _dayIndex = -1;
        private bool _hasSeason;
        private bool _hasPhase;

        public GameDate Date { get; private set; }
        public Season Season { get; private set; }
        /// <summary>Quanto da estação já passou (0 a 1).</summary>
        public double SeasonProgress { get; private set; }
        public DayPhase Phase { get; private set; }
        /// <summary>Horários do sol hoje.</summary>
        public SolarDay Sun { get; private set; }
        /// <summary>Altura do sol (graus), atualizada a cada minuto do jogo.</summary>
        public double SunElevation { get; private set; }
        /// <summary>Direção do sol na bússola (graus a partir do norte), atualizada a cada minuto do jogo.</summary>
        public double SunAzimuth { get; private set; }
        public double Latitude => _definition.Latitude;
        /// <summary>Hora exata do dia (0 a 24), sem arredondar para o minuto: para mover o sol suave.</summary>
        public double ExactHour => _state.TotalHours % GameCalendar.HoursPerDay;
        public bool IsPaused => _isPaused;

        public event Action MinuteChanged;
        public event Action DayChanged;
        public event Action<Season> SeasonChanged;
        public event Action<DayPhase> PhaseChanged;

        private void Awake()
        {
            _calendar = new GameCalendar(_definition.DaysPerMonth);
            _state = new WorldTimeState(_definition.StartHours(_calendar));
            Refresh();
        }

        private void Update()
        {
            if (_isPaused) return;

            _state.Advance(Time.deltaTime * _definition.GameHoursPerSecond * _timeScale);
            Refresh();
        }

        /// <summary>Pula o tempo para frente (horas do jogo).</summary>
        public void AdvanceHours(double hours)
        {
            _state.Advance(hours);
            Refresh();
        }

        public void SetPaused(bool isPaused)
        {
            _isPaused = isPaused;
        }

        private void Refresh()
        {
            double totalHours = _state.TotalHours;
            long minuteIndex = (long)(totalHours * MinutesPerHour);
            if (minuteIndex == _minuteIndex) return;

            _minuteIndex = minuteIndex;
            Date = _calendar.DateAt(totalHours);
            long dayIndex = (long)(totalHours / GameCalendar.HoursPerDay);
            bool isNewDay = dayIndex != _dayIndex;
            if (isNewDay)
            {
                _dayIndex = dayIndex;
                // Os horários do sol valem para o dia todo: calcula ao meio-dia
                double noonFraction = (Date.Month + (Date.Day + 0.5) / Date.DaysPerMonth) / GameCalendar.MonthsPerYear;
                Sun = SunCalculator.Day(Latitude, SunCalculator.RealDayOfYear(noonFraction));
            }

            SeasonProgress = SeasonCalculator.SeasonProgress(Date, Latitude);
            SunElevation = SunCalculator.Elevation(Latitude, Sun.Declination, Date.Hour);
            SunAzimuth = SunCalculator.Azimuth(Latitude, Sun.Declination, Date.Hour);
            Season season = SeasonCalculator.SeasonOf(Date.Month, Latitude);
            DayPhase phase = SunCalculator.PhaseAt(SunElevation, Date.Hour);
            bool isNewSeason = !_hasSeason || season != Season;
            bool isNewPhase = !_hasPhase || phase != Phase;
            Season = season;
            Phase = phase;
            _hasSeason = true;
            _hasPhase = true;

            if (isNewDay) DayChanged?.Invoke();
            if (isNewSeason) SeasonChanged?.Invoke(season);
            if (isNewPhase) PhaseChanged?.Invoke(phase);
            MinuteChanged?.Invoke();
        }
    }
}
