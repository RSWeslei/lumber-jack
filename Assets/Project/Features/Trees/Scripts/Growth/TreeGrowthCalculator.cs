using System;
using LumberJack.WorldTime;

namespace LumberJack.Trees
{
    /// <summary>
    /// Quanto uma árvore cresceu entre o momento em que nasceu e agora, somando dia a dia o ritmo da estação.
    /// Só depende das horas do relógio, então dá o mesmo resultado em todo cliente e mesmo se o tempo pular.
    /// </summary>
    public static class TreeGrowthCalculator
    {
        /// <summary>Crescimento de 0 (muda nova) a 1 (adulta).</summary>
        /// <param name="seasonRates">Ritmo de cada estação, na ordem do enum <see cref="Season"/>.</param>
        public static double Growth(double plantedHours, double nowHours, double growingDays, double[] seasonRates,
            GameCalendar calendar, double latitude)
        {
            if (growingDays <= 0.0) return 1.0;

            double grownDays = 0.0;
            double time = plantedHours;
            while (time < nowHours && grownDays < growingDays)
            {
                // Um dia por vez: a estação pode mudar de um dia para o outro
                double dayEnd = (Math.Floor(time / GameCalendar.HoursPerDay) + 1.0) * GameCalendar.HoursPerDay;
                double end = Math.Min(dayEnd, nowHours);
                Season season = SeasonCalculator.SeasonOf(calendar.DateAt(time).Month, latitude);
                grownDays += (end - time) / GameCalendar.HoursPerDay * seasonRates[(int)season];
                time = end;
            }
            return Math.Min(1.0, grownDays / growingDays);
        }

        /// <summary>Tamanho (escala) da árvore para o crescimento dado.</summary>
        public static double Scale(double growth, double saplingScale)
        {
            return saplingScale + (1.0 - saplingScale) * growth;
        }
    }
}
