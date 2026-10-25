using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Tombo de um corpo rígido em volta de um eixo fixo (a borda do toco) só pela gravidade: o mesmo resultado
    /// em todo cliente, sem quique. O ângulo cresce no mesmo sentido de uma velocidade angular nesse eixo.
    /// </summary>
    public sealed class TreeFallMotion
    {
        // A árvore nunca volta para trás: o empurrão inicial vale como o mínimo (a abertura do corte segura o resto)
        private const float MinSpeedFraction = 0.5f;

        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private float _minSpeed;
        private Vector3 _centerOffset;
        private Vector3 _gravity;
        private float _inertiaPerMass;

        /// <summary>Quanto já tombou (rad).</summary>
        public float Angle { get; private set; }

        /// <summary>Velocidade do tombo (rad/s).</summary>
        public float Speed { get; private set; }

        public Vector3 Axis { get; private set; }
        public Vector3 Pivot { get; private set; }

        /// <summary>Começa o tombo. <paramref name="inertiaPerMass"/>: inércia em volta do eixo dividida pela massa (m²).</summary>
        public void Begin(Vector3 pivot, Vector3 axis, Vector3 position, Quaternion rotation, Vector3 centerOfMass,
            float inertiaPerMass, float startSpeed, Vector3 gravity)
        {
            Pivot = pivot;
            Axis = axis.normalized;
            _startPosition = position;
            _startRotation = rotation;
            _centerOffset = centerOfMass - pivot;
            _inertiaPerMass = inertiaPerMass;
            _gravity = gravity;
            Angle = 0f;
            Speed = startSpeed;
            _minSpeed = startSpeed * MinSpeedFraction;
        }

        public void Step(float deltaTime)
        {
            // Euler semi-implícito: estável com o passo fixo da física
            Speed = Mathf.Max(Speed + AccelerationAt(Angle) * deltaTime, _minSpeed);
            Angle += Speed * deltaTime;
        }

        public void GetPose(float angle, out Vector3 position, out Quaternion rotation)
        {
            Quaternion turn = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Axis);
            position = Pivot + turn * (_startPosition - Pivot);
            rotation = turn * _startRotation;
        }

        // Torque da gravidade no centro de massa, em volta do eixo
        private float AccelerationAt(float angle)
        {
            Vector3 offset = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Axis) * _centerOffset;
            return Vector3.Dot(Vector3.Cross(offset, _gravity), Axis) / _inertiaPerMass;
        }
    }
}
