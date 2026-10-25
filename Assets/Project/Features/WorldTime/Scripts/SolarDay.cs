namespace LumberJack.WorldTime
{
    /// <summary>Horários do sol num dia (horas de 0 a 24).</summary>
    public readonly struct SolarDay
    {
        public SolarDay(double declination, double sunrise, double sunset, double dawn, double dusk, bool hasSunrise,
            double noonElevation)
        {
            Declination = declination;
            Sunrise = sunrise;
            Sunset = sunset;
            Dawn = dawn;
            Dusk = dusk;
            HasSunrise = hasSunrise;
            NoonElevation = noonElevation;
        }

        /// <summary>Declinação do sol (graus) no dia.</summary>
        public double Declination { get; }
        public double Sunrise { get; }
        public double Sunset { get; }
        /// <summary>Começo do crepúsculo da manhã (sol a 6° abaixo do horizonte).</summary>
        public double Dawn { get; }
        /// <summary>Fim do crepúsculo da tarde.</summary>
        public double Dusk { get; }
        /// <summary>False perto dos polos, quando o sol não nasce ou não se põe nesse dia.</summary>
        public bool HasSunrise { get; }
        /// <summary>Altura máxima do sol no dia (graus).</summary>
        public double NoonElevation { get; }

        public double DaylightHours => Sunset - Sunrise;
    }
}
