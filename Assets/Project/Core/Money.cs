namespace LumberJack.Core
{
    /// <summary>
    /// Dinheiro do jogo é sempre em centavos inteiros (int): somar e subtrair nunca acumula
    /// erro de arredondamento como float.
    /// </summary>
    public static class Money
    {
        private const int CentsPerUnit = 100;

        /// <summary>
        /// "$ 12.34", igual em qualquer idioma do sistema. Cria uma string: use só quando o valor muda.
        /// </summary>
        public static string Format(int cents)
        {
            string sign = cents < 0 ? "-" : string.Empty;
            int absolute = cents < 0 ? -cents : cents;
            return $"{sign}$ {absolute / CentsPerUnit}.{absolute % CentsPerUnit:00}";
        }

        /// <summary>
        /// Valor em unidades para TMP_Text.SetText("{0:2}", ...), que formata sem alocar.
        /// </summary>
        public static float ToUnits(int cents)
        {
            return cents / (float)CentsPerUnit;
        }
    }
}
