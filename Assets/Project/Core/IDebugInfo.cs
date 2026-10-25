using System.Text;

namespace LumberJack.Core
{
    /// <summary>
    /// Componente que descreve o próprio estado no painel de debug (o que o jogador está mirando).
    /// </summary>
    public interface IDebugInfo
    {
        void AppendDebugInfo(StringBuilder text);
    }
}
