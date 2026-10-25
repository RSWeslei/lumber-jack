using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Ignora a colisão entre dois grupos de colisores e religa cada par assim que ele não se atravessa mais.
    /// Religar com eles sobrepostos faz o PhysX separar de uma vez: o pedaço pula e sai girando.
    /// </summary>
    public static class CollisionSeparation
    {
        // Ainda encostados depois disso: continuam se ignorando (melhor atravessar do que explodir)
        private const float MaxSeconds = 10f;
        // Penetração (m) a partir da qual nascer encostado vira "sobreposto"; contato normal com o chão é menor
        private const float MinOverlapDepth = 0.05f;
        // Par que se atravessa menos que isso (m) já pode colidir: encostar na face do corte não conta
        private const float SeparatedDepth = 0.01f;
        private const int MaxOverlaps = 32;

        private static readonly Collider[] OverlapBuffer = new Collider[MaxOverlaps];
        private static readonly List<Collider> Overlapping = new();

        public static async void IgnoreUntilApart(Collider[] first, Collider[] second, CancellationToken token)
        {
            SetIgnored(first, second, true);
            // Par a par: duas metades deitadas lado a lado nunca saem de dentro das caixas uma da outra
            var isPairIgnored = new bool[first.Length * second.Length];
            for (int i = 0; i < isPairIgnored.Length; i++) isPairIgnored[i] = true;
            int ignoredPairs = isPairIgnored.Length;
            try
            {
                for (float time = 0f; time < MaxSeconds && ignoredPairs > 0; time += Time.fixedDeltaTime)
                {
                    await Awaitable.FixedUpdateAsync(token);
                    ignoredPairs -= ReleaseSeparatedPairs(first, second, isPairIgnored);
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
                CollectDeepOverlaps(body, collider, layerMask);
            }
            if (Overlapping.Count == 0) return;

            IgnoreUntilApart(colliders, Overlapping.ToArray(), token);
        }

        private static void CollectDeepOverlaps(Rigidbody body, Collider collider, int layerMask)
        {
            Transform colliderTransform = collider.transform;
            Bounds bounds = collider.bounds;
            int count = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, OverlapBuffer, Quaternion.identity,
                layerMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider other = OverlapBuffer[i];
                if (ReferenceEquals(other.attachedRigidbody, body) || Overlapping.Contains(other) || !IsTreePart(other)) continue;

                Transform otherTransform = other.transform;
                bool isOverlapping = Physics.ComputePenetration(collider, colliderTransform.position, colliderTransform.rotation,
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

        // Religa os pares que não se atravessam mais e devolve quantos religou
        private static int ReleaseSeparatedPairs(Collider[] first, Collider[] second, bool[] isPairIgnored)
        {
            int released = 0;
            for (int i = 0; i < first.Length; i++)
            {
                for (int j = 0; j < second.Length; j++)
                {
                    int pair = i * second.Length + j;
                    if (!isPairIgnored[pair]) continue;

                    Collider a = first[i];
                    Collider b = second[j];
                    // Pedaços podem ter sido cortados (destruídos) enquanto esperavam
                    bool isGone = a == null || b == null;
                    if (!isGone && IsPenetrating(a, b)) continue;

                    if (!isGone) Physics.IgnoreCollision(a, b, false);
                    isPairIgnored[pair] = false;
                    released++;
                }
            }
            return released;
        }

        private static bool IsPenetrating(Collider a, Collider b)
        {
            if (!a.bounds.Intersects(b.bounds)) return false;

            Transform aTransform = a.transform;
            Transform bTransform = b.transform;
            return Physics.ComputePenetration(a, aTransform.position, aTransform.rotation, b, bTransform.position,
                bTransform.rotation, out _, out float depth) && depth > SeparatedDepth;
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
