using UnityEngine;

namespace LumberJack.WorldTime
{
    /// <summary>
    /// Regras do tempo do mundo: duração do dia, tamanho dos meses, latitude do mapa (decide o nascer e o pôr do sol
    /// em cada mês) e a data em que o jogo começa.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWorldTime", menuName = "Lumber Jack/World Time/World Time")]
    public class WorldTimeDefinition : ScriptableObject
    {
        private const float MaxLatitude = 66f;
        private const int LastMonth = GameCalendar.MonthsPerYear - 1;
        private const float SecondsPerMinute = 60f;

        [Tooltip("Latitude do mapa (graus; negativa = hemisfério sul)")]
        [Range(-MaxLatitude, MaxLatitude)]
        [SerializeField] private float _latitude = 50f;
        [Tooltip("Minutos reais que dura um dia inteiro do jogo")]
        [Min(1f)]
        [SerializeField] private float _realMinutesPerDay = 24f;
        [Tooltip("Dias de cada mês do jogo")]
        [Min(1)]
        [SerializeField] private int _daysPerMonth = 7;

        [Header("Começo do jogo")]
        [Tooltip("Mês inicial (0 = janeiro)")]
        [Range(0, LastMonth)]
        [SerializeField] private int _startMonth = 2;
        [Tooltip("Dia inicial do mês (0 = primeiro dia)")]
        [Min(0)]
        [SerializeField] private int _startDay;
        [Tooltip("Hora inicial (6,5 = 06:30)")]
        [Range(0f, 23.99f)]
        [SerializeField] private float _startHour = 6f;

        public double Latitude => _latitude;
        public int DaysPerMonth => _daysPerMonth;
        /// <summary>Horas do jogo que passam a cada segundo real.</summary>
        public double GameHoursPerSecond => GameCalendar.HoursPerDay / (_realMinutesPerDay * SecondsPerMinute);

        private void OnValidate()
        {
            _startDay = Mathf.Clamp(_startDay, 0, _daysPerMonth - 1);
        }

        public double StartHours(GameCalendar calendar)
        {
            return calendar.HoursAt(1, _startMonth, _startDay, _startHour);
        }
    }
}
