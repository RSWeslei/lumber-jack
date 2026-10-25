namespace LumberJack.WorldTime
{
    /// <summary>
    /// Data e hora do jogo. Mês 0 = janeiro e dia 0 = primeiro dia do mês (os textos somam 1).
    /// </summary>
    public readonly struct GameDate
    {
        public GameDate(int year, int month, int day, double hour, int daysPerMonth)
        {
            Year = year;
            Month = month;
            Day = day;
            Hour = hour;
            DaysPerMonth = daysPerMonth;
        }

        /// <summary>Ano, começando em 1.</summary>
        public int Year { get; }
        public int Month { get; }
        public int Day { get; }
        /// <summary>Hora do dia, de 0 a 24 (com fração: 6,5 = 06:30).</summary>
        public double Hour { get; }
        public int DaysPerMonth { get; }

        public int HourOfDay => (int)Hour;
        public int Minute => (int)((Hour - HourOfDay) * 60.0);

        /// <summary>Quanto do ano já passou (0 = 1º de janeiro 00:00, 1 = fim de dezembro).</summary>
        public double YearFraction => (Month + (Day + Hour / GameCalendar.HoursPerDay) / DaysPerMonth) / GameCalendar.MonthsPerYear;
    }
}
