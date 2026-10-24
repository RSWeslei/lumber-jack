using UnityEngine;

namespace LumberJack.Core
{
    /// <summary>
    /// Algo que o machado corta (tora, árvore em pé): dá o plano do corte para o preview e recebe o golpe.
    /// </summary>
    public interface IAxeTarget
    {
        Transform Transform { get; }

        /// <summary>Vértices da malha (espaço local, em cache) para desenhar o contorno do corte.</summary>
        Vector3[] Vertices { get; }

        int[] Triangles { get; }

        /// <summary>Quanto do corte atual já foi feito (0 a 1); 0 se corta num golpe só.</summary>
        float CutProgress { get; }

        /// <summary>Plano do corte (espaço local) onde o jogador está mirando.</summary>
        Plane GetCutPlane(Vector3 worldPoint, Vector3 viewForward, Vector3 viewUp, float tilt);

        bool CanCut(Plane localPlane);

        /// <summary>Um golpe de machado nesse plano.</summary>
        void Strike(Plane localPlane, Vector3 swingDirection, float damage);
    }
}
