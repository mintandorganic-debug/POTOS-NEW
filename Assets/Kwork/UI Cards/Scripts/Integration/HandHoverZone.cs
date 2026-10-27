using UnityEngine;

namespace CardCarouselUI
{
    public class HandHoverZone
    {
        private readonly float radiusPadding;
        private readonly float dropFraction;

        public HandHoverZone(float radiusPadding, float dropFraction)
        {
            this.radiusPadding = radiusPadding;
            this.dropFraction = dropFraction;
        }

        public bool TryGetSphere(out Vector3 center, out float radius)
        {
            center = default;
            radius = 0f;

            bool found = false;
            Bounds bounds = default;

            foreach (global::Card card in Object.FindObjectsByType<global::Card>(FindObjectsSortMode.None))
            {
                if (!LocalHandCardFilter.IsInLocalHand(card))
                    continue;

                foreach (Collider collider in card.GetComponentsInChildren<Collider>())
                {
                    if (!found)
                        bounds = collider.bounds;
                    else
                        bounds.Encapsulate(collider.bounds);

                    found = true;
                }
            }

            if (!found)
                return false;

            radius = bounds.extents.magnitude + radiusPadding;
            center = bounds.center + Vector3.down * (radius * 2f * dropFraction);

            return true;
        }

        public bool IsAimedAt(Camera playerCamera, float maxDistance)
        {
            if (!TryGetSphere(out Vector3 center, out float radius))
                return false;

            Vector3 origin = playerCamera.transform.position;
            Vector3 direction = playerCamera.transform.forward;

            Vector3 toOrigin = origin - center;
            float along = Vector3.Dot(toOrigin, direction);
            float discriminant = along * along - (toOrigin.sqrMagnitude - radius * radius);

            if (discriminant < 0f)
                return false;

            float entry = -along - Mathf.Sqrt(discriminant);
            float exit = -along + Mathf.Sqrt(discriminant);

            return exit >= 0f && Mathf.Max(entry, 0f) <= maxDistance;
        }
    }
}
