using UnityEngine;

namespace CardCarouselUI
{
    [RequireComponent(typeof(CardGazeHandInput))]
    public class HandHoverZoneGizmos : MonoBehaviour
    {
        private static readonly Color ZoneColor = Color.green;

        private CardGazeHandInput gaze;

        private void OnDrawGizmos()
        {
            if (gaze == null)
                gaze = GetComponent<CardGazeHandInput>();

            if (!gaze.Zone.TryGetSphere(out Vector3 center, out float radius))
                return;

            Gizmos.color = new Color(ZoneColor.r, ZoneColor.g, ZoneColor.b, 0.15f);
            Gizmos.DrawSphere(center, radius);

            Gizmos.color = ZoneColor;
            Gizmos.DrawWireSphere(center, radius);
        }
    }
}
