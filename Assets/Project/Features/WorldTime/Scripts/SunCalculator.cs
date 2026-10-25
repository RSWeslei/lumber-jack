using System;

namespace LumberJack.WorldTime
{
    /// <summary>
    /// Posição do sol e horários do dia pela latitude e pela data equivalente do ano real (fórmulas astronômicas
    /// simplificadas, sem equação do tempo: o meio-dia solar é 12:00).
    /// </summary>
    public static class SunCalculator
    {
        private const double DaysPerRealYear = 365.2422;
        private const double AxialTilt = 23.44;
        private const double OrbitEccentricity = 0.0167;
        // Dias entre o solstício de dezembro e 1º de janeiro, e entre 1º de janeiro e o periélio
        private const double SolsticeOffset = 10.0;
        private const double PerihelionOffset = 2.0;
        // O sol nasce quando o topo do disco passa o horizonte (refração + raio do disco)
        private const double SunriseElevation = -0.833;
        // Crepúsculo civil: ainda dá para enxergar sem luz artificial
        private const double TwilightElevation = -6.0;
        private const double DegreesPerHour = 15.0;
        private const double SolarNoon = 12.0;
        private const double Deg = Math.PI / 180.0;

        /// <summary>Dia do ano real (0 = 1º de janeiro) equivalente à fração do ano do jogo.</summary>
        public static double RealDayOfYear(double yearFraction)
        {
            return yearFraction * DaysPerRealYear;
        }

        /// <summary>Declinação do sol (graus): +23,4° no solstício de junho, -23,4° no de dezembro.</summary>
        public static double Declination(double realDayOfYear)
        {
            double orbit = 2.0 * Math.PI / DaysPerRealYear;
            double angle = orbit * (realDayOfYear + SolsticeOffset) +
                           2.0 * OrbitEccentricity * Math.Sin(orbit * (realDayOfYear - PerihelionOffset));
            return Math.Asin(Math.Sin(-AxialTilt * Deg) * Math.Cos(angle)) / Deg;
        }

        /// <summary>Altura do sol acima do horizonte (graus; negativa à noite).</summary>
        public static double Elevation(double latitude, double declination, double hour)
        {
            double hourAngle = (hour - SolarNoon) * DegreesPerHour * Deg;
            double sin = Math.Sin(latitude * Deg) * Math.Sin(declination * Deg) +
                         Math.Cos(latitude * Deg) * Math.Cos(declination * Deg) * Math.Cos(hourAngle);
            return Math.Asin(Clamp(sin)) / Deg;
        }

        /// <summary>Direção do sol na bússola (graus a partir do norte, sentido horário: 90 = leste).</summary>
        public static double Azimuth(double latitude, double declination, double hour)
        {
            double hourAngle = (hour - SolarNoon) * DegreesPerHour * Deg;
            double azimuth = Math.Atan2(Math.Sin(hourAngle),
                Math.Cos(hourAngle) * Math.Sin(latitude * Deg) - Math.Tan(declination * Deg) * Math.Cos(latitude * Deg));
            return (azimuth / Deg + 180.0) % 360.0;
        }

        /// <summary>Horários do sol no dia: nascer, pôr e o crepúsculo civil antes e depois.</summary>
        public static SolarDay Day(double latitude, double realDayOfYear)
        {
            double declination = Declination(realDayOfYear);
            HoursAround(latitude, declination, SunriseElevation, out double sunrise, out double sunset, out bool hasSunrise);
            HoursAround(latitude, declination, TwilightElevation, out double dawn, out double dusk, out _);
            return new SolarDay(declination, sunrise, sunset, dawn, dusk, hasSunrise, Elevation(latitude, declination, SolarNoon));
        }

        public static DayPhase PhaseAt(double elevation, double hour)
        {
            if (elevation >= SunriseElevation) return DayPhase.Day;
            if (elevation < TwilightElevation) return DayPhase.Night;
            return hour < SolarNoon ? DayPhase.Dawn : DayPhase.Dusk;
        }

        // Horas em que o sol cruza a altura dada; sem cruzamento o dia todo fica acima (sol da meia-noite) ou abaixo
        private static void HoursAround(double latitude, double declination, double elevation, out double rise, out double set,
            out bool crosses)
        {
            double cos = (Math.Sin(elevation * Deg) - Math.Sin(latitude * Deg) * Math.Sin(declination * Deg)) /
                         (Math.Cos(latitude * Deg) * Math.Cos(declination * Deg));
            crosses = cos > -1.0 && cos < 1.0;
            double halfDay = Math.Acos(Clamp(cos)) / Deg / DegreesPerHour;
            rise = SolarNoon - halfDay;
            set = SolarNoon + halfDay;
        }

        private static double Clamp(double value)
        {
            return Math.Max(-1.0, Math.Min(1.0, value));
        }
    }
}
