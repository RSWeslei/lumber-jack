using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Ignora a colisão entre dois grupos de colisores e só religa quando eles não se encostam mais.
    /// Religar com eles sobrepostos faz o PhysX separar de uma vez: o pedaço pula e sai girando.
    /// </summary>
    public static class CollisionSeparation
    {
        // Ainda encostados depois disso: continuam se ignorando (melhor atravessar do que explodir)
        private const float MaxSeconds = 10f;
        // Penetração (m) a partir da qual nascer encostado vira "sobreposto"; contato normal com o chão é menor
        private const float MinOverlapDepth = 0.05f;
        private const int MaxOverlaps = 32;

        private static readonly Collider[] OverlapBuffer = new Collider[MaxOverlaps];
        private static readonly List<Collider> Overlapping = new();

        public static async void IgnoreUntilApart(Collider[] first, Collider[] second, CancellationToken token)
        {
            SetIgnored(first, second, true);
            try
            {
                for (float time = 0f; time < MaxSeconds; time += Time.fixedDeltaTime)
                {
                    await Awaitable.FixedUpdateAsync(token);
                    if (AnyOverlap(first, second)) continue;

                    SetIgnored(first, second, false);
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                // O dono foi destruído: os pares continuam se ignorando
            }
        }

        /// <summary>
        /// Para um corpo que acabou de nascer: ignora o que ele já nasce atravessando nas camadas de
        /// <paramref name="layerMask"/> (toco, outra árvore, outro pedaço; nunca o chão) até se separarem.
        /// </summary>
        public static void IgnoreSpawnOverlaps(Rigidbody body, Collider[] colliders, int layerMask, CancellationToken token)
        {
            Overlapping.Clear();
            foreach (Collider collider in colliders)
            {
                if (collider is BoxCollider box) CollectDeepOverlaps(body, box, layerMask);
            }
            if (Overlapping.Count == 0) return;

            IgnoreUntilApart(colliders, Overlapping.ToArray(), token);
        }

        private static void CollectDeepOverlaps(Rigidbody body, BoxCollider box, int layerMask)
        {
            Transform boxTransform = box.transform;
            Vector3 center = boxTransform.TransformPoint(box.center);
            Vector3 halfExtents = Vector3.Scale(box.size, boxTransform.lossyScale) * 0.5f;
            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, OverlapBuffer, boxTransform.rotation,
                layerMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider other = OverlapBuffer[i];
                if (ReferenceEquals(other.attachedRigidbody, body) || Overlapping.Contains(other) || !IsTreePart(other)) continue;

                Transform otherTransform = other.transform;
                bool isOverlapping = Physics.ComputePenetration(box, boxTransform.position, boxTransform.rotation,
                    other, otherTransform.position, otherTransform.rotation, out _, out float depth);
                if (isOverlapping && depth > MinOverlapDepth) Overlapping.Add(other);
            }
        }

        // Só toco/árvore ou outro pedaço: chão e cenário nunca são ignorados (o pedaço cairia através deles)
        private static bool IsTreePart(Collider other)
        {
            if (other.TryGetComponent(out ChoppableTree _)) return true;

            Rigidbody otherBody = other.attachedRigidbody;
            // attachedRigidbody devolve null de verdade quando não há corpo: ReferenceEquals basta
            return !ReferenceEquals(otherBody, null) && otherBody.TryGetComponent(out TreePiece _);
        }

        private static bool AnyOverlap(Collider[] first, Collider[] second)
        {
            foreach (Collider firstCollider in first)
            {
                // Pedaços podem ter sido cortados (destruídos) enquanto esperavam
                if (firstCollider == null) continue;

                Bounds bounds = firstCollider.bounds;
                if (second.Any(secondCollider => secondCollider != null && bounds.Intersects(secondCollider.bounds)))
                {
                    return true;
                }
            }
            return false;
        }

        private static void SetIgnored(Collider[] first, Collider[] second, bool isIgnored)
        {
            foreach (Collider firstCollider in first)
            {
                if (firstCollider == null) continue;
                foreach (Collider secondCollider in second)
                {
                    if (secondCollider != null) Physics.IgnoreCollision(firstCollider, secondCollider, isIgnored);
                }
            }
        }
    }
}
