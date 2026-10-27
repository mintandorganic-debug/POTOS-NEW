using Mirror;
using UnityEngine;

namespace CardCarouselUI
{
    public class HandCameraSnap
    {
        private readonly HandHoverZone zone;

        private GameObject target;
        private InteractableObject anchor;
        private uint playerNetId;

        public HandCameraSnap(HandHoverZone zone)
        {
            this.zone = zone;
        }

        public void Engage()
        {
            NetworkIdentity player = NetworkClient.localPlayer;
            global::Card source = FindLocalHandCard();

            if (player == null || source == null || !zone.TryGetSphere(out Vector3 center, out float radius))
                return;

            target = new GameObject("Hand Zone Look Target");
            target.transform.position = center + Vector3.up * radius;

            anchor = source;
            playerNetId = player.netId;

            anchor.LockCameraAt(playerNetId, target.transform);
        }

        public void Release()
        {
            if (target == null)
                return;

            if (anchor != null)
                anchor.UnlockCamera(playerNetId);

            Object.Destroy(target);

            target = null;
            anchor = null;
        }

        private static global::Card FindLocalHandCard()
        {
            foreach (global::Card card in Object.FindObjectsByType<global::Card>(FindObjectsSortMode.None))
            {
                if (LocalHandCardFilter.IsInLocalHand(card))
                    return card;
            }

            return null;
        }
    }
}
