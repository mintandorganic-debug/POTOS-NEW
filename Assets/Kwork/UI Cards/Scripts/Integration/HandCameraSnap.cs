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
            global::GameLog.Write(
                "HandCameraSnap",
                $"Constructor hasZone={zone != null}");

            this.zone = zone;
        }

        public void Engage()
        {
            NetworkIdentity player = NetworkClient.localPlayer;
            global::Card source = FindLocalHandCard();

            global::GameLog.Write(
                "HandCameraSnap",
                $"Engage localPlayer={(player != null ? player.netId.ToString() : "NULL")} anchorCard={(source != null ? source.name : "NULL")}");

            if (player == null || source == null || !zone.TryGetSphere(out Vector3 center, out float radius))
            {
                global::GameLog.Write(
                    "HandCameraSnap",
                    $"Engage skipped: localPlayer={(player != null ? "present" : "NULL")} anchorCard={(source != null ? "present" : "NULL")} (or hand zone sphere not found), camera not locked");

                return;
            }

            target = new GameObject("Hand Zone Look Target");
            target.transform.position = center + Vector3.up * radius;

            anchor = source;
            playerNetId = player.netId;

            global::GameLog.Write(
                "HandCameraSnap",
                $"Engage locking camera playerNetId={playerNetId} anchor={anchor.name} lookTarget={target.transform.position}");

            anchor.LockCameraAt(playerNetId, target.transform);
        }

        public void Release()
        {
            global::GameLog.Write(
                "HandCameraSnap",
                $"Release hasTarget={target != null} hasAnchor={anchor != null} playerNetId={playerNetId}");

            if (target == null)
            {
                global::GameLog.Write(
                    "HandCameraSnap",
                    "Release skipped: camera was not locked");

                return;
            }

            if (anchor != null)
                anchor.UnlockCamera(playerNetId);

            Object.Destroy(target);

            target = null;
            anchor = null;
        }

        private static global::Card FindLocalHandCard()
        {
            foreach (global::Card card in Object.FindObjectsByType<global::Card>())
            {
                if (LocalHandCardFilter.IsInLocalHand(card))
                {
                    global::GameLog.Write(
                        "HandCameraSnap",
                        $"FindLocalHandCard found card={card.name} netId={card.netId}");

                    return card;
                }
            }

            global::GameLog.Write(
                "HandCameraSnap",
                "FindLocalHandCard no local hand card found");

            return null;
        }
    }
}
