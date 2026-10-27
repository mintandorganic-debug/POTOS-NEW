using Mirror;
using UnityEngine;

namespace CardCarouselUI
{
    public class MouseModePopup : IPopup
    {
        private readonly IPopup inner;
        private readonly HandCameraSnap cameraSnap;

        private MonoBehaviour frozen;
        private CursorLockMode previousLockMode;
        private bool previousVisible;

        public MouseModePopup(IPopup inner, HandCameraSnap cameraSnap)
        {
            this.inner = inner;
            this.cameraSnap = cameraSnap;
        }

        public bool IsVisible => inner.IsVisible;

        public void Show()
        {
            previousLockMode = Cursor.lockState;
            previousVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            FreezeInteraction();
            cameraSnap.Engage();

            inner.Show();
        }

        public void Hide()
        {
            cameraSnap.Release();
            UnfreezeInteraction();

            Cursor.lockState = previousLockMode;
            Cursor.visible = previousVisible;

            inner.Hide();
        }

        private void FreezeInteraction()
        {
            NetworkIdentity player = NetworkClient.localPlayer;

            frozen = player != null ? player.GetComponentInChildren<Interactor>() : null;

            if (frozen != null)
            {
                frozen.StopAllCoroutines();
                frozen.enabled = false;
            }

            if (global::HudController.Instance != null)
                global::HudController.Instance.HideCardInfo();
        }

        private void UnfreezeInteraction()
        {
            if (frozen != null)
                frozen.enabled = true;

            frozen = null;
        }
    }
}
