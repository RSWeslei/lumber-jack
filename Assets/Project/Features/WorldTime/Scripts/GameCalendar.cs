using System;

namespace LumberJack.WorldTime
{
    /// <summary>
    /// Calendário do jogo: 12 meses com os nomes reais, mas curtos (poucos dias cada). O tempo do mundo é um número só,
    /// as horas desde 1º de janeiro do ano 1, então todos os clientes chegam à mesma data.
    /// </summary>
    public sealed class GameCalendar
    {
        public const int MonthsPerYear = 12;
        public const double HoursPerDay = 24.0;

        public GameCalendar(int daysPerMonth)
        {
            if (daysPerMonth < 1) throw new ArgumentOutOfRangeException(nameof(daysPerMonth));
            DaysPerMonth = daysPerMonth;
        }

        public int DaysPerMonth { get; }
        public int DaysPerYear => DaysPerMonth * MonthsPerYear;

        public GameDate DateAt(double totalHours)
        {
            if (totalHours < 0.0) totalHours = 0.0;
            long totalDays = (long)Math.Floor(totalHours / HoursPerDay);
            double hour = totalHours - totalDays * HoursPerDay;
            int year = (int)(totalDays / DaysPerYear) + 1;
            int dayOfYear = (int)(totalDays % DaysPerYear);
            return new GameDate(year, dayOfYear / DaysPerMonth, dayOfYear % DaysPerMonth, hour, DaysPerMonth);
        }

        /// <summary>Horas desde o começo do mundo até a data dada (mês e dia começando em 0).</summary>
        public double HoursAt(int year, int month, int day, double hour)
        {
            long days = (long)(year - 1) * DaysPerYear + (long)month * DaysPerMonth + day;
            return days * HoursPerDay + hour;
        }
    }
}
