using UnityEngine;

namespace LumberJack.Core
{
    /// <summary>
    /// Em um campo <c>[SerializeReference]</c> de interface/classe abstrata, mostra no inspector uma lista
    /// para escolher a implementação (ex.: o tipo de distribuição de galhos).
    /// </summary>
    public class SubclassSelectorAttribute : PropertyAttribute
    {
    }
}
