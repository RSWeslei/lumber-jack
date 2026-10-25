using LumberJack.Audio;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Sons da árvore e dos pedaços: rangido ao começar a cair, pancada ao deitar, galho quebrando e pedaços batendo.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTreeSounds", menuName = "Lumber Jack/Trees/Tree Sounds")]
    public class TreeSoundsDefinition : ScriptableObject
    {
        [SerializeField] private SoundDefinition _creak;
        [SerializeField] private SoundDefinition _land;
        [SerializeField] private SoundDefinition _branchBreak;
        [SerializeField] private SoundDefinition _impact;

        [Header("Pancadas dos pedaços")]
        [Tooltip("Velocidade da batida (m/s) abaixo da qual não faz som")]
        [Min(0f)]
        [SerializeField] private float _minImpactSpeed = 1f;
        [Tooltip("Velocidade da batida (m/s) com o som no volume máximo")]
        [Min(0.1f)]
        [SerializeField] private float _fullImpactSpeed = 6f;
        [Tooltip("Tempo mínimo (s) entre pancadas do mesmo pedaço")]
        [Min(0f)]
        [SerializeField] private float _impactInterval = 0.15f;

        public SoundDefinition Creak => _creak;
        public SoundDefinition Land => _land;
        public SoundDefinition BranchBreak => _branchBreak;
        public SoundDefinition Impact => _impact;
        public float ImpactInterval => _impactInterval;

        /// <summary>Volume (0 a 1) da pancada para a velocidade da batida; 0 = sem som.</summary>
        public float ImpactVolume(float speed)
        {
            if (speed < _minImpactSpeed) return 0f;
            return Mathf.Clamp01(speed / _fullImpactSpeed);
        }
    }
}
