using UnityEngine;

namespace LumberJack.Audio
{
    /// <summary>
    /// Fonte de som 3D para objetos criados em runtime (pedaços de árvore): mais alto perto, some com a distância.
    /// </summary>
    public static class SpatialAudio
    {
        private const float MinDistance = 3f;
        private const float MaxDistance = 60f;

        public static AudioSource AddSource(GameObject target)
        {
            var source = target.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = MinDistance;
            source.maxDistance = MaxDistance;
            source.dopplerLevel = 0f;
            return source;
        }
    }
}
