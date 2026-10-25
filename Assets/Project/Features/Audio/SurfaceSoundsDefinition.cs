using System;
using UnityEngine;

namespace LumberJack.Audio
{
    /// <summary>
    /// Som dos passos por tipo de chão: o collider pisado é reconhecido pelo PhysicsMaterial ou pela tag (do collider
    /// ou do corpo dele). O que não bate com nenhum usa o som padrão.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSurfaceSounds", menuName = "Lumber Jack/Audio/Surface Sounds")]
    public class SurfaceSoundsDefinition : ScriptableObject
    {
        [Tooltip("Chão sem tipo reconhecido")]
        [SerializeField] private SoundDefinition _defaultSound;
        [SerializeField] private Surface[] _surfaces = Array.Empty<Surface>();

        public SoundDefinition SoundFor(Collider ground)
        {
            PhysicsMaterial material = ground.sharedMaterial;
            Rigidbody body = ground.attachedRigidbody;
            foreach (Surface surface in _surfaces)
            {
                if (surface.Matches(ground, material, body)) return surface.Sound;
            }
            return _defaultSound;
        }

        [Serializable]
        private class Surface
        {
            [Tooltip("Vazio = não usa material")]
            [SerializeField] private PhysicsMaterial _material;
            [Tooltip("Vazio = não usa tag")]
            [SerializeField] private string _tag;
            [SerializeField] private SoundDefinition _sound;

            public SoundDefinition Sound => _sound;

            public bool Matches(Collider ground, PhysicsMaterial material, Rigidbody body)
            {
                if (_material != null && ReferenceEquals(material, _material)) return true;
                if (string.IsNullOrEmpty(_tag)) return false;
                // attachedRigidbody é null de verdade sem corpo
                return ground.CompareTag(_tag) || (!ReferenceEquals(body, null) && body.CompareTag(_tag));
            }
        }
    }
}
