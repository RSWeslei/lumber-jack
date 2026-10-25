namespace LumberJack.WorldTime
{
    /// <summary>
    /// Estação do mês (meteorológica: inverno = dez, jan, fev no norte); no hemisfério sul, 6 meses deslocada.
    /// A estação é a mesma no mapa inteiro; cada bioma decide como reage a ela.
    /// </summary>
    public static class SeasonCalculator
    {
        private const int MonthsPerSeason = 3;
        private const int SouthernShift = 6;
        // Março abre a primavera no norte: os meses contam a partir dele
        private const int SpringStartMonth = 2;

        public static Season SeasonOf(int month, double latitude)
        {
            return (Season)(MonthsSinceSpring(month, latitude) / MonthsPerSeason);
        }

        /// <summary>Quanto da estação já passou (0 = começo, 1 = fim).</summary>
        public static double SeasonProgress(GameDate date, double latitude)
        {
            int monthInSeason = MonthsSinceSpring(date.Month, latitude) % MonthsPerSeason;
            double days = monthInSeason * date.DaysPerMonth + date.Day + date.Hour / GameCalendar.HoursPerDay;
            return days / (MonthsPerSeason * date.DaysPerMonth);
        }

        private static int MonthsSinceSpring(int month, double latitude)
        {
            int shift = latitude < 0.0 ? SouthernShift : 0;
            return (month - SpringStartMonth + shift + GameCalendar.MonthsPerYear) % GameCalendar.MonthsPerYear;
        }
    }
}
