using System.Collections.Generic;
using LumberJack.Core;

namespace LumberJack.Trees
{
    /// <summary>
    /// Estratégia de onde nascem os galhos de um galho pai (espiral, anéis de pinheiro, bifurcação...).
    /// Escolhida no inspector de cada nível de galhos da espécie.
    /// </summary>
    public interface IBranchLayout
    {
        /// <summary>
        /// Adiciona em <paramref name="results"/> os pontos onde nascem os galhos de um pai com esse comprimento (m).
        /// Deve usar só <paramref name="random"/> para sortear, para a árvore ser determinística.
        /// </summary>
        void Layout(float parentLength, ref SeededRandom random, List<BranchAttachment> results);

        /// <summary>Corrige valores inválidos (chamado no OnValidate da espécie).</summary>
        void Validate();
    }
}
