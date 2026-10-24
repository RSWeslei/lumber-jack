using UnityEngine;

namespace LumberJack.Wood
{
    /// <summary>
    /// Tipo de madeira (carvalho, olmo...): preço e características.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWood", menuName = "Lumber Jack/Wood/Wood Type")]
    public class WoodDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [Tooltip("Preço em centavos por m3 da madeira bruta (tronco)")]
        [SerializeField] private int _rawPriceCentsPerCubicMeter;
        [Tooltip("Preço em centavos por m3 da madeira refinada (tábua)")]
        [SerializeField] private int _refinedPriceCentsPerCubicMeter;
        [SerializeField] private float _hardness;
        [SerializeField] private Texture _texture;

        public string DisplayName => _displayName;
        public int RawPriceCentsPerCubicMeter => _rawPriceCentsPerCubicMeter;
        public int RefinedPriceCentsPerCubicMeter => _refinedPriceCentsPerCubicMeter;
        public float Hardness => _hardness;
        public Texture Texture => _texture;
    }
}
