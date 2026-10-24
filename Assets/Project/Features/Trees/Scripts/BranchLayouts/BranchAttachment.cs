namespace LumberJack.Trees
{
    /// <summary>
    /// Onde um galho nasce no pai: posição ao longo dele e direção em volta dele.
    /// </summary>
    public readonly struct BranchAttachment
    {
        public BranchAttachment(float position, float azimuth)
        {
            Position = position;
            Azimuth = azimuth;
        }

        /// <summary>0 = base do pai, 1 = ponta.</summary>
        public float Position { get; }

        /// <summary>Ângulo (radianos) em volta do pai.</summary>
        public float Azimuth { get; }
    }
}
