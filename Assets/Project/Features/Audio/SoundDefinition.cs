using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Audio
{
    /// <summary>
    /// Um som do jogo: vários clipes parecidos e um pouco de variação de volume e tom, para não repetir igual.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSound", menuName = "Lumber Jack/Audio/Sound")]
    public class SoundDefinition : ScriptableObject
    {
        private const uint RandomSeed = 7919u;

        // Variação só de som (cosmética): uma sequência para todos os sons
        private static SeededRandom _random = new(RandomSeed);

        [SerializeField] private AudioClip[] _clips = System.Array.Empty<AudioClip>();
        [RangeLimits(0f, 1f)]
        [SerializeField] private FloatRange _volume = new(0.8f, 1f);
        [Tooltip("Tom (1 = normal); abaixo de 1 fica mais grave")]
        [RangeLimits(0.1f, 3f)]
        [SerializeField] private FloatRange _pitch = new(0.95f, 1.05f);

        private void OnValidate()
        {
            _volume = _volume.Clamped(0f, 1f);
            _pitch = _pitch.Clamped(0.1f, 3f);
        }

        /// <summary>Toca um dos clipes na fonte; <paramref name="volumeScale"/> multiplica o volume (ex.: força da batida).</summary>
        public void Play(AudioSource source, float volumeScale = 1f)
        {
            if (_clips.Length == 0 || volumeScale <= 0f) return;

            AudioClip clip = _clips[_random.RangeInclusive(0, _clips.Length - 1)];
            source.pitch = _pitch.Pick(ref _random);
            source.PlayOneShot(clip, _volume.Pick(ref _random) * volumeScale);
        }
    }
}
